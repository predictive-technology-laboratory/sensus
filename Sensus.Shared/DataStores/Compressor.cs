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
using System.IO.Compression;

namespace Sensus.DataStores
{
    public class Compressor
    {
        public enum CompressionMethod
        {
            GZip
        }

        private CompressionMethod _method;

        public Compressor(CompressionMethod method)
        {
            _method = method;
        }

        public void Compress(byte[] bytesToCompress, Stream destinationStream)
        {
            if (_method == CompressionMethod.GZip)
            {
                using (GZipStream zip = new GZipStream(destinationStream, CompressionMode.Compress, true))
                {
                    zip.Write(bytesToCompress, 0, bytesToCompress.Length);
                    zip.Flush();
                }
            }
            else
            {
                throw new NotImplementedException("Unrecognized compression method:  " + _method);
            }
        }

        public byte[] Decompress(Stream compressedStream)
        {
            if (_method == CompressionMethod.GZip)
            {
                MemoryStream decompressedStream = new MemoryStream();

                using (GZipStream zip = new GZipStream(compressedStream, CompressionMode.Decompress, true))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = zip.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        decompressedStream.Write(buffer, 0, bytesRead);
                    }
                }

                return decompressedStream.ToArray();
            }
            else
            {
                throw new NotImplementedException("Unrecognized compression method:  " + _method);
            }
        }
    }
}