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
using System.Threading.Tasks;

namespace Sensus.Tests.DataStores.Local
{
    [TestFixture]
    public class FileLocalDataStoreTests
    {
        #region compression should not change the content of the files
        [Test]
        public async Task UncompressedBytesEqualUncompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes1 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();
            byte[] uncompressedBytes2 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();

            Assert.True(uncompressedBytes1.SequenceEqual(uncompressedBytes2));
        }

        [Test]
        public async Task UncompressedBytesEqualFastestDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(await GetLocalDataStoreBytesAsync(data, CompressionLevel.Fastest));

            Assert.True(uncompressedBytes.SequenceEqual(decompressedBytes));
        }

        [Test]
        public async Task UncompressedBytesEqualOptimalDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(await GetLocalDataStoreBytesAsync(data, CompressionLevel.Optimal));

            Assert.True(uncompressedBytes.SequenceEqual(decompressedBytes));
        }
        #endregion

        #region the file sizes should increase without closing the streams. we need this because we track the file sizes to open new files and force remote writes.
        [Test]
        public async Task UncompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            await WriteLocalDataStoreAsync(data, CompressionLevel.NoCompression, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.True(currentSizeMB > 0);
        }

        [Test]
        public async Task FastestCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            await WriteLocalDataStoreAsync(data, CompressionLevel.Fastest, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.True(currentSizeMB > 0);
        }

        [Test]
        public async Task OptimalCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            await WriteLocalDataStoreAsync(data, CompressionLevel.Optimal, fileLocalDataStore =>
            {
                fileLocalDataStore.Flush();
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.True(newSizeMB >= currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.True(currentSizeMB > 0);
        }
        #endregion

        #region compression should reduce file size
        [Test]
        public async Task UncompressedBytesGreaterThanFastestCompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();
            byte[] bytes2 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.Fastest)).ToArray();

            Assert.True(bytes1.Length > bytes2.Length);
        }

        [Test]
        public async Task FastestCompressedBytesGreaterOrEqualOptimalCompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.Fastest)).ToArray();
            byte[] bytes2 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.Optimal)).ToArray();

            Assert.True(bytes1.Length >= bytes2.Length);
        }
        #endregion

        #region data store should create/promote files
        [Test]
        public async Task UncompressedRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.NoCompression, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public async Task FastestCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.Fastest, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public async Task OptimalCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }
        #endregion

        #region tar
        [Test]
        public async Task TarFilesTest()
        {
            await TarTestAsync(1);
            await TarTestAsync(2);
            await TarTestAsync(5);
            await TarTestAsync(10);
        }

        private async Task TarTestAsync(int numFiles)
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            // write the data store multiple times
            FileLocalDataStore localDataStore = null;
            string[] paths = await WriteLocalDataStoreAsync(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;

            }, numFiles);

            Assert.AreEqual(numFiles, paths.Length);

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
            Assert.AreEqual(numFiles, Directory.GetFiles(untarDirectory, "*.json.gz", SearchOption.AllDirectories).Length);

            // check that the files' contents are byte-equal
            foreach (string path in paths)
            {
                byte[] originalBytes = File.ReadAllBytes(path);
                string untarredPath = Directory.GetFiles(untarDirectory, Path.GetFileName(path), SearchOption.AllDirectories).Single();
                byte[] untarredBytes = File.ReadAllBytes(untarredPath);
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

        private async Task<MemoryStream> GetLocalDataStoreBytesAsync(List<Datum> data, CompressionLevel compressionLevel)
        {
            byte[] bytes = File.ReadAllBytes((await WriteLocalDataStoreAsync(data, compressionLevel, numTimes: 1)).Single());
            return new MemoryStream(bytes);
        }

        private async Task<string[]> WriteLocalDataStoreAsync(List<Datum> data, CompressionLevel compressionLevel, Action<FileLocalDataStore> postWriteAction = null, int numTimes = 1)
        {
            Protocol protocol = await CreateProtocolAsync(compressionLevel);
            FileLocalDataStore localDataStore = protocol.LocalDataStore as FileLocalDataStore;

            List<string> paths = new List<string>();

            for (int i = 0; i < numTimes; ++i)
            {
                await localDataStore.StartAsync();
                WriteData(data, localDataStore, postWriteAction);
                string path = localDataStore.Path + ".json" + (compressionLevel == CompressionLevel.NoCompression ? "" : ".gz"); // file is about to be promoted on Stop.
                paths.Add(path);
                await localDataStore.StopAsync();
                Assert.True(File.Exists(path));
                Assert.AreEqual(localDataStore.TotalDataWritten, localDataStore.TotalDataBuffered);
            }

            Assert.AreEqual(numTimes, paths.Count);

            return paths.ToArray();
        }

        private async Task<Protocol> CreateProtocolAsync(CompressionLevel compressionLevel)
        {
            FileLocalDataStore localDataStore = new FileLocalDataStore()
            {
                CompressionLevel = compressionLevel
            };

            ConsoleRemoteDataStore remoteDataStore = new ConsoleRemoteDataStore()
            {
                WriteDelayMS = 1000000
            };

            Protocol protocol = await Protocol.CreateAsync("test");
            protocol.Id = Guid.NewGuid().ToString();
            protocol.LocalDataStore = localDataStore;
            protocol.RemoteDataStore = remoteDataStore;

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