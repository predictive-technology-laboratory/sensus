using Sensus.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sensus
{
    public class AccountService
    {
        public AccountService(string baseServiceUrl)
        {
            _baseServiceUrl = baseServiceUrl;
            _accountServiceURL = _baseServiceUrl + ACCOUNT_SERVICE_PAGE;
            _credentialsServiceURL = _baseServiceUrl + CREDENTIALS_SERVICE_PAGE;
        }
        private const string ACCOUNT_SERVICE_PAGE = "/createaccount?deviceId={0}&participantId={1}";
        private const string CREDENTIALS_SERVICE_PAGE = "/getcredentials?participantId={0}&password={1}";
        private string _baseServiceUrl;
        private string _accountServiceURL;
        private string _credentialsServiceURL;

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
        
        public async Task<Account> GetAccount(string deviceId, string participantId)
        {
            var url = string.Format(_accountServiceURL, deviceId, participantId);
            var account = await GetJsonObjectFromUrl<Account>(url);
            _participantId = account?.participantId;
            _participantPassword = account?.password;
            _lastProtocolURL = account?.protocolURL;
            _lastProtocolId = account?.protocolId;
            return account;
        }

        public bool HasAccount
        {
            get { return _participantId != null && _participantPassword != null; }
        }

        public bool HasValidCredentials
        {
            get { return HasAccount && _lastCredentials != null &&_lastCredentials.expirationDateTime < DateTimeOffset.UtcNow;  }
        }

        public void ClearCredentials() { _lastCredentials = null; }
        public async Task<AccountCredentials> GetCredentials(bool force = false)
        {
            return await GetCredentials(_participantId, _participantPassword);
        }
        public async Task<AccountCredentials> GetCredentials(string participantId, string password, bool force = false)
        {
            if(_participantId != participantId || _participantPassword != password)
            {
                _participantId = participantId;
                _participantPassword = password;
                force = true;
            }

            if(force == true || HasValidCredentials == false)
            {
                var url = string.Format(_credentialsServiceURL, participantId, password);
                _lastCredentials = await GetJsonObjectFromUrl<AccountCredentials>(url);
                _lastProtocolURL = _lastCredentials?.protocolURL;
                _lastProtocolId = _lastCredentials?.protocolId;
            }

            return _lastCredentials;
        }

        private async Task<T> GetJsonObjectFromUrl<T>(string url)
        {

            T rVal = default(T);
            string response;

            HttpClient httpClient = new HttpClient();
            try
            {
                response = await httpClient.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception("Response was empty");
                }
                rVal = await Sensus.Protocol.DeserializeAsync<T>(response);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log($"Error getting json object {typeof(T).Name} from {url}:  {ex.Message}", LoggingLevel.Normal, GetType());
                throw;
            }
            finally
            {
                httpClient.Dispose();
                httpClient = null;
            }
            return rVal;
        }

    }
}
