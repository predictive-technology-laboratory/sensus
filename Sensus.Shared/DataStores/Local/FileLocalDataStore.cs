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
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;
using Sensus.Extensions;
using ICSharpCode.SharpZipLib.Tar;

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
        private const int DATA_WRITES_PER_SIZE_CHECK = 1000;

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

        private List<Datum> _storeBuffer;
        private List<Datum> _toWriteBuffer;
        private Task _writeToFileTask;
        private AutoResetEvent _checkForBufferedData;
        private AutoResetEvent _finishedCheckingForBufferedData;
        private string _path;
        private BufferedStream _file;
        private CompressionLevel _compressionLevel;
        private int _bufferSizeBytes;
        private bool _encrypt;
        private bool _writeToRemote = true;
        private Task _writeToRemoteTask;
        private long _totalDataBuffered;
        private long _totalDataWritten;
        private long _dataWrittenToCurrentFile;
        private int _filesOpened;
        private int _filesClosed;
        private int _filesPromoted;
        private int _filesWrittenToRemote;

        private readonly object _locker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <value>Paths for data upload to S3 bucket.</value>
        private string[] PromotedPaths{
            get {
                // might be a slightly over loaded get, feel free to move the file calls
                CloseFile();
                PromoteFiles();
                OpenFile();

                // get all promoted file paths based on selected options. promoted files are those with an extension (.json, .gz, or .bin).
                string promotedPathExtension = JSON_FILE_EXTENSION + (_compressionLevel != CompressionLevel.NoCompression ? GZIP_FILE_EXTENSION : "") + (_encrypt ? ENCRYPTED_FILE_EXTENSION : "");
                string[] promotedPaths = Directory.GetFiles(StorageDirectory, "*" + promotedPathExtension).ToArray();
                return promotedPaths;
            }
        }

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
        [ListUiProperty("Compression Level:", true, 1, new object[] { CompressionLevel.NoCompression, CompressionLevel.Fastest, CompressionLevel.Optimal }, true)]
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
        [EntryIntegerUiProperty("Buffer Size (Bytes):", true, 2, true)]
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
       
        /// <summary>
        /// Whether or not the application should attempt to broadcast recorded data. Manual mechanism to enforce local data storage. 
        /// Intended for use in customer demonstration in offline setting (email will be emailed, circumventing the standard data flow)
        /// </summary>
        /// <value><c>true</c> to write remote; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Write to remote (true to broadcast data, false to store locally):", true, 7)]
        public bool WriteToRemote
        {
            get
            {
                return _writeToRemote;
            }
            set
            {
                _writeToRemote = value;
                Console.WriteLine(_writeToRemote);

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
        public long DataWrittenToCurrentFile
        {
            get { return _dataWrittenToCurrentFile; }
        }

        [JsonIgnore]
        public long TotalDataBuffered
        {
            get { return _totalDataBuffered; }
        }

        [JsonIgnore]
        public long TotalDataWritten
        {
            get { return _totalDataWritten; }
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
            _storeBuffer = new List<Datum>();
            _toWriteBuffer = new List<Datum>();
            _checkForBufferedData = new AutoResetEvent(false);
            _finishedCheckingForBufferedData = new AutoResetEvent(false);
            _compressionLevel = CompressionLevel.Optimal;
            _bufferSizeBytes = DEFAULT_BUFFER_SIZE_BYTES;
            _encrypt = false;
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

            _totalDataBuffered = 0;
            _totalDataWritten = 0;
            _dataWrittenToCurrentFile = 0;
            _filesOpened = 0;
            _filesClosed = 0;
            _filesPromoted = 0;
            _filesWrittenToRemote = 0;

            OpenFile();
        }

        private void OpenFile()
        {
            lock (_locker)
            {
                // it's possible to stop the data store before entering this lock.
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
                        _dataWrittenToCurrentFile = 0;
                        _filesOpened++;
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

        public override void WriteDatum(Datum datum, CancellationToken cancellationToken)
        {
            if (!Running)
            {
                return;
            }

            lock (_storeBuffer)
            {
                _storeBuffer.Add(datum);
                _totalDataBuffered++;
                _checkForBufferedData.Set();

                // start the long-running task for writing data to file. also check the status of the task after 
                // it has been created and restart the task if it stops for some reason.
                if (_writeToFileTask == null ||
                    _writeToFileTask.Status == TaskStatus.Canceled ||
                    _writeToFileTask.Status == TaskStatus.Faulted ||
                    _writeToFileTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeToFileTask = Task.Run(async () =>
                    {
                        try
                        {
                            while (Running)
                            {
                                // wait for the signal to check for and write data
                                _checkForBufferedData.WaitOne();

                                bool checkSize = false;

                                // be sure to acquire the locks in the same order as done in Flush.
                                lock (_toWriteBuffer)
                                {
                                    // copy the current data to the buffer to write, and clear the current buffer.
                                    lock (_storeBuffer)
                                    {
                                        _toWriteBuffer.AddRange(_storeBuffer);
                                        _storeBuffer.Clear();
                                    }

                                    // write each datum
                                    for (int i = 0; i < _toWriteBuffer.Count;)
                                    {
                                        Datum datumToWrite = _toWriteBuffer[i];

                                        bool datumWritten = false;

                                        // lock the file so that we can safely write the current datum to it
                                        lock (_locker)
                                        {
                                            // it's possible to stop the datastore before entering this lock, in which case we won't
                                            // have a file to write to. check for a running data store here.
                                            if (_file != null)
                                            {
                                                #region write JSON for datum to file
                                                string datumJSON = null;
                                                try
                                                {
                                                    datumJSON = datumToWrite.GetJSON(Protocol.JsonAnonymizer, false);
                                                }
                                                catch (Exception ex)
                                                {
                                                    SensusException.Report("Failed to get JSON for datum.", ex);
                                                }

                                                if (datumJSON != null)
                                                {
                                                    try
                                                    {
                                                        byte[] datumJsonBytes = Encoding.UTF8.GetBytes((_dataWrittenToCurrentFile == 0 ? "" : ",") + Environment.NewLine + datumJSON);
                                                        _file.Write(datumJsonBytes, 0, datumJsonBytes.Length);
                                                        _dataWrittenToCurrentFile++;
                                                        _totalDataWritten++;
                                                        datumWritten = true;

                                                        if (_dataWrittenToCurrentFile % DATA_WRITES_PER_SIZE_CHECK == 0)
                                                        {
                                                            checkSize = true;
                                                        }
                                                    }
                                                    catch (Exception writeException)
                                                    {
                                                        SensusException.Report("Failed to write datum JSON bytes to file.", writeException);

                                                        #region something went wrong with file write...switch to a new file in the hope that it will work better.
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
                                                        #endregion
                                                    }
                                                }
                                                #endregion
                                            }
                                        }

                                        if (datumWritten)
                                        {
                                            _toWriteBuffer.RemoveAt(i);
                                        }
                                        else
                                        {
                                            i++;
                                        }
                                    }
                                }

                                #region periodically check the size of the current file and the entire local data store
                                if (checkSize)
                                {
                                    // must do the check within the lock, since other callers might be trying to close the file and null the path.
                                    lock (_locker)
                                    {
                                        if (SensusServiceHelper.GetFileSizeMB(_path) >= MAX_FILE_SIZE_MB)
                                        {
                                            CloseFile();
                                            OpenFile();
                                        }
                                    }

                                    // write the local data to remote if the overall size has grown too large
                                    await WriteToRemoteIfTooLargeAsync(cancellationToken);
                                }
                                #endregion

                                _finishedCheckingForBufferedData.Set();
                            }
                        }
                        catch (Exception taskException)
                        {
                            SensusException.Report("Local data store write task threw exception.", taskException);
                        }
                    });
                }
            }
        }

        public void Flush()
        {
            while (true)
            {
                bool buffersEmpty;

                // check for data in any of the buffers. be sure to acquire the locks in the same order as done in WriteDatum.
                lock (_toWriteBuffer)
                {
                    lock (_storeBuffer)
                    {
                        buffersEmpty = _storeBuffer.Count == 0 && _toWriteBuffer.Count == 0;
                    }
                }

                if (buffersEmpty)
                {
                    // flush any bytes from the underlying file stream
                    lock (_locker)
                    {
                        _file?.Flush();
                    }

                    break;
                }
                else
                {
                    _checkForBufferedData.Set();
                    _finishedCheckingForBufferedData.WaitOne();
                }
            }
        }


        // TODO remove arguments
        public override void CreateTarFromLocalData()
        {
            // reusing same lock, I assume there is a possibility of conflict between function behaviors

            lock (_locker)
            {
                string[] promotedPaths = PromotedPaths; // TODO clean up these assignments

                string tarOutFn = SensusServiceHelper.Get().GetSharePath(".tar");
                Stream outStream = File.Create(tarOutFn);

                TarOutputStream tarOutputStream = new TarOutputStream(outStream);

                foreach (string filename in promotedPaths)
                {
                    
                    using (Stream inputStream = File.OpenRead(filename))
                    {

                        long fileSize = inputStream.Length;
                        TarEntry entry = TarEntry.CreateTarEntry(filename);

                        // Must set size, otherwise TarOutputStream will fail when output exceeds.
                        entry.Size = fileSize;

                        // Add the entry to the tar stream, before writing the data.
                        tarOutputStream.PutNextEntry(entry);

                        // this is copied from TarArchive.WriteEntryCore
                        byte[] localBuffer = new byte[32 * 1024];
                        while (true)
                        {
                            int numRead = inputStream.Read(localBuffer, 0, localBuffer.Length);
                            if (numRead <= 0)
                            {
                                break;
                            }
                            tarOutputStream.Write(localBuffer, 0, numRead);
                        }
                    }

                    tarOutputStream.CloseEntry();

                }
            }
        }

        protected override bool IsTooLarge()
        {
            lock (_locker)
            {
                return SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) >= REMOTE_WRITE_TRIGGER_STORAGE_DIRECTORY_SIZE_MB;
            }
        }

        public override Task WriteToRemoteAsync(CancellationToken cancellationToken)
        {
            // block writes to remote if the boolean is flipped off
            if (!_writeToRemote)
            {
                return Task.CompletedTask;
            }

            lock (_locker)
            {
                // it's possible to stop the datastore before entering this lock.
                if (!Running)
                {
                    return Task.CompletedTask;
                }

                string[] promotedPaths = PromotedPaths;

                // if no paths were promoted, then we have nothing to do.
                if (promotedPaths.Length == 0)
                {
                    return Task.CompletedTask;
                }

                // if this is the first write or the previous write is finished, run a new task.
                if (_writeToRemoteTask == null ||
                    _writeToRemoteTask.Status == TaskStatus.Canceled ||
                    _writeToRemoteTask.Status == TaskStatus.Faulted ||
                    _writeToRemoteTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeToRemoteTask = Task.Run(async () =>
                    {
                        // write each promoted file
                        for (int i = 0; i < promotedPaths.Length; ++i)
                        {
#if __IOS__
                            CaptionText = "Uploading file " + (i + 1) + " of " + promotedPaths.Length + ". Please keep Sensus open...";
#endif

                            string promotedPath = promotedPaths[i];

                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            // wrap in try-catch to ensure that we process all files
                            try
                            {
                                // get stream name and content type
                                string streamName = System.IO.Path.GetFileName(promotedPath);
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

                                using (FileStream fileToWrite = new FileStream(promotedPath, FileMode.Open, FileAccess.Read))
                                {
                                    await Protocol.RemoteDataStore.WriteDataStreamAsync(fileToWrite, streamName, streamContentType, cancellationToken);
                                }

                                // file was written remotely. delete it locally, and do this within a lock to prevent concurrent access
                                // by the code that checks the size of the data store.
                                lock (_locker)
                                {
                                    File.Delete(promotedPath);
                                }

                                _filesWrittenToRemote++;
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to write file:  " + ex, LoggingLevel.Normal, GetType());
                            }
                        }

                        CaptionText = null;

                    }, cancellationToken);
                }

                return _writeToRemoteTask;
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
                        _filesClosed++;
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

                        string promotedPath = path + JSON_FILE_EXTENSION;

                        if (_compressionLevel != CompressionLevel.NoCompression)
                        {
                            promotedPath += GZIP_FILE_EXTENSION;
                        }

                        if (_encrypt)
                        {
                            promotedPath += ENCRYPTED_FILE_EXTENSION;
                            Protocol.AsymmetricEncryption.EncryptSymmetrically(File.ReadAllBytes(path), ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, promotedPath);
                            File.Delete(path);
                        }
                        else
                        {
                            File.Move(path, promotedPath);
                        }

                        _filesPromoted++;
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
            // flush any remaining data to disk.
            Flush();

            // stop the data store. it could very well be that someone attempts to add additional data 
            // following the flush and prior to stopping. these data will be lost.
            base.Stop();

            // the data stores state is stopped, but the file write task will still be running if the
            // condition in its while-loop hasn't been checked. to ensure that this condition is checked, 
            // signal the long-running write task to check for data, and wait for the task to finish.
            _checkForBufferedData.Set();
            _writeToFileTask.Wait();

            lock (_storeBuffer)
            {
                _storeBuffer.Clear();
            }

            lock (_toWriteBuffer)
            {
                _toWriteBuffer.Clear();
            }

            lock (_locker)
            {
                CloseFile();
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

        public override bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = base.TestHealth(ref events);

            double storageDirectorySizeMB;
            lock (_locker)
            {
                storageDirectorySizeMB = SensusServiceHelper.GetDirectorySizeMB(StorageDirectory);
            }

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Percent Buffer Written", Convert.ToString(_totalDataWritten.RoundToWholePercentageOf(_totalDataBuffered, 5)) },
                { "Percent Closed", Convert.ToString(_filesClosed.RoundToWholePercentageOf(_filesOpened, 5)) },
                { "Percent Promoted", Convert.ToString(_filesPromoted.RoundToWholePercentageOf(_filesClosed, 5)) },
                { "Percent Written", Convert.ToString(_filesWrittenToRemote.RoundToWholePercentageOf(_filesPromoted, 5)) },
                { "Size MB", Convert.ToString(Math.Round(storageDirectorySizeMB, 0)) }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));

            return restart;
        }
    }
}