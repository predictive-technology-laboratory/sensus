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
using System.Security.Cryptography;
using System;

namespace Sensus.Tests.Encryption
{
    
    public class SymmetricEncryptionTests
    {
        [Fact]
        public void ShortEncryptionKeyTest()
        {
            var encryption = new SymmetricEncryption("123123");

            Assert.Throws(typeof(CryptographicException), () => { encryption.Encrypt("A"); });
        }

        [Fact]
        public void OddLengthEncryptionKeyTest()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                var encryption = new SymmetricEncryption("12312");
                encryption.Encrypt("A");
            });
        }

        [Fact]
        public void GoodEncryptionKeyTest()
        {
            var encryption = new SymmetricEncryption("21759BBC6FD5F9AB7012F8BF6C998080F3C5A5A168C3ADCE13CB872F28598A44");

            Assert.Equal("asl3j3lkfjwlkj3lwk3jflwk3j", encryption.DecryptToString(encryption.Encrypt("asl3j3lkfjwlkj3lwk3jflwk3j")));
        }
    }
}
