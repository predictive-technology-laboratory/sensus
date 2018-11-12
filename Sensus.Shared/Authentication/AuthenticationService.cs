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
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Exceptions;
using Sensus.Extensions;

namespace Sensus.Authentication
{
    /// <summary>
    /// Handles all interactions with an [authentication server](xref:authentication_servers).
    /// </summary>
    public class AuthenticationService
    {
        private const string CREATE_ACCOUNT_PATH = "/createaccount?deviceId={0}&participantId={1}";
        private const string GET_CREDENTIALS_PATH = "/getcredentials?participantId={0}&password={1}";

        private readonly string _createAccountURL;
        private readonly string _getCredentialsURL;
        
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
        public AmazonS3Credentials AmazonS3Credentials { get; set; }

        public Protocol Protocol { get; set; }

        public AuthenticationService(string baseServiceURL)
        {
            BaseServiceURL = baseServiceURL.Trim('/');

            _createAccountURL = BaseServiceURL + CREATE_ACCOUNT_PATH;
            _getCredentialsURL = BaseServiceURL + GET_CREDENTIALS_PATH;
        }

        public async Task<Account> CreateAccountAsync(string participantId = null)
        {
            string accountJSON = await new Uri(string.Format(_createAccountURL, SensusServiceHelper.Get().DeviceId, participantId)).DownloadString();

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

        public async Task<AmazonS3Credentials> GetCredentialsAsync()
        {
            // create account if we don't have one for some reason. under normal conditions we can expect to always have an account, 
            // as the account information is downloaded, attached to the protocol, and saved with the protocol when the protocol is started.
            if (Account == null)
            {
                await CreateAccountAsync();
            }

            string credentialsJSON = await new Uri(string.Format(_getCredentialsURL, Account.ParticipantId, Account.Password)).DownloadString();

            // create a new account if the password was bad. this also should not be possible.
            if (IsBadPasswordResponse(credentialsJSON))
            {
                await CreateAccountAsync();

                credentialsJSON = await new Uri(string.Format(_getCredentialsURL, Account.ParticipantId, Account.Password)).DownloadString();

                if (IsBadPasswordResponse(credentialsJSON))
                {
                    SensusException.Report("Received bad password response when getting credentials with newly created account.");
                    throw new NotImplementedException();
                }
            }

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
        }

        private bool IsBadPasswordResponse(string json)
        {
            return true;
        }

        public void ClearCredentials()
        {
            AmazonS3Credentials = null;
        }
    }
}