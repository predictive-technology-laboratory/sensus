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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using NUnit.Framework;
using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes.Device;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;

namespace Sensus.Tests.DataStores.Local
{
    [TestFixture]
    public class FileLocalDataStoreTests
    {
        #region compression should not change the content of the files
        [Test]
        public void UncompressedBytesEqualUncompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes1 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();
            byte[] uncompressedBytes2 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            Assert.True(uncompressedBytes1.SequenceEqual(uncompressedBytes2));
        }

        [Test]
        public void UncompressedBytesEqualFastestDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(GetLocalDataStoreBytes(data, CompressionLevel.Fastest));

            Assert.True(uncompressedBytes.SequenceEqual(decompressedBytes));
        }

        [Test]
        public void UncompressedBytesEqualOptimalDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(GetLocalDataStoreBytes(data, CompressionLevel.Optimal));

            Assert.True(uncompressedBytes.SequenceEqual(decompressedBytes));
        }
        #endregion

        #region the file sizes should increase without closing the streams. we need this because we track the file sizes to open new files and force remote writes.
        [Test]
        public void UncompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.NoCompression, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            }, 1)[0];

            Assert.True(currentSizeMB > 0);
        }

        [Test]
        public void FastestCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.Fastest, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            }, 1)[0];

            Assert.True(currentSizeMB > 0);
        }

        [Test]
        public void OptimalCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.Optimal, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            }, 1)[0];

            Assert.True(currentSizeMB > 0);
        }
        #endregion

        #region compression should reduce file size
        [Test]
        public void UncompressedBytesGreaterThanFastestCompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();
            byte[] bytes2 = GetLocalDataStoreBytes(data, CompressionLevel.Fastest).ToArray();

            Assert.True(bytes1.Length > bytes2.Length);
        }

        [Test]
        public void FastestCompressedBytesGreaterOrEqualOptimalCompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = GetLocalDataStoreBytes(data, CompressionLevel.Fastest).ToArray();
            byte[] bytes2 = GetLocalDataStoreBytes(data, CompressionLevel.Optimal).ToArray();

            Assert.True(bytes1.Length >= bytes2.Length);
        }
        #endregion

        #region data store should create/promote files
        [Test]
        public void UncompressedRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.NoCompression, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public void FastestCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.Fastest, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public void OptimalCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }
        #endregion

        #region tar
        [Test]
        public void TarZeroFilesTest()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TarMultipleFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            // write the data store multiple times
            FileLocalDataStore localDataStore = null;
            const int NUM_FILES = 5;
            string[] paths = WriteLocalDataStore(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;

            }, NUM_FILES);

            Assert.AreEqual(NUM_FILES, paths.Length);

            // write the tar file
            string tarPath = Path.Combine(localDataStore.Protocol.StorageDirectory, Guid.NewGuid().ToString());
            localDataStore.CreateTarFromLocalData(tarPath);

            // untar
            FileStream tarFile = new FileStream(tarPath, FileMode.Open, FileAccess.Read);
            TarArchive tarArchive = TarArchive.CreateInputTarArchive(tarFile);
            string untarDirectory = Path.Combine(localDataStore.Protocol.StorageDirectory, Guid.NewGuid().ToString());
            tarArchive.ExtractContents(untarDirectory);
            tarArchive.Close();
            tarFile.Close();

            // check that the same number of files were created
            Assert.AreEqual(NUM_FILES, Directory.GetFiles(untarDirectory));

            // check that the files' contents are byte-equal
            foreach (string path in paths)
            {
                byte[] originalBytes = File.ReadAllBytes(path);
                byte[] untarredBytes = File.ReadAllBytes(Path.Combine(untarDirectory, Path.GetFileName(path)));
                Assert.IsTrue(originalBytes.SequenceEqual(untarredBytes));
            }

            Directory.Delete(untarDirectory, true);
            File.Delete(tarPath);
        }
        #endregion

        #region helper functions
        private void InitServiceHelper()
        {
            SensusServiceHelper.ClearSingleton();
            TestSensusServiceHelper serviceHelper = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => serviceHelper);
        }

        private List<Datum> GenerateData()
        {
            List<Datum> data = new List<Datum>();
            Random r = new Random();
            for (int i = 0; i < 1000; ++i)
            {
                Datum d;
                int type = r.Next(0, 3);
                if (type == 0)
                {
                    d = new AccelerometerDatum(DateTimeOffset.UtcNow, r.NextDouble(), r.NextDouble(), r.NextDouble());
                }
                else if (type == 1)
                {
                    d = new LocationDatum(DateTimeOffset.UtcNow, r.NextDouble(), r.NextDouble(), r.NextDouble());
                }
                else
                {
                    d = new BatteryDatum(DateTimeOffset.UtcNow, r.NextDouble());
                }

                data.Add(d);
            }

            return data;
        }

        private MemoryStream GetLocalDataStoreBytes(List<Datum> data, CompressionLevel compressionLevel)
        {
            byte[] bytes = File.ReadAllBytes(WriteLocalDataStore(data, compressionLevel, numTimes: 1)[0]);
            return new MemoryStream(bytes);
        }

        private string[] WriteLocalDataStore(List<Datum> data, CompressionLevel compressionLevel, Action<FileLocalDataStore> postWriteAction = null, int numTimes = 1)
        {
            Protocol protocol = CreateProtocol(compressionLevel);
            FileLocalDataStore localDataStore = protocol.LocalDataStore as FileLocalDataStore;

            List<string> paths = new List<string>();

            for (int i = 0; i < numTimes; ++i)
            {
                protocol.LocalDataStore.Start();
                WriteData(data, localDataStore, postWriteAction);
                paths.Add(localDataStore.Path);
                localDataStore.Stop();
            }

            Assert.AreEqual(localDataStore.TotalDataWritten, localDataStore.TotalDataBuffered);

            return paths.ToArray();
        }

        private Protocol CreateProtocol(CompressionLevel compressionLevel)
        {
            FileLocalDataStore localDataStore = new FileLocalDataStore()
            {
                CompressionLevel = compressionLevel
            };

            ConsoleRemoteDataStore remoteDataStore = new ConsoleRemoteDataStore()
            {
                WriteDelayMS = 1000000
            };

            Protocol protocol = new Protocol("test")
            {
                Id = Guid.NewGuid().ToString(),
                LocalDataStore = localDataStore,
                RemoteDataStore = remoteDataStore
            };

            return protocol;
        }

        private void WriteData(List<Datum> data, FileLocalDataStore localDataStore, Action<FileLocalDataStore> postWriteAction = null)
        {
            foreach (Datum datum in data)
            {
                localDataStore.WriteDatum(datum, CancellationToken.None);
                postWriteAction?.Invoke(localDataStore);
            }
        }
        #endregion
    }
}