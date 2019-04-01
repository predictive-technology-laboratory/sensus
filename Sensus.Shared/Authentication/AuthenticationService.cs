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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Newtonsoft.Json;
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

        /// <summary>
        /// The ignored web exception statuses. These are statuses that we expect to encounter under normal operating 
        /// conditions (e.g., due to lack of internet connection, server-side errors, etc.).
        /// </summary>
        private readonly List<WebExceptionStatus> IGNORED_WEB_EXCEPTION_STATUSES = new[] { WebExceptionStatus.ConnectFailure,
                                                                                           WebExceptionStatus.NameResolutionFailure,
                                                                                           WebExceptionStatus.SecureChannelFailure,
                                                                                           WebExceptionStatus.Timeout,
                                                                                           WebExceptionStatus.TrustFailure,
                                                                                           WebExceptionStatus.ReceiveFailure }.ToList();
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

            string accountJSON = await new Uri(string.Format(_createAccountURL, SensusServiceHelper.Get().DeviceId, participantId, deviceType)).DownloadStringAsync();

            try
            {
                Account = accountJSON.DeserializeJson<Account>();
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while deserializing account:  " + ex.Message, ex);
            }

            // check properties. trim while we're at it.

            if (string.IsNullOrWhiteSpace(Account.ParticipantId = Account.ParticipantId?.Trim()))
            {
                SensusException.Report("Empty " + nameof(Account.ParticipantId) + " returned by authentication service for device " + SensusServiceHelper.Get().DeviceId + " and participant " + (participantId ?? "[null]."));
            }

            if (string.IsNullOrWhiteSpace(Account.Password = Account.Password?.Trim()))
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
                // if the get credentials task is in a state from which we wouldn't expect to return a presently
                // valid set of credentials, then start a new task to check/refresh the credentials.
                if (_getCredentialsTask == null ||
                    _getCredentialsTask.Status == TaskStatus.Canceled ||
                    _getCredentialsTask.Status == TaskStatus.Faulted ||
                    _getCredentialsTask.Status == TaskStatus.RanToCompletion)
                {
                    _getCredentialsTask = Task.Run(async () =>
                    {
                        // if the credentials we currently hold will be valid for a while, then simply return them.
                        if (AmazonS3Credentials?.WillBeValidFor(TimeSpan.FromHours(1)) ?? false)
                        {
                            return AmazonS3Credentials;
                        }
                        else
                        {
                            AmazonS3Credentials = null;
                        }

                        // we should always have an account
                        if (Account == null)
                        {
                            Exception noAccountException = new Exception("Tried to get credentials without an account.");
                            SensusException.Report(noAccountException);
                            throw noAccountException;
                        }

                        string credentialsJSON = await new Uri(string.Format(_getCredentialsURL, Account.ParticipantId, Account.Password, SensusServiceHelper.Get().DeviceId)).DownloadStringAsync();

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

                        // check properties. trim while we're at it.

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.AccessKeyId = AmazonS3Credentials.AccessKeyId?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.AccessKeyId) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.CustomerMasterKey = AmazonS3Credentials.CustomerMasterKey?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.CustomerMasterKey) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ExpirationUnixTimeMilliseconds = AmazonS3Credentials.ExpirationUnixTimeMilliseconds?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ExpirationUnixTimeMilliseconds) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ProtocolId = AmazonS3Credentials.ProtocolId?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ProtocolId) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.ProtocolURL = AmazonS3Credentials.ProtocolURL?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.ProtocolURL) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.Region = AmazonS3Credentials.Region?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.Region) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.SecretAccessKey = AmazonS3Credentials.SecretAccessKey?.Trim()))
                        {
                            SensusException.Report("Empty " + nameof(AmazonS3Credentials.SecretAccessKey) + " returned by authentication service for participant " + (Account.ParticipantId ?? "[null]."));
                        }

                        if (string.IsNullOrWhiteSpace(AmazonS3Credentials.SessionToken = AmazonS3Credentials.SessionToken?.Trim()))
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
            // the following catch statements attempt to filter out expected exceptions (e.g., due to naturally lacking internet connections)
            // from those that are fixable errors within the app. expected exceptions are logged but not reported to the app center, whereas
            // all others are reported to the app center. our approach is to whitelist excepted exceptions as we see them appear in the app
            // center, in order to ensure that we don't miss any fixable errors.
            catch (HttpRequestException ex)
            {
                bool logged = false;

                if (ex.InnerException is WebException)
                {
                    WebException webException = ex.InnerException as WebException;

                    if (IGNORED_WEB_EXCEPTION_STATUSES.Contains(webException.Status))
                    {
                        LogKmsEnvelopeException(ex);
                        logged = true;
                    }
                }

                if (!logged)
                {
                    ReportKmsEnvelopeException(ex);
                }

                throw ex;
            }
            catch (WebException webException)
            {
                if (IGNORED_WEB_EXCEPTION_STATUSES.Contains(webException.Status))
                {
                    LogKmsEnvelopeException(webException);
                }
                else
                {
                    ReportKmsEnvelopeException(webException);
                }

                throw webException;
            }
            catch (OperationCanceledException ex)
            {
                LogKmsEnvelopeException(ex);

                throw ex;
            }
            catch (Exception ex)
            {
                ReportKmsEnvelopeException(ex);

                throw ex;
            }
        }

        private void LogKmsEnvelopeException(Exception ex)
        {
            SensusServiceHelper.Get().Logger.Log("Non-reportable exception when running KMS-based envelope encryption:  " + ex.Message, LoggingLevel.Normal, GetType());
        }

        private void ReportKmsEnvelopeException(Exception ex)
        {
            SensusException.Report("Reportable exception when running KMS-based envelope encryption:  " + ex.Message, ex);
        }
    }
}