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

using Xunit;
using Sensus.Encryption;
using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;

namespace Sensus.Tests.Encryption
{
    public class AsymmetricEncryptionTests
    {
        private string _publicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApnQa6IvmNTt2EB4MyA/R" +
                                    "JRyiJavzd7CB/oleluZVpZcEVtFGaAsa75+BRTZfrEpSg7dApMo1CsRDaOG2o8p3" +
                                    "LABb1AwtG1RSRqi8alRptjOjEOWmuhOWD0VtdLAT1bEYJ4VxMaBVmo2Yj0qKwYHD" +
                                    "H2ESMTxtW50r9K6trvbL+zxSqpnG+7Rv6IoMxTzTnG8GLXjVVzRD4Lx0SCX5QArN" +
                                    "6uZqqPJasNvxdY72m6immw3mlpKwsllVTrtmPP2ZWdSquizyybHBWk6++DFmwfc7" +
                                    "jz8AC5Va/c9gDiuZ0H+LFzl/o2JzuY3GvgmZm2N1XnDYGOU7uPjN/TvuNMruQMPf" +
                                    "VwIDAQAB";

        private string _privateKey = "MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCmdBroi+Y1O3YQ" +
                                     "HgzID9ElHKIlq/N3sIH+iV6W5lWllwRW0UZoCxrvn4FFNl+sSlKDt0CkyjUKxENo" +
                                     "4bajyncsAFvUDC0bVFJGqLxqVGm2M6MQ5aa6E5YPRW10sBPVsRgnhXExoFWajZiP" +
                                     "SorBgcMfYRIxPG1bnSv0rq2u9sv7PFKqmcb7tG/oigzFPNOcbwYteNVXNEPgvHRI" +
                                     "JflACs3q5mqo8lqw2/F1jvabqKabDeaWkrCyWVVOu2Y8/ZlZ1Kq6LPLJscFaTr74" +
                                     "MWbB9zuPPwALlVr9z2AOK5nQf4sXOX+jYnO5jca+CZmbY3VecNgY5Tu4+M39O+40" +
                                     "yu5Aw99XAgMBAAECggEAT/bfJnBu+4vBQCTvhvzyQJ3s16QvpoRJLvrXUi79Xjua" +
                                     "fgqzhDAxlIRQGevxMlfSJCzZBVGPAckfiTdGApG1hpH1i3z9/m8Xx5IwUfSThFBy" +
                                     "Oh/ZZPMb1+JGnxQnszUMoY/dvTWFamYzsELjPesUzfJYRwb8klKDV1zDW5Z8kAi4" +
                                     "PuMCwEV5Zvt61/gaxNhX5xiwWtS992ADfS5cs0raMaHvv35DHLsyGOJierfHL0Vx" +
                                     "A0FOIgBJ0bFxXeb8tvnwIZYr3XqAtKz9AulFU9EdUxlTKsO8Vuyt8rGkdTDGN3et" +
                                     "NQ4GISOqJRXZ6glUQBOroyCL/LYkW6m36VFeBq2EwQKBgQDW1gfZ3nIV9t2PeitK" +
                                     "1fGgsD3htsEZx/hYyH2T4f/X3pEEc/ifRFnhaFmYzHXPsnng1CDd9Jxn71P4Kd4W" +
                                     "wv6dtwmsXq0TF8w23dFQRc4Smafi4Fh1GfL3fCL9Ek93oPy1u93qgdi28uDGVSdj" +
                                     "bE8Vfb8m8aWl96Wj8Vq38PxjrwKBgQDGWNpgnU5fLUBdADz/SWhx0gyb2N7xqnWi" +
                                     "mHV62kHiAlj3OMwL7hlolR/O0qkM6CZ6UlBgdLNueokRYqR7OQOXoqbKy6ESs7a4" +
                                     "PXNfdga9ZjNwyd5bUHOsMaJtZZTGzLyzryi54YkP2kA38D8C4CWAuKWd5DjFMqYq" +
                                     "XWHoH1qg2QKBgDTTjjypcR4rhNGJ9elB8FdV3vGIkbT4Mf4K1q4tbU60gK46ohDv" +
                                     "qrY9hYKIDBQVS9jX8HkDdA4ukFQ+X0jzi85WOr+yzBYczO9U3epCL+js9ZZZGgc5" +
                                     "aUAnuybFgNrmsB0z496NLS/XSyQZvkS5VjzvnbhCxTupSIami2sdi8IrAoGAVc6a" +
                                     "qrFi9kndTl6MBOT9CkCUs9dem63itjS+nidN2TiqxEkN/RtEYrogyJjaCXtlKgXy" +
                                     "P8g8186q/ZpvDd/cbf0vqwvs4upcYdgz0Vh+EfHkzyaFy3tCj3vpiOopMtffytw8" +
                                     "Ai5P3UvN/GUy3Uua7dTz0RqqdKU0vZ8ofAMUcgECgYAo+T3ep4RPbwdVcwCQxCs+" +
                                     "L/B05j4fM3SyoKZuzXWPPGE69MFtfCehbGNEhci7IN6JFppEXdI3GwQvkRzlCmhP" +
                                     "H5dCpc4oV20TBgtW6u2Gyax/zzNMoT6DBzPB5TfMYTUfVdyKv26/LZdt8NDcCeqE" +
                                     "8xVwrTW6nszr32cN2fmNLg==";

        [Fact]
        public void EncryptionStringEqualsTest()
        {
            var encryption = new AsymmetricEncryption(_publicKey, _privateKey);

            Assert.Equal("aw3lrifos83fusoi3fjsofisjfo", encryption.DecryptToString(encryption.Encrypt("aw3lrifos83fusoi3fjsofisjfo")));
        }

        [Fact]
        public void EncryptionStringBadPrivateKeyTest()
        {
            Assert.Throws(typeof(FormatException), () => { new AsymmetricEncryption(_publicKey, _privateKey + "93847"); });
        }

        [Fact]
        public void EncryptWithoutPublicKeyTest()
        {
            var encryption = new AsymmetricEncryption(null, _privateKey);

            Assert.Throws(typeof(CryptographicException), () => { encryption.Encrypt("aw3lrifos83fusoi3fjsofisjfo"); });
        }

        [Fact]
        public void DecryptWithoutPrivateKeyTest()
        {
            var encryption = new AsymmetricEncryption(_publicKey, null);

            Assert.Throws(typeof(CryptographicException), () => { encryption.DecryptToString(encryption.Encrypt("aw3lrifos83fusoi3fjsofisjfo")); });
        }

        [Fact]
        public void SymmetricEncryptionEqualsTest()
        {
            string message = "aw3lrifos83fusoi3fjsofisjfo";
            byte[] messageBytes = Encoding.Unicode.GetBytes(message);

            var asymmetricEncryption = new AsymmetricEncryption(_publicKey, _privateKey);
            string path = Path.GetTempFileName();
            string outputPath = Path.GetTempFileName();
            asymmetricEncryption.EncryptSymmetrically(messageBytes, 256, 128, outputPath);

            byte[] encryptedBytes = File.ReadAllBytes(outputPath);

            int keyLen = BitConverter.ToInt32(encryptedBytes.Take(4).ToArray(), 0);
            byte[] key = encryptedBytes.Skip(4).Take(keyLen).ToArray();

            int ivLen = BitConverter.ToInt32(encryptedBytes.Skip(4 + key.Length).Take(4).ToArray(), 0);
            byte[] iv = encryptedBytes.Skip(4 + key.Length + 4).Take(ivLen).ToArray();

            key = asymmetricEncryption.DecryptToBytes(key);
            iv = asymmetricEncryption.DecryptToBytes(iv);

            byte[] encryptedMessageBytes = encryptedBytes.Skip(4 + keyLen + 4 + ivLen).ToArray();

            SymmetricEncryption symmetricEncryption = new SymmetricEncryption(key, iv);
            string decryptedMessage = symmetricEncryption.DecryptToString(encryptedMessageBytes); 
            Assert.Equal(message, decryptedMessage);
        }
    }
}
