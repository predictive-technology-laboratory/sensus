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
using Sensus.Extensions;

namespace Sensus.Authentication
{
    public class AccountService
    {
        private const string ACCOUNT_SERVICE_PAGE = "/createaccount?deviceId={0}&participantId={1}";
        private const string CREDENTIALS_SERVICE_PAGE = "/getcredentials?participantId={0}&password={1}";

        private readonly string _baseServiceUrl;
        private readonly string _accountServiceURL;
        private readonly string _credentialsServiceURL;

        private string _participantId;
        private string _participantPassword;
        private string _lastProtocolURL;
        private string _lastProtocolId;
        private AccountCredentials _lastCredentials;

        public string LastProtocolURL
        {
            get
            {
                return _lastProtocolURL;
            }
        }

        public string LastProtocolId
        {
            get
            {
                return _lastProtocolId;
            }
        }

        public bool HasAccount
        {
            get { return _participantId != null && _participantPassword != null; }
        }

        public bool HasValidCredentials
        {
            get { return HasAccount && _lastCredentials != null && _lastCredentials.expirationDateTime < DateTimeOffset.UtcNow; }
        }

        public AccountService(string baseServiceUrl)
        {
            _baseServiceUrl = baseServiceUrl;
            _accountServiceURL = _baseServiceUrl + ACCOUNT_SERVICE_PAGE;
            _credentialsServiceURL = _baseServiceUrl + CREDENTIALS_SERVICE_PAGE;
        }

        public async Task<Account> GetAccount(string deviceId, string participantId)
        {
            string json = await string.Format(_accountServiceURL, deviceId, participantId).DownloadString();
            Account account = await json.DeserializeJsonAsync<Account>();

            _participantId = account?.participantId;
            _participantPassword = account?.password;
            _lastProtocolURL = account?.protocolURL;
            _lastProtocolId = account?.protocolId;

            return account;
        }

        public void ClearCredentials()
        {
            _lastCredentials = null;
        }

        public async Task<AccountCredentials> GetCredentials()
        {
            return await GetCredentials(_participantId, _participantPassword);
        }

        public async Task<AccountCredentials> GetCredentials(string participantId, string password, bool force = false)
        {
            if (_participantId != participantId || _participantPassword != password)
            {
                _participantId = participantId;
                _participantPassword = password;
                force = true;
            }

            if (force == true || HasValidCredentials == false)
            {
                string credentialsJSON = await string.Format(_credentialsServiceURL, participantId, password).DownloadString();
                _lastCredentials = await credentialsJSON.DeserializeJsonAsync<AccountCredentials>();
                _lastProtocolURL = _lastCredentials?.protocolURL;
                _lastProtocolId = _lastCredentials?.protocolId;
            }

            return _lastCredentials;
        }
    }
}