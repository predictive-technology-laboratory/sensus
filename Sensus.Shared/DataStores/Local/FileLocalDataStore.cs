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
using System.Net;

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
        private const double MAX_FILE_SIZE_MB = 5;

        /// <summary>
        /// File-based storage uses a <see cref="BufferedStream"/> for efficiency. This is the default buffer size in bytes.
        /// </summary>
        private const int DEFAULT_BUFFER_SIZE_BYTES = 4096;

        /// <summary>
        /// This is a default maximum local data storage that can be used before we start deleting the oldest data
        /// </summary>
        private const int DEFAULT_MAX_LOCAL_DATA_SIZE_MB = 2048;

        /// <summary>
        /// This is the default free storage percent that we maintain on the device before we start deleting the oldest data
        /// </summary>
        private const double DEFAULT_MIN_DEVICE_FREE_STORAGE_PERCENT = .20;

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
        /// Step 1:  An in-memory buffer that can be written at high rates.
        /// </summary>
        private List<Datum> _dataBuffer;
        private AutoResetEvent _dataHaveBeenBuffered;
        private long _totalDataBuffered;

        /// <summary>
        /// Step 2:  A file on disk without a file extension, written with data from the in-memory buffer.
        /// </summary>
        private string _currentPath;
        private BufferedStream _currentFile;
        private int _currentFileBufferSizeBytes;
        private Task _writeBufferedDataToFileTask;
        private List<Datum> _toWriteBuffer;
        private AutoResetEvent _bufferedDataHaveBeenWrittenToFile;
        private long _totalDataWrittenToCurrentFile;
        private long _totalDataWritten;
        private int _totalFilesOpened;
        private int _totalFilesClosed;
        private readonly object _fileLocker = new object();

        /// <summary>
        /// Step 3:  A compressed, encrypted file on disk with a file extension. These files are ready for transmission to the <see cref="Remote.RemoteDataStore"/>.
        /// </summary>
        private CompressionLevel _compressionLevel;
        private bool _encrypt;
        private List<string> _pathsPreparedForRemote;
        private List<string> _pathsUnpreparedForRemote;
        private int _totalFilesPreparedForRemote;
        private int _maxLocalDataSize;
        private double _minDeviceFreeStoragePercent;

        /// <summary>
        /// Step 4:  Transmission to the <see cref="Remote.RemoteDataStore"/>.
        /// </summary>
        private Task _writeToRemoteTask;
        private int _totalFilesWrittenToRemote;
        private readonly object _writeToRemoteTaskLocker = new object();

        public override bool HasDataToShare
        {
            get
            {
                lock (_pathsPreparedForRemote)
                {
                    UpdatePathsPreparedForRemote();
                    return _pathsPreparedForRemote.Count > 0;
                }
            }
        }

#if UNIT_TEST
        [JsonIgnore]
        public string CurrentPath
        {
            get { return _currentPath; }
        }

        [JsonIgnore]
        public List<string> PathsPreparedForRemote
        {
            get { return _pathsPreparedForRemote; }
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
            get { return _currentFileBufferSizeBytes; }
            set
            {
                if (value <= 0)
                {
                    value = DEFAULT_BUFFER_SIZE_BYTES;
                }

                _currentFileBufferSizeBytes = value;
            }
        }

        [EntryIntegerUiProperty("Maximum local storage size in MB", true, 3, true)]
        public int MaxLocalDataSize
        {
            get
            {
                return _maxLocalDataSize;
            }
            set
            {
                if (value < _currentFileBufferSizeBytes / 1024)
                {
                    _maxLocalDataSize = _currentFileBufferSizeBytes / 1024; //this should not be smaller than the buffer size
                }
                else
                {
                    _maxLocalDataSize = value;
                }
            }
        }

        [EntryDoubleUiProperty("Minimum available device storage in Percent (0.00-1.00)", true, 4, true)]
        public double MinDeviceFreeStoragePercent
        {
            get
            {
                return _minDeviceFreeStoragePercent;
            }
            set
            {
                if (value < 0)
                {
                    _minDeviceFreeStoragePercent = 0.0;
                }
                else if (value > 1)
                {
                    _minDeviceFreeStoragePercent = 1.0;
                }
                else
                {
                    _minDeviceFreeStoragePercent = value;
                }
            }
        }

        /// <summary>
        /// Whether or not to apply asymmetric-key encryption to data. If this is enabled, then you must either provide a public encryption 
        /// key to <see cref="Protocol.AsymmetricEncryptionPublicKey"/> or use an [authentication server](xref:authentication_servers). 
        /// You can generate a public encryption key following the instructions provided for 
        /// <see cref="Protocol.AsymmetricEncryptionPublicKey"/>. Note that data are not encrypted immediately. They are first
        /// written to disk on the device where they live unencrypted for a period of time.
        /// </summary>
        /// <value><c>true</c> to encrypt; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Encrypt:", true, 6)]
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
        public long TotalDataWrittenToCurrentFile
        {
            get { return _totalDataWrittenToCurrentFile; }
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
            // step 1:  buffer
            _dataBuffer = new List<Datum>();
            _dataHaveBeenBuffered = new AutoResetEvent(false);

            // step 2:  file
            _currentPath = null;
            _currentFile = null;
            _currentFileBufferSizeBytes = DEFAULT_BUFFER_SIZE_BYTES;
            _writeBufferedDataToFileTask = null;
            _toWriteBuffer = new List<Datum>();
            _bufferedDataHaveBeenWrittenToFile = new AutoResetEvent(false);
            _maxLocalDataSize = DEFAULT_MAX_LOCAL_DATA_SIZE_MB;
            _minDeviceFreeStoragePercent = DEFAULT_MIN_DEVICE_FREE_STORAGE_PERCENT;

            // step 3:  compressed, encrypted file
            _compressionLevel = CompressionLevel.Optimal;
            _encrypt = false;
            _pathsPreparedForRemote = new List<string>();
            _pathsUnpreparedForRemote = new List<string>();

            // step 4:  remote data store
            _writeToRemoteTask = null;
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            // ensure that we have a valid encryption setup if one is requested
            if (_encrypt)
            {
                Exception exToThrow = null;

                try
                {
                    using (MemoryStream testStream = new MemoryStream())
                    {
                        await Protocol.EnvelopeEncryptor.EnvelopeAsync(Encoding.UTF8.GetBytes("testing"), ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, testStream, CancellationToken.None);
                    }
                }
                catch (WebException webException)
                {
                    // only throw the web exception if it was not caused by a connection failure. we'll get these under normal conditions.
                    if (webException.Status != WebExceptionStatus.ConnectFailure)
                    {
                        exToThrow = webException;
                    }
                }
                // throw all other exceptions
                catch (Exception ex)
                {
                    exToThrow = ex;
                }

                if (exToThrow != null)
                {
                    throw new Exception("Failed encryption test:  " + exToThrow.Message, exToThrow);
                }
            }

            UpdatePathsPreparedForRemote();

            lock (_pathsUnpreparedForRemote)
            {
                _pathsUnpreparedForRemote.Clear();
            }

            // process any paths that were written but not prepared for transfer to the remote data store. such 
            // paths can exist when the app crashes before a file is closed and prepared. these paths will be 
            // indicated by a lack of file extension.
            foreach (string pathUnpreparedForRemote in Directory.GetFiles(StorageDirectory).Where(path => string.IsNullOrWhiteSpace(Path.GetExtension(path))))
            {
                await PreparePathForRemoteAsync(pathUnpreparedForRemote, CancellationToken.None);
            }

            OpenFile();
        }

        private void UpdatePathsPreparedForRemote()
        {
            lock (_pathsPreparedForRemote)
            {
                _pathsPreparedForRemote.Clear();
                _pathsPreparedForRemote.AddRange(Directory.GetFiles(StorageDirectory).Where(path => !string.IsNullOrWhiteSpace(Path.GetExtension(path))));
            }
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
                for (int tryNum = 0; _currentPath == null && tryNum < 5; ++tryNum)
                {
                    try
                    {
                        _currentPath = Path.Combine(StorageDirectory, Guid.NewGuid().ToString());
                        _currentFile = new BufferedStream(new FileStream(_currentPath, FileMode.CreateNew, FileAccess.Write), _currentFileBufferSizeBytes);
                        _totalDataWrittenToCurrentFile = 0;
                        _totalFilesOpened++;
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
                    byte[] jsonOpenArrayBytes = Encoding.UTF8.GetBytes("[");
                    _currentFile.Write(jsonOpenArrayBytes, 0, jsonOpenArrayBytes.Length);
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
                                // wait for the signal to from data from the buffer to the file
                                _dataHaveBeenBuffered.WaitOne();

                                // write data. be sure to acquire locks in the same order as in Flush.
                                bool checkSize = false;
                                bool startNewFile = false;
                                lock (_toWriteBuffer)
                                {
                                    // copy the current data to the buffer to write, and clear the current buffer. we use
                                    // the intermediary buffer to free up the data buffer lock as quickly as possible for
                                    // callers to WriteDatum. all probes call WriteDatum, so we need to accommodate 
                                    // potentially hundreds of samples per second.
                                    lock (_dataBuffer)
                                    {
                                        for (var i = 0; i < 100000; i++)
                                        {
                                            _toWriteBuffer.AddRange(_dataBuffer);
                                        }
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
                                                        byte[] datumJsonBytes = Encoding.UTF8.GetBytes((_totalDataWrittenToCurrentFile == 0 ? "" : ",") + Environment.NewLine + datumJSON);
                                                        _currentFile.Write(datumJsonBytes, 0, datumJsonBytes.Length);
                                                        _totalDataWrittenToCurrentFile++;
                                                        _totalDataWritten++;
                                                        datumWritten = true;

                                                        // periodically check the size of the current file
                                                        if (_totalDataWrittenToCurrentFile % DATA_WRITES_PER_SIZE_CHECK == 0)
                                                        {
                                                            checkSize = true;
                                                        }
                                                    }
                                                    catch (Exception writeException)
                                                    {
                                                        SensusException.Report("Exception while writing datum JSON bytes to file:  " + writeException.Message, writeException);
                                                        startNewFile = true;
                                                        break;
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

                                if (checkSize)
                                {
                                    // must do the check within the lock, since other callers might be trying to close the file and null the path.
                                    lock (_fileLocker)
                                    {
                                        if (SensusServiceHelper.GetFileSizeMB(_currentPath) >= MAX_FILE_SIZE_MB)
                                        {
                                            startNewFile = true;
                                        }
                                    }
                                }

                                if (startNewFile)
                                {
                                    await StartNewFileAsync(cancellationToken);
                                }

                                if (checkSize)
                                {
                                    await WriteToRemoteIfTooLargeAsync(cancellationToken);
                                }

                                _bufferedDataHaveBeenWrittenToFile.Set();
                            }
                        }
                        catch (Exception writeTaskException)
                        {
                            SensusException.Report("Exception while writing buffered data to file:  " + writeTaskException.Message, writeTaskException);
                        }
                    });
                }
            }
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
                        await StartNewFileAsync(cancellationToken);

                        string[] pathsPreparedForRemote;
                        lock (_pathsPreparedForRemote)
                        {
                            UpdatePathsPreparedForRemote();
                            pathsPreparedForRemote = _pathsPreparedForRemote.ToArray();
                        }

                        // if no paths are prepared, then we have nothing to do.
                        if (pathsPreparedForRemote.Length == 0)
                        {
                            return;
                        }

                        // write each file that is prepared for transmission to the remote data store
                        for (int i = 0; i < pathsPreparedForRemote.Length && !cancellationToken.IsCancellationRequested; ++i)
                        {
                            CaptionText = "Uploading file " + (i + 1) + " of " + pathsPreparedForRemote.Length + ".";

#if __IOS__
                            // add encouragement to keep app open so that the upload may continue
                            CaptionText += " Please keep Sensus open...";
#endif

                            string pathPreparedForRemote = pathsPreparedForRemote[i];

                            // wrap in try-catch to ensure that we process all files
                            try
                            {
                                // get stream name and content type
                                string streamName = Path.GetFileName(pathPreparedForRemote);
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

                                using (FileStream filePreparedForRemote = new FileStream(pathPreparedForRemote, FileMode.Open, FileAccess.Read))
                                {
                                    await Protocol.RemoteDataStore.WriteDataStreamAsync(filePreparedForRemote, streamName, streamContentType, cancellationToken);
                                }

                                // file was written remotely. delete it locally, and do this within a lock to prevent concurrent access
                                // by the code that checks the size of the data store.
                                lock (_pathsPreparedForRemote)
                                {
                                    File.Delete(pathPreparedForRemote);
                                    _pathsPreparedForRemote.Remove(pathPreparedForRemote);
                                }

                                _totalFilesWrittenToRemote++;
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Exception while writing prepared file to remote data store:  " + ex, LoggingLevel.Normal, GetType());
                            }
                        }

                        CaptionText = null;

                    }, cancellationToken);
                }

                return _writeToRemoteTask;
            }
        }

        private async Task StartNewFileAsync(CancellationToken cancellationToken)
        {
            // start a new file within a lock to ensure that anyone with the lock will have a valid file
            string unpreparedPath = null;
            lock (_fileLocker)
            {
                try
                {
                    unpreparedPath = CloseFile();
                }
                catch (Exception closeException)
                {
                    SensusException.Report("Exception while closing file:  " + closeException.Message, closeException);
                }

                if (Running)
                {
                    try
                    {
                        OpenFile();
                    }
                    catch (Exception openException)
                    {
                        SensusException.Report("Exception while opening file:  " + openException.Message, openException);
                    }
                }
            }

            await PreparePathForRemoteAsync(unpreparedPath, cancellationToken);
        }

        public override void CreateTarFromLocalData(string outputPath)
        {
            lock (_pathsPreparedForRemote)
            {
                UpdatePathsPreparedForRemote();

                if (_pathsPreparedForRemote.Count == 0)
                {
                    throw new Exception("No data available.");
                }

                using (FileStream outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    using (TarArchive tarArchive = TarArchive.CreateOutputTarArchive(outputFile))
                    {
                        foreach (string pathPreparedForRemote in _pathsPreparedForRemote)
                        {
                            using (FileStream filePreparedForRemote = File.OpenRead(pathPreparedForRemote))
                            {
                                TarEntry tarEntry = TarEntry.CreateEntryFromFile(pathPreparedForRemote);
                                tarEntry.Name = "data/" + Path.GetFileName(pathPreparedForRemote);
                                tarArchive.WriteEntry(tarEntry, false);
                                filePreparedForRemote.Close();
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
            double sizeMB = 0;

            lock (_pathsPreparedForRemote)
            {
                sizeMB += SensusServiceHelper.GetFileSizeMB(_pathsPreparedForRemote.ToArray());
            }

            return sizeMB;
        }

        private double GetAvailablePercent()
        {
            return SensusServiceHelper.GetAvailableStoragePercent();
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
                    // ask the write task to write buffered data
                    _dataHaveBeenBuffered.Set();

                    // wait for buffered data to be written
                    _bufferedDataHaveBeenWrittenToFile.WaitOne();
                }
            }
        }

        private string CloseFile()
        {
            string path = _currentPath;

            lock (_fileLocker)
            {
                if (_currentFile != null)
                {
                    try
                    {
                        // close the JSON array and close the file
                        byte[] jsonCloseArrayBytes = Encoding.UTF8.GetBytes(Environment.NewLine + "]");
                        _currentFile.Write(jsonCloseArrayBytes, 0, jsonCloseArrayBytes.Length);
                        _currentFile.Flush();
                        _currentFile.Close();
                        _currentFile.Dispose();
                        _currentFile = null;
                        _currentPath = null;
                        _totalFilesClosed++;
                        _totalDataWrittenToCurrentFile = 0;
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Exception while closing file:  " + ex.Message, ex);
                    }
                }
            }

            return path;
        }

        private async Task PreparePathForRemoteAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);

                    string preparedPath = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid() + JSON_FILE_EXTENSION);

                    if (_compressionLevel != CompressionLevel.NoCompression)
                    {
                        preparedPath += GZIP_FILE_EXTENSION;

                        Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
                        MemoryStream compressedStream = new MemoryStream();
                        compressor.Compress(bytes, compressedStream, _compressionLevel);
                        bytes = compressedStream.ToArray();
                    }

                    if (_encrypt)
                    {
                        preparedPath += ENCRYPTED_FILE_EXTENSION;

                        using (FileStream preparedFile = new FileStream(preparedPath, FileMode.Create, FileAccess.Write))
                        {
                            await Protocol.EnvelopeEncryptor.EnvelopeAsync(bytes, ENCRYPTION_KEY_SIZE_BITS, ENCRYPTION_INITIALIZATION_KEY_SIZE_BITS, preparedFile, cancellationToken);
                        }
                    }
                    else
                    {
                        File.WriteAllBytes(preparedPath, bytes);
                    }

                    lock (_pathsPreparedForRemote)
                    {
                        _pathsPreparedForRemote.Add(preparedPath);
                    }

                    File.Delete(path);

                    _totalFilesPreparedForRemote++;
                }

                lock (_pathsUnpreparedForRemote)
                {
                    _pathsUnpreparedForRemote.Remove(path);
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while preparing path for remote:  " + ex.Message, LoggingLevel.Normal, GetType());

                lock (_pathsUnpreparedForRemote)
                {
                    if (!_pathsUnpreparedForRemote.Contains(path))
                    {
                        _pathsUnpreparedForRemote.Add(path);
                    }
                }
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

            await PreparePathForRemoteAsync(CloseFile(), CancellationToken.None);
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

                _totalDataBuffered = 0;
                _totalDataWrittenToCurrentFile = 0;
                _totalDataWritten = 0;
                _totalFilesOpened = 0;
                _totalFilesClosed = 0;
                _totalFilesPreparedForRemote = 0;
                _totalFilesWrittenToRemote = 0;
            }
        }

        private void ReduceFileSizeToLimits()
        {
            List<string> pathsUnpreparedForRemote;

            UpdatePathsPreparedForRemote();

            while (_pathsPreparedForRemote.Count > 1 && GetSizeMB() > _maxLocalDataSize) //TODO:  Should I use SensusServiceHelper.GetDirectorySizeMB();
            {
                DeleteOldestPreparedFile();
            }
            while (_pathsPreparedForRemote.Count > 1 && GetAvailablePercent() < _minDeviceFreeStoragePercent)
            {
                DeleteOldestPreparedFile();
            }

            UpdatePathsPreparedForRemote();

        }

        private void DeleteOldestPreparedFile()
        {
            lock (_pathsPreparedForRemote)
            {
                var toDelete = _pathsPreparedForRemote.LastOrDefault();
                System.IO.File.Delete(toDelete);
                _pathsPreparedForRemote.Remove(toDelete);
            }
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            // retry file preparation for any unprepared paths
            List<string> pathsUnpreparedForRemote;
            lock (_pathsUnpreparedForRemote)
            {
                pathsUnpreparedForRemote = _pathsUnpreparedForRemote.ToList();
            }

            foreach (string pathUnpreparedForRemote in pathsUnpreparedForRemote)
            {
                await PreparePathForRemoteAsync(pathUnpreparedForRemote, CancellationToken.None);
            }

            double startFileSize = GetSizeMB();
            double endFileSize = startFileSize;
            double availablePercentage = GetAvailablePercent();
            if (startFileSize > _maxLocalDataSize ||
                availablePercentage < _minDeviceFreeStoragePercent)
            {
                ReduceFileSizeToLimits();
                endFileSize = GetSizeMB();
                availablePercentage = GetAvailablePercent();
            }


            HealthTestResult result = await base.TestHealthAsync(events);

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Percent Buffer Written To File", Convert.ToString(_totalDataWritten.RoundToWholePercentageOf(_totalDataBuffered, 5)) },
                { "Percent Files Closed", Convert.ToString(_totalFilesClosed.RoundToWholePercentageOf(_currentPath == null ? _totalFilesOpened : _totalFilesOpened - 1, 5)) },  // don't count the currently open file in the denominator. we want the number to reflect the extent to which all files that should have been closed indeed were.
                { "Percent Closed Files Prepared For Remote", Convert.ToString(_totalFilesPreparedForRemote.RoundToWholePercentageOf(_totalFilesClosed, 5)) },
                { "Percent Closed Files Written To Remote", Convert.ToString(_totalFilesWrittenToRemote.RoundToWholePercentageOf(_totalFilesClosed, 5)) },
                { "Paths Unprepared For Remote", Convert.ToString(_pathsUnpreparedForRemote.Count) },
                { "Prepared Files Size MB", Convert.ToString(Math.Round(GetSizeMB(), 0)) },
                { "Maximum Files Size MB", Convert.ToString(_maxLocalDataSize) },
                { "Available Disk Space Percent",  Convert.ToString(Math.Round(availablePercentage, 3)) },
                { "Minimum Disk Space Percent",  Convert.ToString(Math.Round(_minDeviceFreeStoragePercent, 3)) },
                { "Amount Reduced MB", Convert.ToString(startFileSize-endFileSize) },
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return result;
        }
    }
}