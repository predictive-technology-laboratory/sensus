//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Xunit;
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
    
    public class FileLocalDataStoreTests
    {
        #region compression should not change the content of the files
        [Fact]
        public async Task UncompressedBytesEqualUncompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes1 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();
            byte[] uncompressedBytes2 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();

            Assert.True(uncompressedBytes1.SequenceEqual(uncompressedBytes2));
        }

        [Fact]
        public async Task UncompressedBytesEqualFastestDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(await GetLocalDataStoreBytesAsync(data, CompressionLevel.Fastest));

            Assert.True(uncompressedBytes.SequenceEqual(decompressedBytes));
        }

        [Fact]
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
        [Fact]
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

        [Fact]
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

        [Fact]
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
        [Fact]
        public async Task UncompressedBytesGreaterThanFastestCompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.NoCompression)).ToArray();
            byte[] bytes2 = (await GetLocalDataStoreBytesAsync(data, CompressionLevel.Fastest)).ToArray();

            Assert.True(bytes1.Length > bytes2.Length);
        }

        [Fact]
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
        [Fact]
        public async Task UncompressedRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.NoCompression, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Fact]
        public async Task FastestCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.Fastest, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Fact]
        public async Task OptimalCompressionRemoteWriteClearsFilesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            await WriteLocalDataStoreAsync(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            await localDataStore.WriteToRemoteAsync(CancellationToken.None);

            // there should still be 1 file (the new open file)
            Assert.Equal(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }
        #endregion

        #region tar
        [Fact]
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

            Assert.Equal(numFiles, paths.Length);

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
            Assert.Equal(numFiles, Directory.GetFiles(untarDirectory, "*.json.gz", SearchOption.AllDirectories).Length);

            // check that the files' contents are byte-equal
            foreach (string path in paths)
            {
                byte[] originalBytes = File.ReadAllBytes(path);
                string untarredPath = Directory.GetFiles(untarDirectory, Path.GetFileName(path), SearchOption.AllDirectories).Single();
                byte[] untarredBytes = File.ReadAllBytes(untarredPath);
                Assert.True(originalBytes.SequenceEqual(untarredBytes));
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
                Assert.Equal(localDataStore.TotalDataWritten, localDataStore.TotalDataBuffered);
            }

            Assert.Equal(numTimes, paths.Count);

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
