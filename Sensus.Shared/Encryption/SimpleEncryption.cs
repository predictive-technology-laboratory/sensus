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
