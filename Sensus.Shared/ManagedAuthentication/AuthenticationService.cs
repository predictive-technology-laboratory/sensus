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

using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Extensions;

namespace Sensus.ManagedAuthentication
{
    public class AuthenticationService
    {
        private const string ACCOUNT_SERVICE_PAGE = "/createaccount?deviceId={0}&participantId={1}";
        private const string CREDENTIALS_SERVICE_PAGE = "/getcredentials?participantId={0}&password={1}";

        private readonly string _accountServiceURL;
        private readonly string _uploadCredentialsServiceURL;
        
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
        /// Gets or sets the upload credentials. Not serialized to prevent storage of sensitive information.
        /// </summary>
        /// <value>The upload credentials.</value>
        [JsonIgnore]
        public UploadCredentials UploadCredentials { get; set; }

        public AuthenticationService(string baseServiceUrl)
        {
            BaseServiceURL = baseServiceUrl;
            _accountServiceURL = baseServiceUrl + ACCOUNT_SERVICE_PAGE;
            _uploadCredentialsServiceURL = baseServiceUrl + CREDENTIALS_SERVICE_PAGE;
        }

        public async Task<Account> CreateAccount(string participantId = null)
        {
            string accountJSON = await string.Format(_accountServiceURL, SensusServiceHelper.Get().DeviceId, participantId).DownloadString();
            Account = await accountJSON.DeserializeJsonAsync<Account>();

            return Account;
        }

        public async Task<UploadCredentials> GetCredentials()
        {
            string credentialsJSON = await string.Format(_uploadCredentialsServiceURL, Account.ParticipantId, Account.Password).DownloadString();
            UploadCredentials = await credentialsJSON.DeserializeJsonAsync<UploadCredentials>();

            return UploadCredentials;
        }

        public void ClearCredentials()
        {
            UploadCredentials = null;
        }
    }
}