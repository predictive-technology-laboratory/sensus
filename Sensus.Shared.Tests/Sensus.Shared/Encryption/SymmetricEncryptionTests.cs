﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

using Xunit;
using Sensus.Encryption;
using System.Security.Cryptography;
using System;
using System.Text;

namespace Sensus.Tests.Encryption
{
    
    public class SymmetricEncryptionTests
    {
        [Fact]
        public void ShortEncryptionKeyTest()
        {
            var encryption = new SymmetricEncryption("123123");

            Assert.Throws(typeof(CryptographicException), () => { encryption.Encrypt("A", Encoding.UTF8); });
        }

        [Fact]
        public void OddLengthEncryptionKeyTest()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                var encryption = new SymmetricEncryption("12312");
                encryption.Encrypt("A", Encoding.UTF8);
            });
        }

        [Fact]
        public void GoodEncryptionKeyTest()
        {
            var encryption = new SymmetricEncryption("21759BBC6FD5F9AB7012F8BF6C998080F3C5A5A168C3ADCE13CB872F28598A44");

            Assert.Equal("asl3j3lkfjwlkj3lwk3jflwk3j", encryption.DecryptToString(encryption.Encrypt("asl3j3lkfjwlkj3lwk3jflwk3j", Encoding.UTF8), Encoding.UTF8));
        }
    }
}