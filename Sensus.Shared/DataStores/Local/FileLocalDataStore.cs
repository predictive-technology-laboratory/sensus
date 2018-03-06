// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Sensus.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO.Compression;
using Sensus.UI.UiProperties;
using System.Linq;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// Stores each <see cref="Datum"/> as plain-text JSON in a gzip-compressed file on the device's local storage media. Also
    /// supports encryption.
    /// </summary>
    public class FileLocalDataStore : LocalDataStore, IClearableDataStore
    {
        /// <summary>
        /// Number of <see cref="Datum"/> to write before checking the size of the data store.
        /// </summary>
        private const int DATA_WRITES_PER_SIZE_CHECK = 10000;

        /// <summary>
        /// Threshold on the size of the local storage directory in MB. When this is exceeded, a write to the <see cref="Remote.RemoteDataStore"/> 
        /// will be forced.
        /// </summary>
        private const double REMOTE_WRITE_TRIGGER_STORAGE_DIRECTORY_SIZE_MB = 10;

        /// <summary>
        /// Threshold on the size of files within the local storage directory. When this is exceeded, a new file will be started.
        /// </summary>
        private const double MAX_FILE_SIZE_MB = 1;

        /// <summary>
        /// File-based storage uses a <see cref="BufferedStream"/> for efficiency. This is the default buffer size in bytes.
        /// </summary>
        private const int DEFAULT_BUFFER_SIZE_BYTES = 4096;

        /// <summary>
        /// File extension to use for JSON files.
        /// </summary>
        private const string JSON_FILE_EXTENSION = ".json";

        /// <summary>
        /// File extension to use for GZip files.
        /// </summary>
        private const string GZIP_FILE_EXTENSION = ".gz";

        /// <summary>
        /// Files extension to use for encrypted files.
        /// </summary>
        private const string ENCRYPTED_FILE_EXTENSION = ".bin";

        /// <summary>
        /// The encryption key size in bits.
        /// </summary>
        private const int ENCRYPTION_KEY_SIZE_BITS = 32 * 8;

        /// <summary>
        /// The encryption initialization vector size in bits.
        /// </summary>
        private const int ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS = 16 * 8;

        private string _path;
        private BufferedStream _file;
        private CompressionLevel _compressionLevel;
        private int _bufferSizeBytes;
        private bool _encrypt;
        private Task _writeToRemoteTask;
        private long _totalDataWritten;
        private long _bytesWrittenToCurrentFile;
        private long _dataWrittenToCurrentFile;

        private readonly object _locker = new object();

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Gets or sets the compression level. Options are <see cref="CompressionLevel.NoCompression"/> (no compression), <see cref="CompressionLevel.Fastest"/> 
        /// (computationally faster but less compression), and <see cref="CompressionLevel.Optimal"/> (computationally slower but more compression).
        /// </summary>
        /// <value>The compression level.</value>
        [ListUiProperty("Compression Level:", true, 1, new object[] { CompressionLevel.NoCompression, CompressionLevel.Fastest, CompressionLevel.Optimal })]
        public CompressionLevel CompressionLevel
        {
            get
            {
                return _compressionLevel;
            }
            set
            {
                _compressionLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets the buffer size in bytes. All <see cref="Probes.Probe"/>d data will be held in memory until the buffer is filled, at which time
        /// the data will be written to the file. There is not a single-best <see cref="BufferSizeBytes"/> value. If data are collected at high rates, a
        /// higher value will be best to minimize write operations. If data are collected at low rates, a lower value will be best to minimize the likelihood
        /// of data loss when the app is killed or crashes (as the buffer resides in RAM).
        /// </summary>
        /// <value>The buffer size in bytes.</value>
        [EntryIntegerUiProperty("Buffer Size (Bytes):", true, 2)]
        public int BufferSizeBytes
        {
            get { return _bufferSizeBytes; }
            set
            {
                if (value <= 0)
                {
                    value = DEFAULT_BUFFER_SIZE_BYTES;
                }

                _bufferSizeBytes = value;
            }
        }

        /// <summary>
        /// Whether or not to apply asymmetric-key encryption to data. If this is enabled, then you must provide a public encryption 
        /// key to <see cref="Protocol.AsymmetricEncryptionPublicKey"/>. You can generate a public encryption key following the instructions
        /// provided for <see cref="Protocol.AsymmetricEncryptionPublicKey"/>. Note that data are not encrypted immediately. They are first
        /// written to a compressed file on the device where they live unencrypted for a period of time.
        /// </summary>
        /// <value><c>true</c> to encrypt; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Encrypt (must set public encryption key on protocol in order to use):", true, 6)]
        public bool Encrypt
        {
            get
            {
                return _encrypt;
            }
            set
            {
                _encrypt = value;
            }
        }

        [JsonIgnore]
        public string StorageDirectory
        {
            get
            {
                string directory = System.IO.Path.Combine(Protocol.StorageDirectory, GetType().FullName);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                return directory;
            }
        }

        [JsonIgnore]
        public long BytesWrittenToCurrentFile
        {
            get { return _bytesWrittenToCurrentFile; }
        }

        [JsonIgnore]
        public long DataWrittenToCurrentFile
        {
            get { return _dataWrittenToCurrentFile; }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get { return "File"; }
        }

        [JsonIgnore]
        public override string SizeDescription
        {
            get
            {
                string description = null;

                try
                {
                    description = Math.Round(SensusServiceHelper.GetDirectorySizeMB(StorageDirectory), 1) + " MB";
                }
                catch (Exception)
                {
                }

                return description;
            }
        }

        public FileLocalDataStore()
        {
            _compressionLevel = CompressionLevel.Optimal;
            _bufferSizeBytes = DEFAULT_BUFFER_SIZE_BYTES;
            _encrypt = false;
            _totalDataWritten = 0;
            _bytesWrittenToCurrentFile = 0;
            _dataWrittenToCurrentFile = 0;
        }

        public override void Start()
        {
            base.Start();

            // ensure that we have a valid encryption setup if one is requested
            if (_encrypt)
            {
                try
                {
                    Protocol.AsymmetricEncryption.Encrypt("testing");
                }
                catch (Exception)
                {
                    throw new Exception("Ensure that a valid public key is set on the Protocol.");
                }
            }

            // file needs to be ready to accept data immediately, so write new file before calling base.Start
            OpenFile();
        }

        private void OpenFile()
        {
            lock (_locker)
            {
                // it's possible to stop the datastore before entering this lock.
                if (!Running)
                {
                    return;
                }

                // open new file
                _path = null;
                Exception mostRecentException = null;
                for (int i = 0; _path == null && i < 5; ++i)
                {
                    try
                    {
                        _path = System.IO.Path.Combine(StorageDirectory, Guid.NewGuid().ToString());
                        Stream file = new FileStream(_path, FileMode.CreateNew, FileAccess.Write);

                        // add gzip stream if doing compression
                        if (_compressionLevel != CompressionLevel.NoCompression)
                        {
                            file = new GZipStream(file, _compressionLevel, false);
                        }

                        // use buffering for compression and runtime performance
                        _file = new BufferedStream(file, _bufferSizeBytes);
                        _bytesWrittenToCurrentFile = 0;
                        _dataWrittenToCurrentFile = 0;
                    }
                    catch (Exception ex)
                    {
                        mostRecentException = ex;
                        _path = null;
                    }
                }

                // we could not open a file to write, so we cannot proceed. report the most recent exception and bail.
                if (_path == null)
                {
                    throw SensusException.Report("Failed to open file for local data store.", mostRecentException);
                }
                else
                {
                    // open the JSON array
                    byte[] jsonBeginArrayBytes = Encoding.UTF8.GetBytes("[");
                    _file.Write(jsonBeginArrayBytes, 0, jsonBeginArrayBytes.Length);                    
                }
            }
        }

        public override Task<bool> WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                bool written = false;

                // only 1 write operation at once
                lock (_locker)
                {
                    // it's possible to stop the datastore before entering this lock, in which case we won't
                    // have a file to write to. check for a running data store here.
                    if(!Running)
                    {
                        return written;
                    }

                    // get anonymized JSON for datum
                    string datumJSON = null;
                    try
                    {
                        datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Failed to get JSON for datum.", ex);
                    }

                    // write JSON to file
                    if (datumJSON != null)
                    {
                        try
                        {
                            byte[] datumJsonBytes = Encoding.UTF8.GetBytes((_dataWrittenToCurrentFile == 0 ? "" : ",") + Environment.NewLine + datumJSON);
                            _file.Write(datumJsonBytes, 0, datumJsonBytes.Length);
                            _bytesWrittenToCurrentFile += datumJsonBytes.Length;
                            _dataWrittenToCurrentFile++;
                            _totalDataWritten++;
                            written = true;
                        }
                        catch (Exception writeException)
                        {
                            SensusException.Report("Failed to write datum JSON bytes to file.", writeException);

                            // something went wrong with file write...switch to a new file in the hope that it will work better.

                            try
                            {
                                CloseFile();
                            }
                            catch (Exception closeException)
                            {
                                SensusException.Report("Failed to close file after failing to write it.", closeException);
                            }

                            try
                            {
                                OpenFile();
                            }
                            catch (Exception openException)
                            {
                                SensusException.Report("Failed to open new file after failing to write the previous one.", openException);
                            }
                        }
                    }
                }

                // every so often, check the sizes of the file and data store
                if ((_dataWrittenToCurrentFile % DATA_WRITES_PER_SIZE_CHECK) == 0)
                {
                    // switch to a new file if the current one has grown too large
                    if (SensusServiceHelper.GetFileSizeMB(_path) >= MAX_FILE_SIZE_MB)
                    {
                        CloseFile();
                        OpenFile();
                    }

                    // write the local data to remote if the overall size has grown too large
                    await WriteToRemoteIfTooLargeAsync(cancellationToken);
                }

                return written;
            });
        }

        public override Task WriteToRemoteAsync(CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                // it's possible to stop the datastore before entering this lock.
                if (!Running)
                {
                    return Task.CompletedTask;
                }

                CloseFile();
                PromoteFiles();
                OpenFile();

                // if this is the first write or the previous write is finished, run a new task.
                if (_writeToRemoteTask == null ||
                    _writeToRemoteTask.Status == TaskStatus.Canceled ||
                    _writeToRemoteTask.Status == TaskStatus.Faulted ||
                    _writeToRemoteTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeToRemoteTask = Task.Run(async () =>
                    {
                        // get all promoted file paths based on selected options. promoted files are those with an extension (.json, .gz, or .bin)
                        string promotedPathExtension = 
                            JSON_FILE_EXTENSION +
                            (_compressionLevel != CompressionLevel.NoCompression ? GZIP_FILE_EXTENSION : "") +
                            (_encrypt ? ENCRYPTED_FILE_EXTENSION : "");

                        // get paths to write
                        string[] promotedPaths = Directory.GetFiles(StorageDirectory, "*" + promotedPathExtension).ToArray();

                        // write each promoted file
                        foreach (string protmotedPath in promotedPaths)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            // wrap in try-catch to ensure that we process all files
                            try
                            {
                                // get stream name and content type
                                string streamName = System.IO.Path.GetFileName(protmotedPath);
                                string streamContentType;
                                if (streamName.EndsWith(JSON_FILE_EXTENSION))
                                {
                                    streamContentType = "application/json";
                                }
                                else if (streamName.EndsWith(GZIP_FILE_EXTENSION))
                                {
                                    streamContentType = "application/gzip";
                                }
                                else if (streamName.EndsWith(ENCRYPTED_FILE_EXTENSION))
                                {
                                    streamContentType = "application/octet-stream";
                                }
                                else
                                {
                                    // this should never happen. write anyway and report the situation.
                                    streamContentType = "application/octet-stream";
                                    SensusException.Report("Unknown stream file extension:  " + streamName);
                                }

                                using (FileStream fileToWrite = new FileStream(protmotedPath, FileMode.Open, FileAccess.Read))
                                {
                                    await Protocol.RemoteDataStore.WriteDataStreamAsync(fileToWrite, streamName, streamContentType, cancellationToken);
                                }

                                // file was written remotely. delete it locally.
                                File.Delete(protmotedPath);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to write file:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }

                    }, cancellationToken);
                }

                return _writeToRemoteTask;
            }
        }

        protected override bool IsTooLarge()
        {
            lock (_locker)
            {
                return SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) >= REMOTE_WRITE_TRIGGER_STORAGE_DIRECTORY_SIZE_MB;
            }
        }

        private void CloseFile()
        {
            lock (_locker)
            {
                if (_file != null)
                {
                    try
                    {
                        // end the JSON array and close the file
                        byte[] jsonEndArrayBytes = Encoding.UTF8.GetBytes(Environment.NewLine + "]");
                        _file.Write(jsonEndArrayBytes, 0, jsonEndArrayBytes.Length);
                        _file.Flush();
                        _file.Dispose();
                        _file = null;
                        _path = null;
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Failed to close the local file.", ex);
                    }
                }
            }
        }

        private void PromoteFiles()
        {
            lock (_locker)
            {
                foreach (string path in Directory.GetFiles(StorageDirectory))
                {
                    try
                    {
                        // promotion applies to files that don't yet have a file extension
                        if (!string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(path)))
                        {
                            continue;
                        }

                        // add the .json file extension, marking the file as complete.
                        string finalPath = path + JSON_FILE_EXTENSION;
                        File.Move(path, finalPath);

                        // add the gzip file extension if we're doing compression
                        if (_compressionLevel != CompressionLevel.NoCompression)
                        {
                            // add the .gz extension to the path
                            string gzipPath = finalPath + GZIP_FILE_EXTENSION;
                            File.Move(finalPath, gzipPath);
                            finalPath = gzipPath;
                        }

                        // encrypt the file if needed
                        if (_encrypt)
                        {
                            string encryptedPath = finalPath + ENCRYPTED_FILE_EXTENSION;

                            Protocol.AsymmetricEncryption.EncryptSymmetrically(File.ReadAllBytes(finalPath), ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, encryptedPath);

                            // if everything went through okay, delete the unencrypted file.
                            File.Delete(finalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Failed to promote file.", ex);
                    }
                }
            }
        }

        public override void Stop()
        {
            base.Stop();

            lock (_locker)
            {
                CloseFile();
                _bytesWrittenToCurrentFile = 0;
                _dataWrittenToCurrentFile = 0;
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                if (Protocol != null)
                {
                    foreach (string path in Directory.GetFiles(StorageDirectory))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to delete local file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                }
            }
        }

        public override void Reset()
        {
            base.Reset();

            _path = null;
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            lock (_locker)
            {
                string name = GetType().Name;
                string[] paths = Directory.GetFiles(StorageDirectory);

                misc += name + ":  Number of files = " + paths.Length + Environment.NewLine +
                        name + ":  Average file size (MB) = " + Math.Round(SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) / (float)paths.Length, 2) + Environment.NewLine +
                        name + ":  Promoted files = " + paths.Count(path => !string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(path))) + Environment.NewLine;
            }

            return restart;
        }
    }
}