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
using Sensus.UI.UiProperties;
using System.Threading;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using Sensus.Exceptions;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;
using Sensus.Extensions;
using Sensus.Models;
using System.Net.Http;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// 
    /// The Amazon S3 Remote Data Store allows Sensus to upload data from the device to [Amazon's Simple Storage Service (S3)](https://aws.amazon.com/s3). The 
    /// S3 service is a simple, non-relational storage system that is relatively cheap, easy to use, and robust.
    /// 
    /// # Prerequisites
    /// 
    ///   * Sign up for an account with Amazon Web Services, if you don't have one already. The [Free Tier](https://aws.amazon.com/free) is sufficient.
    ///   * Install the [AWS Command Line Interface(CLI)](https://aws.amazon.com/cli).
    ///   * Download and unzip our [AWS configuration scripts](https://github.com/predictive-technology-laboratory/sensus/raw/develop/Scripts/ConfigureAWS.zip).
    ///   * Run the following command to configure an S3 bucket for use within a Sensus Amazon S3 Remote Data Store, where `NAME` is an informative name
    ///     (alphanumerics and dashes only) and `REGION` is the region in which your bucket will reside (e.g., `us-east-1`):
    /// 
    ///     ```
    ///     ./configure-s3.sh NAME REGION
    ///     ```
    /// 
    ///   * The previous command will create a bucket as well as an IAM group and user with write-only access to the bucket. If successful, the command will 
    ///     output something like the following:
    /// 
    ///     ```
    ///     Done. Details:
    ///       Sensus S3 bucket:  test-bucket-eee8ef46-5d6a-4508-b745-e6635d195a85
    ///       Sensus S3 IAM account:  XXXX:XXXX
    ///     ```
    /// 
    ///   * The bucket and IAM account produced on the final line should be kept confidential. Use these values as <see cref="Bucket"/> and 
    ///     <see cref="IamAccountString"/>, respectively.
    /// 
    /// # Downloading Data from Amazon S3
    /// 
    /// Install the [AWS Command Line Interface](http://aws.amazon.com/cli). Assuming you have created and populated an S3 bucket named `BUCKET` and 
    /// a folder named `FOLDER`, you can download all of your Sensus data in a few different ways:
    /// 
    ///   1. You can use the functions (e.g., `sensus.sync.from.aws.s3`) in the [SensusR](https://cran.r-project.org/web/packages/SensusR/index.html) package.
    ///   1. You can execute the following command to download everything to a directory named `data` on your desktop:
    /// 
    ///      ```
    ///      aws s3 cp --recursive s3://BUCKET/FOLDER ~/data
    ///      ```
    /// 
    ///   1. You can run [`dowload-from-s3.sh`](https://raw.githubusercontent.com/predictive-technology-laboratory/sensus/master/Scripts/ConfigureAWS/dowload-from-s3.sh).
    ///   1. You can use a third-party application like [Bucket Explorer](http://www.bucketexplorer.com) to browse and download data from Amazon S3.
    /// 
    /// # Deconfiguration
    /// 
    /// If you are finished collecting data and you would like to prevent any future data submission, you can deconfigure the IAM group and user
    /// with the following command, where `BUCKET` corresponds to the Sensus S3 bucket name created above:
    /// 
    ///   ```
    ///   ./deconfigure-s3.sh BUCKET
    ///   ```
    /// 
    /// The preceding command will not delete your bucket or data.
    /// 
    /// </summary>
    public class ManagedAmazonS3RemoteDataStore : AmazonS3DataStore
    {
        private string _accountServiceURL;
        private string _credentialsServiceURL;
        private string _participantPassword;
        private AccountCredentials _accountCredentials;

        /// <summary>
        /// The AWS region in which <see cref="Bucket"/> resides (e.g., us-east-2).
        /// </summary>
        /// <value>The region.</value>
        [ListUiProperty(null, true, 1, new object[] { "us-east-2", "us-east-1", "us-west-1", "us-west-2", "ca-central-1", "ap-south-1", "ap-northeast-2", "ap-southeast-1", "ap-southeast-2", "ap-northeast-1", "eu-central-1", "eu-west-1", "eu-west-2", "sa-east-1" }, true)]
        public string Region
        {
            get
            {
                return _region;
            }
            set
            {
                _region = value;
            }
        }

        /// <summary>
        /// The AWS S3 bucket in which data should be stored. This is the bucket identifier output by the steps described in the summary for this class.
        /// </summary>
        /// <value>The bucket.</value>
        [EntryStringUiProperty(null, true, 2, true)]
        public string Bucket
        {
            get
            {
                return _bucket;
            }
            set
            {
                if (value != null)
                {
                    value = value.Trim().ToLower();  // bucket names must be lowercase.
                }

                _bucket = value;
            }
        }

        /// <summary>
        /// The folder within <see cref="Bucket"/> where data should be stored.
        /// </summary>
        /// <value>The folder.</value>
        [EntryStringUiProperty(null, true, 3, false)]
        public string Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                if (value != null)
                {
                    value = value.Trim().Trim('/');
                }

                _folder = value;
            }
        }

        /// <summary>
        /// Alternative URL to use for S3, instead of the default. Use this to set up [SSL certificate pinning](xref:ssl_pinning).
        /// </summary>
        /// <value>The pinned service URL.</value>
        [EntryStringUiProperty("Pinned S3 Service URL:", true, 5, false)]
        public string PinnedServiceURL
        {
            get
            {
                return _pinnedServiceURL;
            }
            set
            {
                if (value != null)
                {
                    value = value.Trim();

                    if (value == "")
                    {
                        value = null;
                    }
                    else
                    {
                        if (!value.ToLower().StartsWith("https://"))
                        {
                            value = "https://" + value;
                        }
                    }
                }

                _pinnedServiceURL = value;
            }
        }

        /// <summary>
        /// Pinned SSL public encryption key associated with <see cref="PinnedServiceURL"/>. Use this to set up [SSL certificate pinning](xref:ssl_pinning).
        /// </summary>
        /// <value>The pinned public key.</value>
        [EntryStringUiProperty("Pinned S3 Public Key:", true, 6, false)]
        public string PinnedPublicKey
        {
            get
            {
                return _pinnedPublicKey;
            }
            set
            {
                _pinnedPublicKey = value?.Trim().Replace("\n", "").Replace(" ", "");
            }
        }

        /// <summary>
        /// Service end point URL that is called to set up the account to be used with the <see cref="CredentialsServiceURL"/>.  
        /// It will fill {0} with the device id and {1} with the participant id.  
        /// For instance https://www.service.url/account?deviceId={0}&participantId={1}.
        /// </summary>
        /// <value>The account service URL.</value>
        [EntryStringUiProperty("Account Service URL:", true, 7, false)]
        public string AccountServiceURL
        {
            get { return _accountServiceURL; }
            set { _accountServiceURL = value?.Trim(); }
        }

        /// <summary>
        /// Service end point URL that is called to set up the credentials that are used with the Amazon S3.
        /// This uses the account id and password that comes from the <see cref="AccountServiceURL"/>.  
        /// It will fill {0} with the participant id and {1} with the password.  
        /// For instance https://www.service.url/credentials?participantId={0}&password={1}.
        /// </summary>
        /// <value>The account service URL.</value>
        [EntryStringUiProperty("Credentials Service URL:", true, 7, false)]
        public string CredentialsServiceURL
        {
            get { return _credentialsServiceURL; }
            set { _credentialsServiceURL = value?.Trim(); }
        }

        [JsonIgnore]
        public override bool CanRetrieveWrittenData
        {
            get
            {
                return true;
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get
            {
                return "Managed Amazon S3";
            }
        }

        public ManagedAmazonS3RemoteDataStore()
        {
            _region = _bucket = _folder = null;
            _pinnedServiceURL = null;
            _pinnedPublicKey = null;
            _putCount = _successfulPutCount = 0;
        }
        public override void Start()
        {
            ConfirmCredentials(); //make sure we valid credentials before we initializeS3
            base.Start();
        }
        public override AmazonS3Client InitializeS3()
        {
            ConfirmCredentials(); //make sure we valid credentials before we initializeS3
            return base.InitializeS3();
        }  
        private async Task RefreshAccount()
        {
            var deviceId = SensusServiceHelper.Get().DeviceId;
            if(string.IsNullOrWhiteSpace(Protocol.ParticipantId))
            {
                Protocol.ParticipantId = Guid.NewGuid().ToString("n");
            }
            var url = string.Format(_accountServiceURL, deviceId, Protocol.ParticipantId);
            var account = await GetJsonObjectFromUrl<Account>(url);
            if(Protocol.ParticipantId != account.participantId)
            {
                Protocol.ParticipantId = account.participantId;
            }
            _participantPassword = account.password;
        }
        private void ConfirmCredentials()
        {
            var t = Task.Run(async () =>
                    {
                        if (string.IsNullOrWhiteSpace(Protocol.ParticipantId) || string.IsNullOrWhiteSpace(_participantPassword))
                        {
                            await RefreshAccount();
                        }
                        if (_accountCredentials == null || _accountCredentials.IsExpired)
                        {
                            var url = string.Format(_credentialsServiceURL, Protocol.ParticipantId, _participantPassword);
                            _accountCredentials = await GetJsonObjectFromUrl<AccountCredentials>(url);
                            _iamAccessKey = _accountCredentials.accessKeyId;
                            _iamSecretKey = _accountCredentials.secretAccessKey;
                        }
                    });
            t.Wait();
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
                rVal = JsonConvert.DeserializeObject<T>(response);

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