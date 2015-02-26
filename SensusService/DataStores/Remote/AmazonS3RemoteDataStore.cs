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
using DataNuage.Aws;
using SensusUI.UiProperties;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text;

namespace SensusService.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        /// <summary>
        /// Amazon s3 datum contract resolver. Ignores any unnecessary JSON fields within Datum objects.
        /// </summary>
        private class AmazonS3DatumContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization).Where(property => property.PropertyName != typeof(Datum).GetProperty("Id").Name).ToList();
            }
        }

        private S3 _s3;
        private string _bucket;
        private string _folder;
        private string _accessKey;
        private string _secretKey;
        private AmazonS3DatumContractResolver _jsonContractResolver;

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
                _folder = value;
            }
        }           

        [EntryStringUiProperty("Access Key:", true, 4)]
        public string AccessKey
        {
            get
            {
                return _accessKey;
            }
            set
            {
                _accessKey = value;
            }
        }

        [EntryStringUiProperty("Secret Key:", true, 5)]
        public string SecretKey
        {
            get
            {
                return _secretKey;
            }
            set
            {
                _secretKey = value;
            }
        }

        protected override string DisplayName
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
            _jsonContractResolver = new AmazonS3DatumContractResolver();
        }

        public override void Start()
        {
            lock (_locker)
            {
                _s3 = new S3(_accessKey, _secretKey);
                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data)
        {
            List<Datum> committedData = new List<Datum>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            Dictionary<string, StringBuilder> datumTypeJsonString = new Dictionary<string, StringBuilder>();
            Dictionary<string, List<Datum>> datumTypeDataSubset = new Dictionary<string, List<Datum>>();
            foreach (Datum datum in data)
            {
                string datumType = datum.GetType().Name;

                StringBuilder jsonString;
                if (!datumTypeJsonString.TryGetValue(datumType, out jsonString))
                {
                    jsonString = new StringBuilder();
                    datumTypeJsonString.Add(datumType, jsonString);
                }

                jsonString.Append(datum.GetJSON(_jsonContractResolver) + Environment.NewLine);

                List<Datum> dataSubset;
                if(!datumTypeDataSubset.TryGetValue(datumType, out dataSubset))
                {
                    dataSubset = new List<Datum>();
                    datumTypeDataSubset.Add(datumType, dataSubset);
                }

                dataSubset.Add(datum);
            }

            foreach(string datumType in datumTypeJsonString.Keys)
            {
                try
                {
                    _s3.PutObjectAsync(_bucket, _folder.Trim('/') + "/" + datumType + "/" + Guid.NewGuid(), datumTypeJsonString[datumType].ToString(), contentType:"application/json").Wait();
                    committedData.AddRange(datumTypeDataSubset[datumType]);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Amazon S3 bucket \"" + _bucket + "\" in " + (DateTimeOffset.UtcNow - startTime).TotalSeconds + " seconds.", LoggingLevel.Verbose, GetType());

            return committedData;
        }
    }
}

