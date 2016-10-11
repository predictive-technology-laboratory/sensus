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

using NUnit.Framework;
using Sensus.Shared.Encryption;

namespace Sensus.Shared.Tests.Encryption
{
    [TestFixture]
    public class SimpleEncryptionTests
    {
        [Test]
        public void BlankEncryptionStringTest()
        {
            var encryption = new SimpleEncryption("");

            Assert.AreEqual("A", encryption.Decrypt(encryption.Encrypt("A")));
        }

        [Test]
        public void ShortEncryptionStringTest()
        {
            var encryption = new SimpleEncryption("ab325-sdf21");

            Assert.AreEqual("A", encryption.Decrypt(encryption.Encrypt("A")));

        }

        [Test]
        public void LongEncryptionStringTest()
        {
            var encryption = new SimpleEncryption(new string('c', 200));

            Assert.AreEqual("A", encryption.Decrypt(encryption.Encrypt("A")));

        }
    }
}