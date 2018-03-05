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
using System.Security.Cryptography;
using Sensus.Encryption;
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
        private const int NUM_DATA_PER_SIZE_CHECK = 10000;

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
        /// File extension to use for GZip files.
        /// </summary>
        private const string GZIP_FILE_EXTENSION = ".gz";

        /// <summary>
        /// Files extension to use for encrypted files.
        /// </summary>
        private const string ENCRYPTED_FILE_EXTENSION = ".bin";

        private string _path;
        private BufferedStream _bufferedGzipStream;
        private CompressionLevel _compressionLevel;
        private int _bufferSizeBytes;
        private bool _encrypt;
        private Task _writeToRemoteTask;
        private long _numDataWritten;

        private readonly object _locker = new object();

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
        /// Gets or sets the buffer size in bytes.
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
        private string StorageDirectory
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
            _numDataWritten = 0;
        }

        public override void Start()
        {
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

            // file needs to be ready to accept data immediately, so open new file before calling base.Start
            SwitchToNewFile();

            base.Start();
        }

        public override Task<bool> WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                bool written = false;

                // only 1 write operation at once
                lock (_locker)
                {
                    // get anonymized JSON for datum
                    string datumJSON = null;
                    try
                    {
                        datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);
                    }
                    catch (Exception ex)
                    {
                        string message = "Failed to get JSON for datum:  " + ex.Message;
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        SensusException.Report(message, ex);
                    }

                    // write JSON to file
                    if (datumJSON != null)
                    {
                        try
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(datumJSON);
                            _bufferedGzipStream.Write(bytes, 0, bytes.Length);
                            written = true;
                        }
                        catch (Exception writeException)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to write datum JSON bytes to local file:  " + writeException.Message, LoggingLevel.Normal, GetType());

                            // something went wrong with file write...switch to a new file in the hope that it will work better
                            try
                            {
                                SwitchToNewFile();
                                SensusServiceHelper.Get().Logger.Log("Initialized new local file.", LoggingLevel.Normal, GetType());
                            }
                            catch (Exception newFileException)
                            {
                                SensusException.Report("Failed to initialize new file after failing to write the old one.", newFileException);
                            }
                        }
                    }
                }

                // every so often, check the size of the file and data store
                if ((++_numDataWritten % NUM_DATA_PER_SIZE_CHECK) == 0)
                {
                    // switch to a new file if the current one has grown too large
                    if (SensusServiceHelper.GetFileSizeMB(_path) >= MAX_FILE_SIZE_MB)
                    {
                        SwitchToNewFile();
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
                // if this is the first write or the previous write is finished, run a new task.
                if (_writeToRemoteTask == null ||
                    _writeToRemoteTask.Status == TaskStatus.Canceled ||
                    _writeToRemoteTask.Status == TaskStatus.Faulted ||
                    _writeToRemoteTask.Status == TaskStatus.RanToCompletion)
                {
                    _writeToRemoteTask = Task.Run(async () =>
                    {
                        // close/encrypt the current file and switch to a new one
                        SwitchToNewFile();

                        // get all completed .gz (compressed) or .gz.bin (compressed+encrypted) file paths
                        string[] pathsToWrite;
                        if (_encrypt)
                        {
                            pathsToWrite = Directory.GetFiles(StorageDirectory, "*" + GZIP_FILE_EXTENSION + ENCRYPTED_FILE_EXTENSION).ToArray();
                        }
                        else
                        {
                            pathsToWrite = Directory.GetFiles(StorageDirectory, "*" + GZIP_FILE_EXTENSION);
                        }

                        // write each file
                        foreach (string pathToWrite in pathsToWrite)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            // wrap in try-catch to ensure that we process all files
                            try
                            {
                                string name = Path.GetFileName(pathToWrite);
                                string contentType;
                                if (name.EndsWith(GZIP_FILE_EXTENSION))
                                {
                                    contentType = "application/gzip";
                                }
                                else if (name.EndsWith(ENCRYPTED_FILE_EXTENSION))
                                {
                                    contentType = "application/octet-stream";
                                }
                                else
                                {
                                    contentType = "application/octet-stream";
                                }

                                using (FileStream fileToWrite = new FileStream(pathToWrite, FileMode.Open, FileAccess.Read))
                                {
                                    await Protocol.RemoteDataStore.WriteDatumStreamAsync(fileToWrite, name, contentType, cancellationToken);
                                }

                                // file was written remotely. delete it locally.
                                File.Delete(pathToWrite);
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

        protected override bool TooLarge()
        {
            lock (_locker)
            {
                return SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) >= REMOTE_WRITE_TRIGGER_STORAGE_DIRECTORY_SIZE_MB;
            }
        }

        private void SwitchToNewFile()
        {
            lock (_locker)
            {
                // close the current file if there is one
                if (_bufferedGzipStream != null)
                {
                    try
                    {
                        _bufferedGzipStream.Flush();
                        _bufferedGzipStream.Dispose();

                        // add the .gz extension to the path, marking it as completed.
                        string gzPath = _path + GZIP_FILE_EXTENSION;
                        File.Move(_path, gzPath);
                        _path = gzPath;

                        // encrypt the file we just closed
                        if (_encrypt)
                        {
                            // apply symmetric-key encryption to the file, and send the symmetric key encrypted using the asymmetric public key
                            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                            {
                                // ensure that we generate a 32-byte symmetric key and 16-byte IV (IV length is block size / 8)
                                aes.KeySize = 256;
                                aes.BlockSize = 128;
                                aes.GenerateKey();
                                aes.GenerateIV();

                                // apply symmetric-key encryption to our file
                                SymmetricEncryption symmetricEncryption = new SymmetricEncryption(aes.Key, aes.IV);
                                byte[] encryptedFileBytes = symmetricEncryption.Encrypt(File.ReadAllBytes(_path));

                                // encrypt the symmetric key and initialization vector using our asymmetric public key
                                byte[] encryptedKeyBytes = Protocol.AsymmetricEncryption.Encrypt(aes.Key);
                                byte[] encryptedIVBytes = Protocol.AsymmetricEncryption.Encrypt(aes.IV);

                                // write the encrypted file
                                string encryptedPath = _path + ENCRYPTED_FILE_EXTENSION;
                                try
                                {
                                    using (FileStream encryptedFile = new FileStream(encryptedPath, FileMode.Create, FileAccess.Write))
                                    {
                                        // ...encrypted symmetric key length and bytes
                                        byte[] encryptedKeyBytesLength = BitConverter.GetBytes(encryptedKeyBytes.Length);
                                        encryptedFile.Write(encryptedKeyBytesLength, 0, encryptedKeyBytesLength.Length);
                                        encryptedFile.Write(encryptedKeyBytes, 0, encryptedKeyBytes.Length);

                                        // ...encrypted initialization vector length and bytes
                                        byte[] encryptedIVBytesLength = BitConverter.GetBytes(encryptedIVBytes.Length);
                                        encryptedFile.Write(encryptedIVBytesLength, 0, encryptedIVBytesLength.Length);
                                        encryptedFile.Write(encryptedIVBytes, 0, encryptedIVBytes.Length);

                                        // ...encrypted file bytes
                                        encryptedFile.Write(encryptedFileBytes, 0, encryptedFileBytes.Length);
                                    }

                                    // if everything went through okay, delete the unencrypted file and switch to encrypted file.
                                    File.Delete(_path);
                                    _path = encryptedPath;
                                }
                                catch (Exception ex)
                                {
                                    SensusException.Report("Failed to encrypt file.", ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report("Failed to close/encrypt file.", ex);
                    }
                }

                // open new file
                _path = null;
                Exception mostRecentException = null;
                for (int i = 0; _path == null && i < 5; ++i)
                {
                    try
                    {
                        _path = Path.Combine(StorageDirectory, Guid.NewGuid().ToString());
                        FileStream file = new FileStream(_path, FileMode.CreateNew, FileAccess.Write);
                        GZipStream gzipStream = new GZipStream(file, _compressionLevel);
                        _bufferedGzipStream = new BufferedStream(gzipStream, _bufferSizeBytes);
                    }
                    catch (Exception ex)
                    {
                        mostRecentException = ex;
                        _path = null;
                    }
                }

                if (_path == null)
                {
                    throw SensusException.Report("Failed to create file for local data store.", mostRecentException);
                }
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
                int fileCount = Directory.GetFiles(StorageDirectory).Length;

                string name = GetType().Name;
                misc += "Number of files (" + name + "):  " + fileCount + Environment.NewLine +
                        "Average file size (MB) (" + name + "):  " + Math.Round(SensusServiceHelper.GetDirectorySizeMB(StorageDirectory) / (float)fileCount, 2) + Environment.NewLine;
            }

            return restart;
        }
    }
}