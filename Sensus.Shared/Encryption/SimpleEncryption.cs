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
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Sensus.Service.Tools
{
    class SimpleEncryption: IEncryption
    {
        #region Fields
        private readonly byte[] EncryptionKeyBytes;
        #endregion

        #region Constructor
        public SimpleEncryption(string encryptionKey)
        {
            EncryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey).Concat(new byte [32]).Take(32).ToArray();
        }
        #endregion

        #region Public Methods
        public byte[] Encrypt(string unencryptedValue)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateEncryptor(encryptionKeyBytes, initialization))
                {
                    byte[] unencryptedBytes = Encoding.Unicode.GetBytes(unencryptedValue);
                    return transform.TransformFinalBlock(unencryptedBytes, 0, unencryptedBytes.Length);
                }
            }
        }

        public string Decrypt(byte[] encryptedBytes)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] encryptionKeyBytes = EncryptionKeyBytes;
                aes.KeySize = encryptionKeyBytes.Length * 8;

                byte[] initialization = new byte[16];
                aes.BlockSize = initialization.Length * 8;

                using (ICryptoTransform transform = aes.CreateDecryptor(encryptionKeyBytes, initialization))
                {
                    return Encoding.Unicode.GetString(transform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
                }
            }
        }
        #endregion
    }
}
