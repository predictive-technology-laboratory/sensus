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
using Sensus.Extensions;

namespace Sensus.Authentication
{
    public class AuthenticationService
    {
        private const string CREATE_ACCOUNT_PATH = "/createaccount?deviceId={0}&participantId={1}";
        private const string GET_CREDENTIALS_PATH = "/getcredentials?participantId={0}&password={1}";

        private readonly string _createAccountURL;
        private readonly string _getCredentialsURL;
        
        /// <summary>
        /// Gets or sets the base service URL. Serialized so that the app can refresh information.
        /// </summary>
        /// <value>The base service URL.</value>
        public string BaseServiceURL { get; set; }

        /// <summary>
        /// Gets or sets the account. Not serialized to prevent storage of sensitive information.
        /// </summary>
        /// <value>The account.</value>
        [JsonIgnore]
        public Account Account { get; set; }

        /// <summary>
        /// Gets or sets the AWS S3 credentials. Not serialized to prevent storage of sensitive information.
        /// </summary>
        /// <value>The credentials.</value>
        [JsonIgnore]
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

            Account = accountJSON.DeserializeJson<Account>();

            await CheckForProtocolChangeAsync(Account.ProtocolId, new Uri(Account.ProtocolURL), Account.ParticipantId);

            return Account;
        }

        public async Task<AmazonS3Credentials> GetCredentialsAsync()
        {
            string credentialsJSON = await new Uri(string.Format(_getCredentialsURL, Account.ParticipantId, Account.Password)).DownloadString();

            AmazonS3Credentials = credentialsJSON.DeserializeJson<AmazonS3Credentials>();

            await CheckForProtocolChangeAsync(Account.ProtocolId, new Uri(Account.ProtocolURL), Account.ParticipantId);

            return AmazonS3Credentials;
        }

        private async Task CheckForProtocolChangeAsync(string desiredProtocolId, Uri desiredProtocolURI, string participantId)
        {
            if (Protocol.Id != desiredProtocolId)
            {
                bool startDesiredProtocol = Protocol.Running;

                await Protocol.StopAsync();
                await Protocol.DeleteAsync();

                // get desired protocol and wire it up with the current authentication service
                Protocol desiredProtocol = await Protocol.DeserializeAsync(desiredProtocolURI);
                desiredProtocol.ParticipantId = participantId;
                desiredProtocol.AuthenticationService = this;
                Protocol = desiredProtocol;

                // start the new protocol
                if (startDesiredProtocol)
                {
                    await desiredProtocol.StartAsync();
                }
            }
        }

        public void ClearCredentials()
        {
            AmazonS3Credentials = null;
        }
    }
}