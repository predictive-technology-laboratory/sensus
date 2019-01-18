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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sensus.Context;
using Sensus.Encryption;
using Sensus.Exceptions;
using Sensus.Extensions;

namespace Sensus.Authentication
{
    /// <summary>
    /// Handles all interactions with an [authentication server](xref:authentication_servers).
    /// </summary>
    public class AuthenticationService : IEnvelopeEncryptor
    {
        private const string CREATE_ACCOUNT_PATH = "/createaccount?deviceId={0}&participantId={1}&deviceType={2}";
        private const string GET_CREDENTIALS_PATH = "/getcredentials?participantId={0}&password={1}&deviceId={2}";

        private readonly string _createAccountURL;
        private readonly string _getCredentialsURL;
        private Task<AmazonS3Credentials> _getCredentialsTask;
        private readonly object _getCredentialsTaskLocker = new object();

        /// <summary>
        /// Gets or sets the base service URL.
        /// </summary>
        /// <value>The base service URL.</value>
        public string BaseServiceURL { get; set; }

        /// <summary>
        /// Gets or sets the account.
        /// </summary>
        /// <value>The account.</value>
        public Account Account { get; set; }

        /// <summary>
        /// Gets or sets the AWS S3 credentials.
        /// </summary>
        /// <value>The credentials.</value>
        [JsonProperty]
        private AmazonS3Credentials AmazonS3Credentials { get; set; }

        public Protocol Protocol { get; set; }

        public AuthenticationService(string baseServiceURL)
        {
            BaseServiceURL = baseServiceURL.Trim('/');

            _createAccountURL = BaseServiceURL + CREATE_ACCOUNT_PATH;
            _getCredentialsURL = BaseServiceURL + GET_CREDENTIALS_PATH;
        }

        public async Task<Account> CreateAccountAsync(string participantId)
        {
            string deviceType = "";
            if (SensusContext.Current.Platform == Platform.Android)
            {
                deviceType = "android";
            }
            else if (SensusContext.Current.Platform == Platform.iOS)
            {
                deviceType = "ios";
            }
            else
            {
                SensusException.Report("Unrecognized platform:  " + SensusContext.Current.Platform);
            }

            string accountJSON = await new Uri(string.Format(_createAccountURL, SensusServiceHelper.Get().DeviceId, participantId, deviceType)).DownloadString();

            try
            {
                Account = accountJSON.DeserializeJson<Account>();
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while deserializing account:  " + ex.Message, ex);
            }

            // check properties

            if (string.IsNullOrWhiteSpace(Account.ParticipantId))
            {
                SensusException.Report("Empty " + nameof(Account.ParticipantId) + " returned by authentication service for device " + SensusServiceHelper.Get().DeviceId + " and participant " + (participantId ?? "[null]."));
            }

            if (string.IsNullOrWhiteSpace(Account.Password))
            {
                SensusException.Report("Empty " + nameof(Account.Password) + " returned by authentication service for device " + SensusServiceHelper.Get().DeviceId + " and participant " + (participantId ?? "[null]."));
            }

            // save the app state to hang on to the account
            await SensusServiceHelper.Get().SaveAsync();

            return Account;
        }

        public Task<AmazonS3Credentials> GetCredentialsAsync()
        {
            lock (_getCredentialsTaskLocker)
            {
                if (_getCredentialsTask == null ||
                    _getCredentialsTask.Status == TaskStatus.Canceled ||
                    _getCredentialsTask.Status == TaskStatus.Faulted ||
                    _getCredentialsTask.Status == TaskStatus.RanToCompletion)
                {
                    _getCredentialsTask = Task.Run(async () =>
                    {
                        if (AmazonS3Credentials?.WillBeValidFor(TimeSpan.FromHours(1)) ?? false)
                        {
                            return AmazonS3Credentials;
                        }
                        else
                        {
                            AmazonS3Credentials = null;
                        }

                        if (Account == null)
                        {
                            throw new Exception("Tried to get credentials without an account.");
                        }

                        string credentialsJSON = await new Uri(string.Format(_getCredentialsURL, Account.ParticipantId, Account.Password, SensusServiceHelper.Get().DeviceId)).DownloadString();

                        // deserialize credentials
                        try
                        {
                            AmazonS3Credentials = credentialsJSON.DeserializeJson<AmazonS3Credentials>();
                        }
                        catch (Exception ex)
                        {
                            SensusException.Report("Exception while deserializing AWS S3 credentials.", ex);
                            throw ex;
                        }

                        // check properties

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.AccessKeyId))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.AccessKeyId) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.CustomerMasterKey))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.CustomerMasterKey) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ExpirationUnixTimeMilliseconds))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ExpirationUnixTimeMilliseconds) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ProtocolId))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ProtocolId) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ProtocolURL))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ProtocolURL) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.Region))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.Region) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.SecretAccessKey))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.SecretAccessKey) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.SessionToken))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.SessionToken) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        // save the app state to hang on to the credentials
                        await SensusServiceHelper.Get().SaveAsync();

                        return AmazonS3Credentials;
                    });
                }

                return _getCredentialsTask;
            }
        }

        public async Task EnvelopeAsync(byte[] unencryptedBytes, int symmetricKeySizeBits, int symmetricInitializationVectorSizeBits, Stream encryptedOutputStream, CancellationToken cancellationToken)
        {
            try
            {
                AmazonS3Credentials kmsCredentials = await GetCredentialsAsync();

                if (symmetricKeySizeBits != 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(symmetricKeySizeBits), "Invalid value " + symmetricKeySizeBits + ". Only 256-bit keys are supported.");
                }

                if (symmetricInitializationVectorSizeBits != 128)
                {
                    throw new ArgumentOutOfRangeException(nameof(symmetricInitializationVectorSizeBits), "Invalid value " + symmetricInitializationVectorSizeBits + ". Only 128-bit initialization vectors are supported.");
                }

                AmazonKeyManagementServiceClient kmsClient = new AmazonKeyManagementServiceClient(kmsCredentials.AccessKeyId, kmsCredentials.SecretAccessKey, kmsCredentials.SessionToken, kmsCredentials.RegionEndpoint);

                // generate a symmetric data key
                GenerateDataKeyResponse dataKeyResponse = await kmsClient.GenerateDataKeyAsync(new GenerateDataKeyRequest
                {
                    KeyId = kmsCredentials.CustomerMasterKey,
                    KeySpec = DataKeySpec.AES_256

                }, cancellationToken);

                // write encrypted payload

                // write encrypted data key length and bytes
                byte[] encryptedDataKeyBytes = dataKeyResponse.CiphertextBlob.ToArray();
                byte[] encryptedDataKeyBytesLength = BitConverter.GetBytes(encryptedDataKeyBytes.Length);
                encryptedOutputStream.Write(encryptedDataKeyBytesLength, 0, encryptedDataKeyBytesLength.Length);
                encryptedOutputStream.Write(encryptedDataKeyBytes, 0, encryptedDataKeyBytes.Length);

                // write encrypted random initialization vector length and bytes
                Random random = new Random();
                byte[] initializationVectorBytes = new byte[16];
                random.NextBytes(initializationVectorBytes);

                byte[] encryptedInitializationVectorBytes = (await kmsClient.EncryptAsync(new EncryptRequest
                {
                    KeyId = kmsCredentials.CustomerMasterKey,
                    Plaintext = new MemoryStream(initializationVectorBytes)

                }, cancellationToken)).CiphertextBlob.ToArray();

                byte[] encryptedInitializationVectorBytesLength = BitConverter.GetBytes(encryptedInitializationVectorBytes.Length);
                encryptedOutputStream.Write(encryptedInitializationVectorBytesLength, 0, encryptedInitializationVectorBytesLength.Length);
                encryptedOutputStream.Write(encryptedInitializationVectorBytes, 0, encryptedInitializationVectorBytes.Length);

                // write symmetrically encrypted bytes
                byte[] dataKeyBytes = dataKeyResponse.Plaintext.ToArray();
                SymmetricEncryption symmetricEncryption = new SymmetricEncryption(dataKeyBytes, initializationVectorBytes);
                byte[] encryptedBytes = symmetricEncryption.Encrypt(unencryptedBytes);
                encryptedOutputStream.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
            catch (WebException ex)
            {
                // don't report the exception if it was caused by a connection failure, as we'll get this under expected conditions.
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to connect when running KMS-based envelope encryption:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                // report non-connect based exceptions
                else
                {
                    SensusException.Report("Non-connection web exception when running KMS-based envelope encryption:  " + ex.Message, ex);
                }

                // always throw the exception though, as we did not succeed.
                throw ex;
            }
            // any other exceptions may be problematic, so report and throw them.
            catch (Exception ex)
            {
                SensusException.Report("Exception when running KMS-based envelope encryption:  " + ex.Message, ex);
                throw ex;
            }
        }
    }
}