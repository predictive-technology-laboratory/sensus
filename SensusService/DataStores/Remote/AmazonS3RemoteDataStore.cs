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

namespace SensusService.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private AmazonS3Client _s3;
        private string _bucket;
        private string _folder;
        private string _cognitoIdentityPoolId;

        private object _locker = new object();

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

        public override string DisplayName
        {
            get
            {
                return "Amazon S3";
            }
        }

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

        public override void Start()
        {
            lock (_locker)
            {
                RegionEndpoint amazonRegion = RegionEndpoint.GetBySystemName(_cognitoIdentityPoolId.Substring(0, _cognitoIdentityPoolId.IndexOf(":")));
                CognitoAWSCredentials credentials = new CognitoAWSCredentials(_cognitoIdentityPoolId, amazonRegion);
                _s3 = new AmazonS3Client(credentials, amazonRegion);

                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data, CancellationToken cancellationToken)
        {
            DateTimeOffset commitStartTime = DateTimeOffset.UtcNow;

            List<Datum> committedData = new List<Datum>();

            Dictionary<string, List<Datum>> datumTypeData = new Dictionary<string, List<Datum>>();
            Dictionary<string, StringBuilder> datumTypeJSON = new Dictionary<string, StringBuilder>();

            foreach (Datum datum in data)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                string datumType = datum.GetType().Name;
                string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer);

                // upload all participation reward data as single S3 objects so we can retrieve them individually at a later time for verification.
                if (datum is ParticipationRewardDatum)
                {
                    try
                    {
                        PutObjectRequest putRequest = new PutObjectRequest
                        {
                            BucketName = _bucket,
                            Key = (_folder + "/" + datumType + "/" + datum.Id + ".json").Trim('/'),  // trim '/' in case folder is blank
                            ContentBody = datumJSON,
                            ContentType = "application/json"
                        };

                        if (_s3.PutObjectAsync(putRequest, cancellationToken).Wait(60000, cancellationToken))
                            committedData.Add(datum);
                        else
                            throw new Exception("Timed out while sending data to S3.");
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }
                else
                {
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

            // commit all data batches by type
            foreach (string datumType in datumTypeJSON.Keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                StringBuilder json = datumTypeJSON[datumType];
                json.Append(Environment.NewLine + "]");

                try
                {
                    PutObjectRequest putRequest = new PutObjectRequest
                    {
                        BucketName = _bucket,
                        Key = (_folder + "/" + datumType + "/" + Guid.NewGuid() + ".json").Trim('/'),  // trim '/' in case folder is blank
                        ContentBody = json.ToString(),
                        ContentType = "application/json"
                    };

                    if (_s3.PutObjectAsync(putRequest, cancellationToken).Wait(60000, cancellationToken))
                        committedData.AddRange(datumTypeData[datumType]);
                    else
                        throw new Exception("Timed out while sending data to S3.");
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Amazon S3 bucket \"" + _bucket + "\" in " + (DateTimeOffset.UtcNow - commitStartTime).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());

            return committedData;
        }

        public override void Stop()
        {
            base.Stop();

            try
            {
                _s3.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}