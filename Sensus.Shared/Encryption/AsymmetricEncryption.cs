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

using System;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Sensus.Encryption
{
    public class AsymmetricEncryption : IEncryption
    {
        private RSAParameters _rsaPublicParameters;
        private RSAParameters _rsaPrivateParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Encryption.AsymmetricEncryption"/> class.
        /// </summary>
        /// <param name="publicKeyString">Public key string. Can be generated following the generation of the private key (see below)
        /// using the following command:
        /// 
        ///   openssl rsa -in private.pem -outform PEM -pubout -out public.pem
        /// 
        /// </param>
        public AsymmetricEncryption(string publicKeyString)
            : this(publicKeyString, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Encryption.AsymmetricEncryption"/> class.
        /// </summary>
        /// <param name="publicKeyString">Public key string. Can be generated following the generation of the private key (see below)
        /// using the following command:
        /// 
        ///   openssl rsa -in private.pem -outform PEM -pubout -out public.pem
        /// 
        /// </param>
        /// <param name="privateKeyString">Private key string. Only necessary if you want to be able to decrypt using this object. Can 
        /// be generated with the following commands:
        /// 
        ///   openssl genrsa -des3 -out private.pem 2048
        ///   openssl pkcs8 -topk8 -nocrypt -in private.pem
        /// 
        /// </param>
        public AsymmetricEncryption(string publicKeyString, string privateKeyString)
        {
            // the following is adapted from http://stackoverflow.com/questions/9283716/c-sharp-net-crypto-using-base64-encoded-public-key-to-verify-rsa-signature/9290086#9290086

            if (publicKeyString != null)
            {
                byte[] publicKeyBytes = Convert.FromBase64String(publicKeyString);
                RsaKeyParameters rsaKeyParameters = PublicKeyFactory.CreateKey(publicKeyBytes) as RsaKeyParameters;

                _rsaPublicParameters = new RSAParameters();
                _rsaPublicParameters.Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned();
                _rsaPublicParameters.Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned();
            }

            if (privateKeyString != null)
            {
                byte[] privateKeyBytes = Convert.FromBase64String(privateKeyString);
                RsaPrivateCrtKeyParameters privateKeyParameters = PrivateKeyFactory.CreateKey(privateKeyBytes) as RsaPrivateCrtKeyParameters;

                _rsaPrivateParameters = new RSAParameters();
                _rsaPrivateParameters.DP = privateKeyParameters.DP.ToByteArrayUnsigned();
                _rsaPrivateParameters.DQ = privateKeyParameters.DQ.ToByteArrayUnsigned();
                _rsaPrivateParameters.P = privateKeyParameters.P.ToByteArrayUnsigned();
                _rsaPrivateParameters.Q = privateKeyParameters.Q.ToByteArrayUnsigned();
                _rsaPrivateParameters.InverseQ = privateKeyParameters.QInv.ToByteArrayUnsigned();
                _rsaPrivateParameters.Modulus = privateKeyParameters.Modulus.ToByteArrayUnsigned();
                _rsaPrivateParameters.Exponent = privateKeyParameters.PublicExponent.ToByteArrayUnsigned();
            }
        }

        public byte[] Encrypt(string unencryptedValue)
        {
            return Encrypt(Encoding.Unicode.GetBytes(unencryptedValue));
        }

        public byte[] Encrypt(byte[] unencryptedBytes)
        {
            using (RSACryptoServiceProvider encrypter = new RSACryptoServiceProvider())
            {
                encrypter.ImportParameters(_rsaPublicParameters);
                return encrypter.Encrypt(unencryptedBytes, false);
            }
        }

        public byte[] DecryptToBytes(byte[] encryptedBytes)
        {
            using (RSACryptoServiceProvider decrypter = new RSACryptoServiceProvider())
            {
                decrypter.ImportParameters(_rsaPrivateParameters);
                return decrypter.Decrypt(encryptedBytes, false);
            }
        }

        public string DecryptToString(byte[] encryptedBytes)
        {
            return Encoding.Unicode.GetString(DecryptToBytes(encryptedBytes));
        }

        /// <summary>
        /// Encrypts bytes asymmetrically via symmetric encryption. Since asymmetric encryption does not support large data sizes, the approach
        /// is to generate a symmetric encryption key that is designed for large data sizes, encrypt the data with the symmetric key, encrypt
        /// the symmetric key with the asymmetric key, and send the encrypted symmetric key and encrypted data to the same file.
        /// </summary>
        /// <param name="unencryptedBytes">Unencrypted bytes.</param>
        /// <param name="symmetricKeySizeBits">Symmetric key size in bits.</param>
        /// <param name="symmetricInitializationVectorSizeBits">Symmetric initialization vector size in bits.</param>
        /// <param name="encryptedOutputPath">Encrypted output path.</param>
        public void EncryptSymmetrically(byte[] unencryptedBytes, int symmetricKeySizeBits, int symmetricInitializationVectorSizeBits, string encryptedOutputPath)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                // generate new symmetric key and initialization vector
                aes.KeySize = symmetricKeySizeBits;
                aes.BlockSize = symmetricInitializationVectorSizeBits;
                aes.GenerateKey();
                aes.GenerateIV();

                // encrypt the data symmetrically
                SymmetricEncryption symmetricEncryption = new SymmetricEncryption(aes.Key, aes.IV);
                byte[] encryptedBytes = symmetricEncryption.Encrypt(unencryptedBytes);

                // encrypt the symmetric key and initialization vector asymmetrically
                byte[] encryptedKeyBytes = Encrypt(aes.Key);
                byte[] encryptedIVBytes = Encrypt(aes.IV);

                // write the encrypted output file
                using (FileStream encryptedOutputFile = new FileStream(encryptedOutputPath, FileMode.Create, FileAccess.Write))
                {
                    // ...encrypted symmetric key length and bytes
                    byte[] encryptedKeyBytesLength = BitConverter.GetBytes(encryptedKeyBytes.Length);
                    encryptedOutputFile.Write(encryptedKeyBytesLength, 0, encryptedKeyBytesLength.Length);
                    encryptedOutputFile.Write(encryptedKeyBytes, 0, encryptedKeyBytes.Length);

                    // ...encrypted initialization vector length and bytes
                    byte[] encryptedIVBytesLength = BitConverter.GetBytes(encryptedIVBytes.Length);
                    encryptedOutputFile.Write(encryptedIVBytesLength, 0, encryptedIVBytesLength.Length);
                    encryptedOutputFile.Write(encryptedIVBytes, 0, encryptedIVBytes.Length);

                    // ...encrypted bytes
                    encryptedOutputFile.Write(encryptedBytes, 0, encryptedBytes.Length);
                }
            }
        }
    }
}
