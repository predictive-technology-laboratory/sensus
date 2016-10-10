using NUnit.Framework;
using Sensus.Service.Tools;

namespace Sensus.ToolsTests1.Encryption
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