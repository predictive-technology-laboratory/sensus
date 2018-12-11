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
using Sensus.Notifications;

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
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private const string DATA_DIRECTORY = "data";
        private const string PUSH_NOTIFICATIONS_DIRECTORY = "push-notifications";
        private const string ADAPTIVE_EMA_POLICIES_DIRECTORY = "adaptive-ema-policies";

        private string _region;
        private string _bucket;
        private string _folder;
        private string _iamAccessKey;
        private string _iamSecretKey;
        private string _pinnedServiceURL;
        private string _pinnedPublicKey;
        private int _putCount;
        private int _successfulPutCount;

        /// <summary>
        /// The AWS region in which <see cref="Bucket"/> resides (e.g., us-east-2).
        /// </summary>
        /// <value>The region.</value>
        [ListUiProperty(null, true, 1, new object[] { "us-east-2", "us-east-1", "us-west-1", "us-west-2", "ca-central-1", "ap-south-1", "ap-northeast-2", "ap-southeast-1", "ap-southeast-2", "ap-northeast-1", "eu-central-1", "eu-west-1", "eu-west-2", "sa-east-1", "us-gov-west-1" }, true)]
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
        /// The IAM user's access and secret keys output by the steps described in the summary for this class.
        /// </summary>
        /// <value>The iam account string.</value>
        [EntryStringUiProperty("IAM Account:", true, 4, true)]
        public string IamAccountString
        {
            get
            {
                return _iamAccessKey + ":" + _iamSecretKey;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string[] parts = value.Split(':');
                    if (parts.Length == 2)
                    {
                        _iamAccessKey = parts[0].Trim();
                        _iamSecretKey = parts[1].Trim();
                    }
                }
            }
        }

        /// <summary>
        /// Alternative URL to use for S3, instead of the default. Use this to set up [SSL certificate pinning](xref:ssl_pinning).
        /// </summary>
        /// <value>The pinned service URL.</value>
        [EntryStringUiProperty("Pinned Service URL:", true, 5, false)]
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
        [EntryStringUiProperty("Pinned Public Key:", true, 6, false)]
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
                return "Amazon S3";
            }
        }

        public override string StorageDescription
        {
            get
            {
                return base.StorageDescription ?? "Data will be transmitted " + TimeSpan.FromMilliseconds(WriteDelayMS).GetIntervalString().ToLower();
            }
        }

        public AmazonS3RemoteDataStore()
        {
            _region = _bucket = _folder = null;
            _pinnedServiceURL = null;
            _pinnedPublicKey = null;
            _putCount = _successfulPutCount = 0;
        }

        public override async Task StartAsync()
        {
            if (_pinnedServiceURL != null)
            {
                // ensure that we have a pinned public key if we're pinning the service URL
                if (string.IsNullOrWhiteSpace(_pinnedPublicKey))
                {
                    throw new Exception("Ensure that a pinned public key is provided to the AWS S3 remote data store.");
                }
                // set up a certificate validation callback if we're pinning and have a public key
                else
                {
                    ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationCallback;
                }
            }

            if (string.IsNullOrWhiteSpace(_iamAccessKey) || string.IsNullOrWhiteSpace(_iamSecretKey))
            {
                throw new Exception("Must specify an IAM account within the S3 remote data store.");
            }

            // start base last so we're set up for any callbacks that get scheduled
            await base.StartAsync();
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sllPolicyErrors)
        {
            if (certificate == null)
            {
                return false;
            }

            if (certificate.Subject == "CN=" + _pinnedServiceURL.Substring("https://".Length))
            {
                return Convert.ToBase64String(certificate.GetPublicKey()) == _pinnedPublicKey;
            }
            else
            {
                return true;
            }
        }

        private AmazonS3Client InitializeS3()
        {
            AWSConfigs.LoggingConfig.LogMetrics = false;  // getting many uncaught exceptions from AWS S3 related to logging metrics
            AmazonS3Config clientConfig = new AmazonS3Config();
            clientConfig.ForcePathStyle = true;  // when using pinning via CloudFront reverse proxy, the bucket name is prepended to the host if the path style is not used. the resulting host does not exist for our reverse proxy, causing DNS name resolution errors. by using the path style, the bucket is appended to the reverse-proxy host and everything goes through fine.

            if (_pinnedServiceURL == null)
            {
                clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(_region);
            }
            else
            {
                clientConfig.ServiceURL = _pinnedServiceURL;
            }

            return new AmazonS3Client(_iamAccessKey, _iamSecretKey, clientConfig);
        }

        public override async Task WriteDataStreamAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();

                await PutAsync(s3, stream, DATA_DIRECTORY + "/" + (string.IsNullOrWhiteSpace(_folder) ? "" : _folder + "/") + (string.IsNullOrWhiteSpace(Protocol.ParticipantId) ? "" : Protocol.ParticipantId + "/") + name, contentType, cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();
                string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);
                byte[] datumJsonBytes = Encoding.UTF8.GetBytes(datumJSON);
                MemoryStream dataStream = new MemoryStream(datumJsonBytes);

                await PutAsync(s3, dataStream, GetDatumKey(datum), "application/json", cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task SendPushNotificationTokenAsync(string token, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                // send the token
                s3 = InitializeS3();
                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                MemoryStream dataStream = new MemoryStream(tokenBytes);

                await PutAsync(s3, dataStream, GetPushNotificationTokenKey(), "text/plain", cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task DeletePushNotificationTokenAsync(CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                // send an empty data stream to clear the token. we don't have delete access.
                s3 = InitializeS3();

                await PutAsync(s3, new MemoryStream(), GetPushNotificationTokenKey(), "text/plain", cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        private string GetPushNotificationTokenKey()
        {
            // the key is device- and protocol-specific, providing us a way to quickly disable all PNs (i.e., by clearing the token file).
            return PUSH_NOTIFICATIONS_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId + ":" + Protocol.Id;
        }

        public override async Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();
                byte[] requestJsonBytes = Encoding.UTF8.GetBytes(request.JSON);
                MemoryStream dataStream = new MemoryStream(requestJsonBytes);

                await PutAsync(s3, dataStream, GetPushNotificationRequestKey(request), "application/json", cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            await DeletePushNotificationRequestAsync(request.Id, cancellationToken);
        }

        public override async Task DeletePushNotificationRequestAsync(string id, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                // send an empty data stream to clear the request. we don't have delete access.
                s3 = InitializeS3();

                await PutAsync(s3, new MemoryStream(), GetPushNotificationRequestKey(id), "text/plain", cancellationToken);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        private string GetPushNotificationRequestKey(PushNotificationRequest request)
        {
            return GetPushNotificationRequestKey(request.Id);
        }

        private string GetPushNotificationRequestKey(string pushNotificationRequestId)
        {
            return PUSH_NOTIFICATIONS_DIRECTORY + "/" + pushNotificationRequestId + ".json";
        }

        private async Task PutAsync(AmazonS3Client s3, Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            _putCount++;

            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucket,
                    CannedACL = S3CannedACL.BucketOwnerFullControl,  // without this, the bucket owner will not have access to the uploaded data
                    InputStream = stream,
                    Key = key,
                    ContentType = contentType
                };

                HttpStatusCode putStatus = (await s3.PutObjectAsync(putRequest, cancellationToken)).HttpStatusCode;

                if (putStatus == HttpStatusCode.OK)
                {
                    _successfulPutCount++;
                }
                else
                {
                    throw new Exception("Bad status code:  " + putStatus);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.TrustFailure)
                {
                    string message = "A trust failure has occurred between Sensus and the AWS S3 endpoint. This is likely the result of a failed match between the server's public key and the pinned public key within the Sensus AWS S3 remote data store.";
                    SensusException.Report(message, ex);
                }

                throw ex;
            }
            catch (Exception ex)
            {
                string message = "Failed to write data stream to Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message + " " + ex.Message, LoggingLevel.Normal, GetType());
                throw new Exception(message, ex);
            }
        }

        public override string GetDatumKey(Datum datum)
        {
            return DATA_DIRECTORY + "/" + (string.IsNullOrWhiteSpace(_folder) ? "" : _folder + "/") + (string.IsNullOrWhiteSpace(Protocol.ParticipantId) ? "" : Protocol.ParticipantId + "/") + datum.GetType().Name + "/" + datum.Id + ".json";
        }

        public override async Task<T> GetDatumAsync<T>(string datumKey, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();

                Stream responseStream = (await s3.GetObjectAsync(_bucket, datumKey, cancellationToken)).ResponseStream;
                T datum = null;
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string datumJSON = reader.ReadToEnd().Trim();
                    datumJSON = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(datumJSON);
                    datum = Datum.FromJSON(datumJSON) as T;
                }

                return datum;
            }
            catch (Exception ex)
            {
                string message = "Failed to get datum from Amazon S3:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                throw new Exception(message);
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task<string> GetScriptAgentPolicyAsync(CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();

                Stream responseStream = (await s3.GetObjectAsync(_bucket, ADAPTIVE_EMA_POLICIES_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId, cancellationToken)).ResponseStream;

                string policyJSON;
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    policyJSON = reader.ReadToEnd().Trim();
                }

                return policyJSON;
            }
            finally
            {
                DisposeS3(s3);
            }
        }

        public override async Task StopAsync()
        {
            await base.StopAsync();

            // remove the callback
            if (_pinnedServiceURL != null && !string.IsNullOrWhiteSpace(_pinnedPublicKey))
            {
                ServicePointManager.ServerCertificateValidationCallback -= ServerCertificateValidationCallback;
            }
        }

        private void DisposeS3(AmazonS3Client s3)
        {
            if (s3 != null)
            {
                try
                {
                    s3.Dispose();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to dispose Amazon S3 client:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Put Success", Convert.ToString(_successfulPutCount.RoundToWholePercentageOf(_putCount, 5)) }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return result;
        }
    }
}
