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
    /// supports encryption prior to transmission to a <see cref="Remote.RemoteDataStore"/>.
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

        /// <summary>
        /// First stop for data:  An in-memory buffer.
        /// </summary>
        private List<Datum> _dataBuffer;
        private long _totalDataBuffered;
        private AutoResetEvent _dataHaveBeenBuffered;

        /// <summary>
        /// Next stop for data:  A (possibly compressed) file on disk. Such files are referred to as unpromoted.
        /// </summary>
        private Task _writeBufferedDataToFileTask;
        private AutoResetEvent _bufferedDataHaveBeenWrittenToFile;
        private List<Datum> _toWriteBuffer;
        private string _currentPath;
        private BufferedStream _currentFile;
        private CompressionLevel _compressionLevel;
        private int _bufferSizeBytes;
        private long _dataWrittenToCurrentFile;
        private long _totalDataWritten;
        private int _filesOpened;
        private int _filesClosed;
        private List<string> _unpromotedPaths;

        private readonly object _fileLocker = new object();

        /// <summary>
        /// Next stop for data:  An encrypted file on disk. Such files are referred to as promoted.
        /// </summary>
        private Task _promoteFilesTask;
        private bool _encrypt;
        private int _filesPromoted;
        private List<string> _promotedPaths;

        private readonly object _promoteFilesTaskLocker = new object();

        /// <summary>
        /// Next stop for data:  The <see cref="Remote.RemoteDataStore"/>.
        /// </summary>
        private Task _writeToRemoteTask;
        private int _filesWrittenToRemote;

        private readonly object _writeToRemoteTaskLocker = new object();

        public override bool HasDataToShare
        {
            get
            {
                // we'll only have a collection of paths after the data store has started.
                if (_promotedPaths == null)
                {
                    return false;
                }
                else
                {
                    lock (_promotedPaths)
                    {
                        return _promotedPaths.Count > 0;
                    }
                }
            }
        }

#if UNIT_TEST
        /// <summary>
        /// Gets the file path currently being written.
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
        public string CurrentPath
        {
            get { return _currentPath; }
        }
#endif

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

        [JsonIgnore]
        public string StorageDirectory
        {
            get
            {
                string directory = Path.Combine(Protocol.StorageDirectory, GetType().FullName);

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
            _dataBuffer = new List<Datum>();
            _toWriteBuffer = new List<Datum>();
            _dataHaveBeenBuffered = new AutoResetEvent(false);
            _bufferedDataHaveBeenWrittenToFile = new AutoResetEvent(false);
            _compressionLevel = CompressionLevel.Optimal;
            _bufferSizeBytes = DEFAULT_BUFFER_SIZE_BYTES;
            _encrypt = false;
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            // ensure that we have a valid encryption setup if one is requested
            if (_encrypt)
            {
                try
                {
                    using (MemoryStream testStream = new MemoryStream())
                    {
                        await Protocol.EnvelopeEncryptor.EnvelopeAsync(Encoding.UTF8.GetBytes("testing"), ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, testStream, CancellationToken.None);
                    }
                }
                catch (Exception encryptionTestException)
                {
                    throw new Exception("Envelope encryption test failed:  " + encryptionTestException.Message);
                }
            }

            // get all unpromoted file paths. unpromoted file paths are those without an extension.
            _unpromotedPaths = Directory.GetFiles(StorageDirectory).Where(path => string.IsNullOrWhiteSpace(Path.GetExtension(path))).ToList();

            // get all promoted file paths based on selected options. promoted files are those with an extension (.json, .gz, or .bin).
            string promotedPathExtension = JSON_FILE_EXTENSION + (_compressionLevel == CompressionLevel.NoCompression ? "" : GZIP_FILE_EXTENSION) + (_encrypt ? ENCRYPTED_FILE_EXTENSION : "");
            _promotedPaths = Directory.GetFiles(StorageDirectory, "*" + promotedPathExtension).ToList();

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
            lock (_fileLocker)
            {
                // it's possible to stop the data store before entering this lock.
                if (!Running)
                {
                    return;
                }

                // try a few times to open a new file within the storage directory
                _currentPath = null;
                Exception mostRecentException = null;
                for (int i = 0; _currentPath == null && i < 5; ++i)
                {
                    try
                    {
                        _currentPath = Path.Combine(StorageDirectory, Guid.NewGuid().ToString());
                        Stream file = new FileStream(_currentPath, FileMode.CreateNew, FileAccess.Write);

                        // add gzip stream if doing compression
                        if (_compressionLevel != CompressionLevel.NoCompression)
                        {
                            file = new GZipStream(file, _compressionLevel, false);
                        }

                        // use buffering for compression and runtime performance
                        _currentFile = new BufferedStream(file, _bufferSizeBytes);
                        _dataWrittenToCurrentFile = 0;
                        _filesOpened++;
                    }
                    catch (Exception ex)
                    {
                        mostRecentException = ex;
                        _currentPath = null;
                    }
                }

                // we could not open a file to write, so we cannot proceed. report the most recent exception and bail.
                if (_currentPath == null)
                {
                    throw SensusException.Report("Failed to open file for local data store.", mostRecentException);
                }
                else
                {
                    // open the JSON array
                    byte[] jsonBeginArrayBytes = Encoding.UTF8.GetBytes("[");
                    _currentFile.Write(jsonBeginArrayBytes, 0, jsonBeginArrayBytes.Length);
                }
            }
        }

        public override void WriteDatum(Datum datum, CancellationToken cancellationToken)
        {
            if (!Running)
            {
                return;
            }

            lock (_dataBuffer)
            {
                _dataBuffer.Add(datum);
                _totalDataBuffered++;
                _dataHaveBeenBuffered.Set();

                // start the long-running task for writing data to file. also check the status of the task after 
                // it has been created and restart the task if it stops due to cancellation, fault, or completion.
                if (_writeBufferedDataToFileTask == null ||
                    _writeBufferedDataToFileTask.Status == TaskStatus.Canceled ||
                    _writeBufferedDataToFileTask.Status == TaskStatus.Faulted ||
                    _writeBufferedDataToFileTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeBufferedDataToFileTask = Task.Run(async () =>
                    {
                        try
                        {
                            while (Running)
                            {
                                // wait for the signal to check for and write buffered data
                                _dataHaveBeenBuffered.WaitOne();

                                bool checkSize = false;

                                // be sure to acquire the locks in the same order as done in Flush.
                                lock (_toWriteBuffer)
                                {
                                    // copy the current data to the buffer to write, and clear the current buffer. we use
                                    // the intermediary buffer to free up the data buffer lock as quickly as possible for
                                    // callers to WriteDatum. all probes call WriteDatum, so we need to accommodate 
                                    // potentially hundreds of samples per second.
                                    lock (_dataBuffer)
                                    {
                                        _toWriteBuffer.AddRange(_dataBuffer);
                                        _dataBuffer.Clear();
                                    }

                                    // write each datum from the intermediary buffer to disk.
                                    for (int i = 0; i < _toWriteBuffer.Count;)
                                    {
                                        Datum datumToWrite = _toWriteBuffer[i];

                                        bool datumWritten = false;

                                        lock (_fileLocker)
                                        {
                                            // it's possible to stop the datastore and dispose the file before entering this lock, in 
                                            // which case we won't have a file to write to. check the file.
                                            if (_currentFile != null)
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
                                                        _currentFile.Write(datumJsonBytes, 0, datumJsonBytes.Length);
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
                                    lock (_fileLocker)
                                    {
                                        if (SensusServiceHelper.GetFileSizeMB(_currentPath) >= MAX_FILE_SIZE_MB)
                                        {
                                            CloseFile();
                                            OpenFile();
                                        }
                                    }

                                    // write the local data to remote if the overall size has grown too large
                                    await WriteToRemoteIfTooLargeAsync(cancellationToken);
                                }
                                #endregion

                                _bufferedDataHaveBeenWrittenToFile.Set();
                            }
                        }
                        catch (Exception taskException)
                        {
                            SensusException.Report("Local data store write task threw exception:  " + taskException.Message, taskException);
                        }
                    });
                }
            }
        }

        public void Flush()
        {
            // there's a race condition between writing new data to the buffers and flushing them. enter an
            // infinite loop that terminates when all buffers are empty and the file stream has been flushed.
            while (true)
            {
                bool buffersEmpty;

                // check for data in any of the buffers. be sure to acquire the locks in the same order as done in WriteDatum.
                lock (_toWriteBuffer)
                {
                    lock (_dataBuffer)
                    {
                        buffersEmpty = _dataBuffer.Count == 0 && _toWriteBuffer.Count == 0;
                    }
                }

                if (buffersEmpty)
                {
                    // flush any bytes from the underlying file stream
                    lock (_fileLocker)
                    {
                        _currentFile?.Flush();
                    }

                    break;
                }
                else
                {
                    _dataHaveBeenBuffered.Set();
                    _bufferedDataHaveBeenWrittenToFile.WaitOne();
                }
            }
        }

        public override async Task CreateTarFromLocalDataAsync(string outputPath)
        {
            await PromoteFilesAsync(CancellationToken.None);

            lock (_promotedPaths)
            {
                if (_promotedPaths.Count == 0)
                {
                    throw new Exception("No data available.");
                }

                using (FileStream outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    using (TarArchive tarArchive = TarArchive.CreateOutputTarArchive(outputFile))
                    {
                        foreach (string promotedPath in _promotedPaths)
                        {
                            using (FileStream promotedFile = File.OpenRead(promotedPath))
                            {
                                TarEntry tarEntry = TarEntry.CreateEntryFromFile(promotedPath);
                                tarEntry.Name = "data/" + Path.GetFileName(promotedPath);
                                tarArchive.WriteEntry(tarEntry, false);

                                promotedFile.Close();
                            }
                        }

                        tarArchive.Close();
                    }

                    outputFile.Close();
                }
            }
        }

        protected override bool IsTooLarge()
        {
            return GetSizeMB() >= REMOTE_WRITE_TRIGGER_STORAGE_DIRECTORY_SIZE_MB;
        }

        private double GetSizeMB()
        {
            double fileSizeMB = 0;

            lock (_fileLocker)
            {
                fileSizeMB += SensusServiceHelper.GetFileSizeMB(_currentPath);
            }

            lock (_unpromotedPaths)
            {
                fileSizeMB += SensusServiceHelper.GetFileSizeMB(_unpromotedPaths.ToArray());
            }

            lock (_promotedPaths)
            {
                fileSizeMB += SensusServiceHelper.GetFileSizeMB(_promotedPaths.ToArray());
            }

            return fileSizeMB;
        }

        public override Task WriteToRemoteAsync(CancellationToken cancellationToken)
        {
            lock (_writeToRemoteTaskLocker)
            {
                // it's possible to stop the datastore before entering this lock.
                if (!Running || !WriteToRemote)
                {
                    return Task.CompletedTask;
                }

                // if this is the first write or the previous write completed due to cancellation, fault, or completion, then
                // run a new task. if a write-to-remote task is currently running, then return it to the caller instead.
                if (_writeToRemoteTask == null ||
                    _writeToRemoteTask.Status == TaskStatus.Canceled ||
                    _writeToRemoteTask.Status == TaskStatus.Faulted ||
                    _writeToRemoteTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeToRemoteTask = Task.Run(async () =>
                    {
                        await PromoteFilesAsync(cancellationToken);

                        string[] promotedPaths;
                        lock (_promotedPaths)
                        {
                            promotedPaths = _promotedPaths.ToArray();
                        }

                        // if no paths were promoted, then we have nothing to do.
                        if (promotedPaths.Length == 0)
                        {
                            return;
                        }

                        // write each promoted file
                        for (int i = 0; i < promotedPaths.Length && !cancellationToken.IsCancellationRequested; ++i)
                        {
#if __IOS__
                            CaptionText = "Uploading file " + (i + 1) + " of " + promotedPaths.Length + ". Please keep Sensus open...";
#endif

                            string promotedPath = promotedPaths[i];

                            // wrap in try-catch to ensure that we process all files
                            try
                            {
                                // get stream name and content type
                                string streamName = Path.GetFileName(promotedPath);
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

                                using (FileStream promotedFile = new FileStream(promotedPath, FileMode.Open, FileAccess.Read))
                                {
                                    await Protocol.RemoteDataStore.WriteDataStreamAsync(promotedFile, streamName, streamContentType, cancellationToken);
                                }

                                // file was written remotely. delete it locally, and do this within a lock to prevent concurrent access
                                // by the code that checks the size of the data store.
                                lock (_promotedPaths)
                                {
                                    File.Delete(promotedPath);
                                    _promotedPaths.Remove(promotedPath);
                                }

                                _filesWrittenToRemote++;
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to write file to remote data store:  " + ex, LoggingLevel.Normal, GetType());
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
            lock (_fileLocker)
            {
                if (_currentFile != null)
                {
                    try
                    {
                        // end the JSON array and close the file
                        byte[] jsonEndArrayBytes = Encoding.UTF8.GetBytes(Environment.NewLine + "]");
                        _currentFile.Write(jsonEndArrayBytes, 0, jsonEndArrayBytes.Length);
                        _currentFile.Flush();
                        _currentFile.Close();
                        _currentFile.Dispose();

                        // add to our list of unpromoted files
                        lock (_unpromotedPaths)
                        {
                            _unpromotedPaths.Add(_currentPath);
                        }

                        _currentFile = null;
                        _currentPath = null;
                        _filesClosed++;
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Failed to close and dispose the local file.", ex);
                    }
                }
            }
        }

        private Task PromoteFilesAsync(CancellationToken cancellationToken)
        {
            lock (_promoteFilesTaskLocker)
            {
                lock (_fileLocker)
                {
                    // close the current file, as we're about to delete/move all files in the storage directory
                    // that do not have a file extension. the file currently being written is one such file, and 
                    // we'll get an exception if we don't close it before moving it. if there is no current file
                    // this will have no effect.
                    CloseFile();

                    // open a new file for writing if the data store is currently running. we're going to return
                    // immediately after checking/spawning the promotion task below, and there needs to be a file
                    // ready as soon as we release the current lock. if the data store is not running, then we 
                    // shouldn't open a new file as it will likely never be closed.
                    if (Running)
                    {
                        OpenFile();
                    }
                }

                // if this is the first promote or the previous promote completed due to cancellation, fault, or completion, then
                // run a new task. if a promote task is currently running, then return it to the caller instead.
                if (_promoteFilesTask == null ||
                    _promoteFilesTask.Status == TaskStatus.Canceled ||
                    _promoteFilesTask.Status == TaskStatus.Faulted ||
                    _promoteFilesTask.Status == TaskStatus.RanToCompletion)
                {
                    _promoteFilesTask = Task.Run(async () =>
                    {
                        // get a copied list of all files that have been closed but not promoted.
                        string[] unpromotedPaths;
                        lock (_unpromotedPaths)
                        {
                            unpromotedPaths = _unpromotedPaths.ToArray();
                        }

                        // promote each file
                        foreach (string unpromotedPath in unpromotedPaths)
                        {
                            try
                            {
                                string promotedPath = unpromotedPath + JSON_FILE_EXTENSION;

                                if (_compressionLevel != CompressionLevel.NoCompression)
                                {
                                    promotedPath += GZIP_FILE_EXTENSION;
                                }

                                if (_encrypt)
                                {
                                    promotedPath += ENCRYPTED_FILE_EXTENSION;

                                    // the target promoted path should not currently exist. if it does, then delete it since we're about to write to it.
                                    if (File.Exists(promotedPath))
                                    {
                                        File.Delete(promotedPath);
                                    }

                                    using (FileStream promotedFile = new FileStream(promotedPath, FileMode.CreateNew, FileAccess.Write))
                                    {
                                        await Protocol.EnvelopeEncryptor.EnvelopeAsync(File.ReadAllBytes(unpromotedPath), ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, promotedFile, cancellationToken);
                                    }
                                }
                                else
                                {
                                    // the target promoted path should not currently exist. if it does, then delete it since we're about to copy the current file to it.
                                    if (File.Exists(promotedPath))
                                    {
                                        File.Delete(promotedPath);
                                    }

                                    // we were previously using File.Move, but we were getting many sharing violation errors
                                    // when doing so. looks like some folks have seen the same problem, and one person fixed 
                                    // the issue by using a copy followed by delete:  https://forums.xamarin.com/discussion/42145/android-sharing-violation-on-file-rename
                                    File.Copy(unpromotedPath, promotedPath);
                                }

                                // file has been promoted. delete the file and remove it from the list.
                                lock (_unpromotedPaths)
                                {
                                    File.Delete(unpromotedPath);
                                    _unpromotedPaths.Remove(unpromotedPath);
                                }

                                // add to the list of promoted paths.
                                lock (_promotedPaths)
                                {
                                    _promotedPaths.Add(promotedPath);
                                }

                                _filesPromoted++;
                            }
                            catch (Exception ex)
                            {
                                SensusException.Report("Failed to promote file:  " + ex.Message, ex);
                            }
                        }
                    });
                }

                return _promoteFilesTask;
            }
        }

        public override async Task StopAsync()
        {
            // flush any remaining data to disk.
            Flush();

            // stop the data store. it could very well be that someone attempts to add additional data 
            // following the flush and prior to stopping. these data will be lost.
            await base.StopAsync();

            // the data stores state is stopped, but the file write task will still be running if the
            // condition in its while-loop hasn't been checked. to ensure that this condition is checked, 
            // signal the long-running write task to check for data, and wait for the task to finish.
            _dataHaveBeenBuffered.Set();
            await (_writeBufferedDataToFileTask ?? Task.CompletedTask);  // if no data have been written, then there will not yet be a task.

            lock (_dataBuffer)
            {
                _dataBuffer.Clear();
            }

            lock (_toWriteBuffer)
            {
                _toWriteBuffer.Clear();
            }

            // promote any existing files. this will encrypt any files if needed and ready them
            // for local sharing. a new file will not be opened because the data store has already
            // been stopped above.
            await PromoteFilesAsync(CancellationToken.None);

            _dataWrittenToCurrentFile = 0;
        }

        public void Clear()
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

        public override void Reset()
        {
            base.Reset();

            _currentPath = null;
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Percent Buffer Written", Convert.ToString(_totalDataWritten.RoundToWholePercentageOf(_totalDataBuffered, 5)) },
                { "Percent Closed", Convert.ToString(_filesClosed.RoundToWholePercentageOf(_filesOpened, 5)) },
                { "Percent Promoted", Convert.ToString(_filesPromoted.RoundToWholePercentageOf(_filesClosed, 5)) },
                { "Percent Written", Convert.ToString(_filesWrittenToRemote.RoundToWholePercentageOf(_filesPromoted, 5)) },
                { "Unpromoted Count", Convert.ToString(_unpromotedPaths == null ? 0 : _unpromotedPaths.Count) },
                { "Promoted Count", Convert.ToString(_promotedPaths == null ? 0 : _promotedPaths.Count) },
                { "Size MB", Convert.ToString(Math.Round(GetSizeMB(), 0)) }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return result;
        }
    }
}