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
