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
using Amazon.CognitoIdentity;
using Amazon;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using Sensus.Encryption;

namespace Sensus.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private string _bucket;
        private string _folder;
        private string _cognitoIdentityPoolId;
        private bool _compress;
        private bool _encrypt;
        private string _pinnedServiceURL;
        private string _pinnedPublicKey;

        [EntryStringUiProperty("Bucket:", true, 2)]
        public string Bucket
        {
            get
            {
                return _bucket;
            }
            set
            {
                if (value != null)
                    value = value.Trim();

                _bucket = value;
            }
        }

        [EntryStringUiProperty("Folder:", true, 3)]
        public string Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                if (value != null)
                    value = value.Trim().Trim('/');

                _folder = value;
            }
        }

        [EntryStringUiProperty("Cognito Pool Id:", true, 4)]
        public string CognitoIdentityPoolId
        {
            get
            {
                return _cognitoIdentityPoolId;
            }
            set
            {
                // newlines and spaces will cause problems when extracting the region and using it in the URL
                if (value != null)
                    value = value.Trim();

                _cognitoIdentityPoolId = value;
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
            _bucket = _folder = "";
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

            // ensure that we have a pinned public key if we're pinning the service
            if (_pinnedServiceURL != null && string.IsNullOrWhiteSpace(_pinnedPublicKey))
            {
                throw new Exception("Ensure that a pinned public key is provided to the AWS S3 remote data store.");
            }

            base.Start();
        }

        private AmazonS3Client InitializeS3()
        {
            AWSConfigs.LoggingConfig.LogMetrics = false;  // getting many uncaught exceptions from AWS S3:  https://insights.xamarin.com/app/Sensus-Production/issues/351

            RegionEndpoint amazonRegion = RegionEndpoint.GetBySystemName(_cognitoIdentityPoolId.Substring(0, _cognitoIdentityPoolId.IndexOf(":")));
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(_cognitoIdentityPoolId, amazonRegion);

            AmazonS3Config clientConfig = new AmazonS3Config();
            if (_pinnedServiceURL == null)
            {                
                clientConfig.RegionEndpoint = amazonRegion;
            }
            else
            {
                clientConfig.ServiceURL = _pinnedServiceURL;

                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (certificate == null)
                    {
                        return false;
                    }

                    return true;

                    return certificate.GetPublicKeyString() == _pinnedPublicKey;
                };
            }

            return new AmazonS3Client(credentials, clientConfig);
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
                            break;

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

                return (await s3.PutObjectAsync(putRequest, cancellationToken)).HttpStatusCode;
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