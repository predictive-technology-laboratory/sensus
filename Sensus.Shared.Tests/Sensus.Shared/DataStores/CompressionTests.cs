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
using System.IO;
using NUnit.Framework;
using Sensus.DataStores;
using System.Linq;

namespace Sensus.Tests.DataStores
{
    [TestFixture]
    public class CompressionTests
    {
        [Test]
        public void GZipCompressDecompressEqualityTest()
        {
            for (int i = 0; i < 100; ++i)
            {
                Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);

                byte[] bytes = new byte[2056];
                new Random().NextBytes(bytes);

                MemoryStream compressedStream = new MemoryStream();
                compressor.Compress(bytes, compressedStream);
                compressedStream.Position = 0;

                byte[] decompressedBytes = compressor.Decompress(compressedStream);

                Assert.True(bytes.SequenceEqual(decompressedBytes));
            }
        }
    }
}
