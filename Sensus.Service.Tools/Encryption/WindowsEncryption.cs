using System;
using System.Text;

namespace Sensus.Service.Tools
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
