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
using SensusUI.UiProperties;
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
using Newtonsoft.Json;
using System.Net;

namespace SensusService.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private string _bucket;
        private string _folder;
        private string _cognitoIdentityPoolId;

        [EntryStringUiProperty("Bucket:", true, 2)]
        public string Bucket
        {
            get
            {
                return _bucket;
            }
            set
            {
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
                _cognitoIdentityPoolId = value;
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
        }

        private AmazonS3Client InitializeS3()
        {
            AWSConfigs.LoggingConfig.LogMetrics = false;  // getting many uncaught exceptions from AWS S3:  https://insights.xamarin.com/app/Sensus-Production/issues/351
            RegionEndpoint amazonRegion = RegionEndpoint.GetBySystemName(_cognitoIdentityPoolId.Substring(0, _cognitoIdentityPoolId.IndexOf(":")));
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(_cognitoIdentityPoolId, amazonRegion);
            return new AmazonS3Client(credentials, amazonRegion);
        }

        protected override Task<List<Datum>> CommitDataAsync(List<Datum> data, CancellationToken cancellationToken)
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
                                // use datum ID in key for retrieval later.
                                string key = datumType + "/" + datum.Id + ".json";

                                // the JSON for each participation reward datum must be indented so that cross-platform type conversion will work if/when the datum is retrieved.
                                string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);

                                try
                                {
                                    if ((await PutJsonAsync(s3, key, "[" + Environment.NewLine + datumJSON + Environment.NewLine + "]", cancellationToken)).HttpStatusCode == HttpStatusCode.OK)
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

                            string key = datumType + "/" + Guid.NewGuid() + ".json";

                            StringBuilder json = datumTypeJSON[datumType];
                            json.Append(Environment.NewLine + "]");

                            try
                            {
                                if ((await PutJsonAsync(s3, key, json.ToString(), cancellationToken)).HttpStatusCode == HttpStatusCode.OK)
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

        private Task<PutObjectResponse> PutJsonAsync(AmazonS3Client s3, string key, string json, CancellationToken cancellationToken)
        {
            PutObjectRequest putRequest = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = _folder + "/" + key.Trim('/'),  // trim '/' in case folder is blank
                ContentBody = json,
                ContentType = "application/json"
            };

            return s3.PutObjectAsync(putRequest, cancellationToken);
        }

        public override async Task<T> GetDatum<T>(string datumId, CancellationToken cancellationToken)
        {
            AmazonS3Client s3 = null;

            try
            {
                s3 = InitializeS3();

                string key = (_folder + "/" + typeof(T).Name + "/" + datumId + ".json").Trim('/');
                Stream responseStream = (await s3.GetObjectAsync(_bucket, key, cancellationToken)).ResponseStream;
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