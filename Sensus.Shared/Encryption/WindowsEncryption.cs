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
using System.Text;

namespace Sensus.Shared.Encryption
{
    class WindowsEncryption: IEncryption
    {
        private readonly byte[] EncryptionKeyBytes;

        public WindowsEncryption(string encryptionKey)
        {
            EncryptionKeyBytes = Encoding.Unicode.GetBytes(encryptionKey);
        }

        public byte[] Encrypt(string unencryptedValue)
        {
            throw new NotImplementedException("This needs a windows phone endpoint to build against");
            //return ProtectedData.Protect(Encoding.Unicode.GetBytes(unencryptedValue), EncryptionKeyBytes);
        }

        public string Decrypt(byte[] encryptedBytes)
        {
            throw new NotImplementedException("This needs a windows phone endpoint to build against");
            //byte[] unencryptedBytes = ProtectedData.Unprotect(encryptedBytes, EncryptionKeyBytes);
            //return Encoding.Unicode.GetString(unencryptedBytes, 0, unencryptedBytes.Length);
        }
    }
}
