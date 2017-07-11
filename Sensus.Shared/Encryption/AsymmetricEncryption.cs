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
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;

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

        public string Decrypt(byte[] encryptedBytes)
        {
            using (RSACryptoServiceProvider decrypter = new RSACryptoServiceProvider())
            {
                decrypter.ImportParameters(_rsaPrivateParameters);
                return Encoding.Unicode.GetString(decrypter.Decrypt(encryptedBytes, false));
            }
        }
    }
}