using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AppCenter.Analytics;
using Sensus.Exceptions;
using Sensus.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.DataStores.Remote
{
    public abstract class AmazonS3DataStore : RemoteDataStore
    {
        protected string _region;
        protected string _bucket;
        protected string _folder;
        protected string _iamAccessKey;
        protected string _iamSecretKey;
        protected string _pinnedServiceURL;
        protected string _pinnedPublicKey;
        protected int _putCount;
        protected int _successfulPutCount;

        public override void Start()
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
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();

            // remove the callback
            if (_pinnedServiceURL != null && !string.IsNullOrWhiteSpace(_pinnedPublicKey))
            {
                ServicePointManager.ServerCertificateValidationCallback -= ServerCertificateValidationCallback;
            }
        }


        public virtual bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sllPolicyErrors)
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

        public virtual AmazonS3Client InitializeS3()
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

        public override Task WriteDataStreamAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                AmazonS3Client s3 = null;

                try
                {
                    s3 = InitializeS3();

                    await Put(s3, stream, (string.IsNullOrWhiteSpace(_folder) ? "" : _folder + "/") + (string.IsNullOrWhiteSpace(Protocol.ParticipantId) ? "" : Protocol.ParticipantId + "/") + name, contentType, cancellationToken);
                }
                finally
                {
                    DisposeS3(s3);
                }
            });
        }

        public override Task WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                AmazonS3Client s3 = null;

                try
                {
                    s3 = InitializeS3();
                    string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);
                    byte[] datumJsonBytes = Encoding.UTF8.GetBytes(datumJSON);
                    MemoryStream dataStream = new MemoryStream();
                    dataStream.Write(datumJsonBytes, 0, datumJsonBytes.Length);
                    dataStream.Position = 0;

                    await Put(s3, dataStream, GetDatumKey(datum), "application/json", cancellationToken);
                }
                finally
                {
                    DisposeS3(s3);
                }
            });
        }

        private Task Put(AmazonS3Client s3, Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
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
            });
        }

        public override string GetDatumKey(Datum datum)
        {
            return (string.IsNullOrWhiteSpace(_folder) ? "" : _folder + "/") + (string.IsNullOrWhiteSpace(Protocol.ParticipantId) ? "" : Protocol.ParticipantId + "/") + datum.GetType().Name + "/" + datum.Id + ".json";
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

        public override bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = base.TestHealth(ref events);

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Put Success", Convert.ToString(_successfulPutCount.RoundToWholePercentageOf(_putCount, 5)) }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));

            return restart;
        }
    }
}
