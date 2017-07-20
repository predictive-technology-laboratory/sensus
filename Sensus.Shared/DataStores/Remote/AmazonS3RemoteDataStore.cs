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
using System.Collections.Generic;
using Sensus.UI.UiProperties;
using System.Text;
using System.Threading;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using Sensus.Encryption;
using Sensus.Exceptions;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Sensus.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private string _region;
        private string _bucket;
        private string _folder;
        private bool _compress;
        private bool _encrypt;
        private string _pinnedServiceURL;
        private string _pinnedPublicKey;

        [ListUiProperty(null, true, 1, new object[] { "us-east-2", "us-east-1", "us-west-1", "us-west-2", "ca-central-1", "ap-south-1", "ap-northeast-2", "ap-southeast-1", "ap-southeast-2", "ap-northeast-1", "eu-central-1", "eu-west-1", "eu-west-2", "sa-east-1" })]
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

        [EntryStringUiProperty(null, true, 2)]
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

        [EntryStringUiProperty(null, true, 3)]
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

        [OnOffUiProperty(null, true, 5)]
        public bool Compress
        {
            get
            {
                return _compress;
            }
            set
            {
                _compress = value;
            }
        }

        [OnOffUiProperty("Encrypt (must set public encryption key on protocol in order to use):", true, 6)]
        public bool Encrypt
        {
            get
            {
                return _encrypt;
            }
            set
            {
                _encrypt = value;
            }
        }

        [EntryStringUiProperty("Pinned Service URL:", true, 7)]
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

        [EntryStringUiProperty("Pinned Public Key:", true, 8)]
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
        public override bool CanRetrieveCommittedData
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

        [JsonIgnore]
        public override bool Clearable
        {
            get
            {
                return false;
            }
        }

        public AmazonS3RemoteDataStore()
        {
            _region = _bucket = _folder = null;
            _compress = false;
            _encrypt = false;
            _pinnedServiceURL = null;
            _pinnedPublicKey = null;
        }

        public override void Start()
        {
            // ensure that we have a valid encryption setup if one is requested
            if (_encrypt)
            {
                try
                {
                    Protocol.AsymmetricEncryption.Encrypt("testing");
                }
                catch (Exception)
                {
                    throw new Exception("Ensure that a valid public key is set on the Protocol.");
                }
            }

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

            base.Start();
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

            return new AmazonS3Client(null, clientConfig);
        }

        protected override Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                AmazonS3Client s3 = null;

                try
                {
                    s3 = InitializeS3();

                    DateTimeOffset commitStartTime = DateTimeOffset.UtcNow;

                    List<Datum> committedData = new List<Datum>();

                    #region group data by type and get JSON for each datum
                    Dictionary<string, List<Datum>> datumTypeData = new Dictionary<string, List<Datum>>();
                    Dictionary<string, StringBuilder> datumTypeJSON = new Dictionary<string, StringBuilder>();

                    foreach (Datum datum in data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        string datumType = datum.GetType().Name;

                        // upload all participation reward data as individual S3 objects so we can retrieve them individually at a later time for participation verification.
                        if (datum is ParticipationRewardDatum)
                        {
                            // the JSON for each participation reward datum must be indented so that cross-platform type conversion will work if/when the datum is retrieved.
                            string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);

                            try
                            {
                                // do not compress the json. it's too small to do much good.
                                if ((await PutJsonAsync(s3, GetDatumKey(datum), "[" + Environment.NewLine + datumJSON + Environment.NewLine + "]", false, false, cancellationToken)) == HttpStatusCode.OK)
                                {
                                    committedData.Add(datum);
                                }
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }
                        else
                        {
                            string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, false);

                            // group all other data (i.e., other than participation reward data) by type for batch committal
                            List<Datum> dataSubset;
                            if (!datumTypeData.TryGetValue(datumType, out dataSubset))
                            {
                                dataSubset = new List<Datum>();
                                datumTypeData.Add(datumType, dataSubset);
                            }

                            dataSubset.Add(datum);

                            // add datum to its JSON array string
                            StringBuilder json;
                            if (!datumTypeJSON.TryGetValue(datumType, out json))
                            {
                                json = new StringBuilder("[" + Environment.NewLine);
                                datumTypeJSON.Add(datumType, json);
                            }

                            json.Append((dataSubset.Count == 1 ? "" : "," + Environment.NewLine) + datumJSON);
                        }
                    }
                    #endregion

                    #region commit all data, batched by type
                    foreach (string datumType in datumTypeJSON.Keys)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        string key = (_folder + "/" + datumType + "/" + Guid.NewGuid() + ".json").Trim('/');  // trim '/' in case folder is blank

                        StringBuilder json = datumTypeJSON[datumType];
                        json.Append(Environment.NewLine + "]");

                        try
                        {
                            if ((await PutJsonAsync(s3, key, json.ToString(), _compress, _encrypt, cancellationToken)) == HttpStatusCode.OK)
                            {
                                committedData.AddRange(datumTypeData[datumType]);
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                        }
                    }
                    #endregion

                    SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Amazon S3 bucket \"" + _bucket + "\" in " + (DateTimeOffset.UtcNow - commitStartTime).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());

                    return committedData;
                }
                finally
                {
                    DisposeS3(s3);
                }
            });
        }

        private Task<HttpStatusCode> PutJsonAsync(AmazonS3Client s3, string key, string json, bool compress, bool encrypt, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucket,
                    CannedACL = S3CannedACL.BucketOwnerFullControl,  // without this, the bucket owner will not have access to the uploaded data
                    Key = key
                };

                if (!compress && !encrypt)
                {
                    putRequest.ContentBody = json;
                    putRequest.ContentType = "application/json";
                }
                else
                {
                    byte[] inputStreamBytes = Encoding.UTF8.GetBytes(json);

                    if (encrypt)
                    {
                        // apply symmetric-key encryption to the JSON, and send the symmetric key to S3 encrypted using the public key we have
                        using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                        {
                            // ensure that we generate a 32-byte symmetric key and 16-byte IV (IV length is block size / 8)
                            aes.KeySize = 256;
                            aes.BlockSize = 128;
                            aes.GenerateKey();
                            aes.GenerateIV();

                            // apply symmetric-key encryption to our JSON
                            SymmetricEncryption symmetricEncryption = new SymmetricEncryption(aes.Key, aes.IV);
                            byte[] encryptedInputStreamBytes = symmetricEncryption.Encrypt(inputStreamBytes);

                            // encrypt the symmetric key and initialization vector using our asymmetric public key
                            byte[] encryptedKeyBytes = Protocol.AsymmetricEncryption.Encrypt(aes.Key);
                            byte[] encryptedIVBytes = Protocol.AsymmetricEncryption.Encrypt(aes.IV);

                            // write the new input stream...
                            using (MemoryStream newInputStream = new MemoryStream())
                            {
                                // ...encrypted symmetric key
                                byte[] encryptedKeyBytesLength = BitConverter.GetBytes(encryptedKeyBytes.Length);
                                newInputStream.Write(encryptedKeyBytesLength, 0, encryptedKeyBytesLength.Length);
                                newInputStream.Write(encryptedKeyBytes, 0, encryptedKeyBytes.Length);

                                // ...encrypted initialization vector
                                byte[] encryptedIVBytesLength = BitConverter.GetBytes(encryptedIVBytes.Length);
                                newInputStream.Write(encryptedIVBytesLength, 0, encryptedIVBytesLength.Length);
                                newInputStream.Write(encryptedIVBytes, 0, encryptedIVBytes.Length);

                                // ...encrypted JSON
                                newInputStream.Write(encryptedInputStreamBytes, 0, encryptedInputStreamBytes.Length);

                                // change the input stream bytes to those we just generated
                                inputStreamBytes = newInputStream.ToArray();
                                putRequest.Key += ".bin";
                            }
                        }
                    }

                    MemoryStream putRequestInputStream = new MemoryStream();

                    if (compress)
                    {
                        // zip json string if option is selected -- from https://stackoverflow.com/questions/2798467/c-sharp-code-to-gzip-and-upload-a-string-to-amazon-s3
                        using (GZipStream zip = new GZipStream(putRequestInputStream, CompressionMode.Compress, true))
                        {
                            zip.Write(inputStreamBytes, 0, inputStreamBytes.Length);
                            zip.Flush();
                        }

                        putRequest.ContentType = "application/gzip";
                        putRequest.Key += ".gz";
                    }
                    else
                    {
                        putRequestInputStream.Write(inputStreamBytes, 0, inputStreamBytes.Length);
                        putRequest.ContentType = "application/octet-stream";
                    }

                    // reset the stream and use it for the put request input
                    putRequestInputStream.Position = 0;
                    putRequest.InputStream = putRequestInputStream;
                }

                try
                {
                    return (await s3.PutObjectAsync(putRequest, cancellationToken)).HttpStatusCode;
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
            });
        }

        public override string GetDatumKey(Datum datum)
        {
            return (_folder + "/" + datum.GetType().Name + "/" + datum.Id + ".json").Trim('/');
        }

        public override async Task<T> GetDatum<T>(string datumKey, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();

                Stream responseStream = (await s3.GetObjectAsync(_bucket, datumKey, cancellationToken)).ResponseStream;
                T datum = null;
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string json = reader.ReadToEnd().Trim().Trim('[', ']');  // there will only be one datum in the array, so trim the braces and deserialize the datum.
                    json = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(json);
                    datum = Datum.FromJSON(json) as T;
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

        public override void Stop()
        {
            base.Stop();

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
    }
}