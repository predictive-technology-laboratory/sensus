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

using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System;

namespace Sensus.Encryption
{
    public class SymmetricEncryption : IEncryption
    {
        private readonly byte[] _encryptionKeyBytes;
        private readonly byte[] _initializationVectorBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Encryption.SymmetricEncryption"/> class. Uses zero-valued initialization vector.
        /// 
        /// </summary>
        /// <param name="encryptionKeyHexString">A 64-character hex-encoded string for a 256-bit symmetric AES encryption key. Can be generated with the following:
        /// 
        ///     openssl enc -aes-256-cbc -k secret -P -md sha1
        /// 
        /// </param>
        public SymmetricEncryption(string encryptionKeyHexString)
        {
            if (string.IsNullOrWhiteSpace(encryptionKeyHexString))
            {
                _encryptionKeyBytes = new byte[32];
            }
            else
            {
                _encryptionKeyBytes = ConvertHexStringToByteArray(encryptionKeyHexString);
            }

            _initializationVectorBytes = new byte[16];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Encryption.SymmetricEncryption"/> class.
        /// </summary>
        /// <param name="encryptionKeyBytes">A 256-bit (32-byte) symmetric AES encryption key.</param>
        /// <param name="initializationVectorBytes">A 128-bit (16-byte) initialization vector.</param>
        public SymmetricEncryption(byte[] encryptionKeyBytes, byte[] initializationVectorBytes)
        {
            _encryptionKeyBytes = encryptionKeyBytes;
            _initializationVectorBytes = initializationVectorBytes;
        }

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        public byte[] Encrypt(string unencryptedValue)
        {
            return Encrypt(Encoding.Unicode.GetBytes(unencryptedValue));
        }

        public byte[] Encrypt(byte[] unencryptedBytes)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = _encryptionKeyBytes.Length * 8;
                aes.BlockSize = _initializationVectorBytes.Length * 8;  // IV length must be block size divided by 8:  https://msdn.microsoft.com/en-us/library/system.security.cryptography.symmetricalgorithm.iv(v=vs.110).aspx

                using (ICryptoTransform transform = aes.CreateEncryptor(_encryptionKeyBytes, _initializationVectorBytes))
                {
                    return transform.TransformFinalBlock(unencryptedBytes, 0, unencryptedBytes.Length);
                }
            }
        }

        public string DecryptToString(byte[] encryptedBytes)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = _encryptionKeyBytes.Length * 8;
                aes.BlockSize = _initializationVectorBytes.Length * 8;

                using (ICryptoTransform transform = aes.CreateDecryptor(_encryptionKeyBytes, _initializationVectorBytes))
                {
                    return Encoding.Unicode.GetString(transform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
                }
            }
        }
    }
}
