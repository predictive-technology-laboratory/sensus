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

using System.Text;
using System.Security.Cryptography;
using System;

namespace Sensus.Encryption
{
    public class SymmetricEncryption : IEncryptor
    {
        private readonly byte[] _encryptionKeyBytes;
        private readonly byte[] _initializationVectorBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Encryption.SymmetricEncryption"/> class. Uses a zero-valued initialization vector.
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

        public byte[] Encrypt(string unencryptedValue, Encoding encoding)
        {
            return Encrypt(encoding.GetBytes(unencryptedValue));
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

        public string DecryptToString(byte[] encryptedBytes, Encoding encoding)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = _encryptionKeyBytes.Length * 8;
                aes.BlockSize = _initializationVectorBytes.Length * 8;

                using (ICryptoTransform transform = aes.CreateDecryptor(_encryptionKeyBytes, _initializationVectorBytes))
                {
                    return encoding.GetString(transform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
                }
            }
        }
    }
}