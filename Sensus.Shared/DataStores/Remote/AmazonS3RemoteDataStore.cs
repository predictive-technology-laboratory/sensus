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
using Sensus.Shared.UI.UiProperties;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.S3;
using Amazon.CognitoIdentity;
using Amazon;
using Amazon.S3.Model;
using Xamarin;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Net;

namespace Sensus.Shared.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private string _bucket;
        private string _folder;
        private string _cognitoIdentityPoolId;
        private bool _compress;

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

        [OnOffUiProperty("Compress:", true, 5)]
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
        }

        private AmazonS3Client InitializeS3()
        {
            AWSConfigs.LoggingConfig.LogMetrics = false;  // getting many uncaught exceptions from AWS S3:  https://insights.xamarin.com/app/Sensus-Production/issues/351
            RegionEndpoint amazonRegion = RegionEndpoint.GetBySystemName(_cognitoIdentityPoolId.Substring(0, _cognitoIdentityPoolId.IndexOf(":")));
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(_cognitoIdentityPoolId, amazonRegion);
            return new AmazonS3Client(credentials, amazonRegion);
        }

        public override Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken)
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
                                break;

                            string datumType = datum.GetType().Name;

                            // upload all participation reward data as individual S3 objects so we can retrieve them individually at a later time for participation verification.
                            if (datum is ParticipationRewardDatum)
                            {
                                // the JSON for each participation reward datum must be indented so that cross-platform type conversion will work if/when the datum is retrieved.
                                string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);

                                try
                                {
                                    // do not compress the json. it's too small to do much good.
                                    if ((await PutJsonAsync(s3, GetDatumKey(datum), "[" + Environment.NewLine + datumJSON + Environment.NewLine + "]", false, cancellationToken)) == HttpStatusCode.OK)
                                        committedData.Add(datum);
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
                                if ((await PutJsonAsync(s3, key, json.ToString(), _compress, cancellationToken)) == HttpStatusCode.OK)
                                    committedData.AddRange(datumTypeData[datumType]);
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

        private Task<HttpStatusCode> PutJsonAsync(AmazonS3Client s3, string key, string json, bool compress, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = key + (compress ? ".gz" : ""),
                    ContentType = compress ? "application/gzip" : "application/json"
                };

                if (compress)
                {
                    // zip json string if option is selected -- from https://stackoverflow.com/questions/2798467/c-sharp-code-to-gzip-and-upload-a-string-to-amazon-s3
                    MemoryStream compressed = new MemoryStream();

                    using (GZipStream zip = new GZipStream(compressed, CompressionMode.Compress, true))
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        zip.Write(buffer, 0, buffer.Length);
                        zip.Flush();
                    }

                    compressed.Position = 0;

                    putRequest.InputStream = compressed;
                }
                else
                    putRequest.ContentBody = json;

                HttpStatusCode responseCode = (await s3.PutObjectAsync(putRequest, cancellationToken)).HttpStatusCode;

                if (responseCode == HttpStatusCode.OK)
                    MostRecentSuccessfulCommitTime = DateTime.Now;

                return responseCode;
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