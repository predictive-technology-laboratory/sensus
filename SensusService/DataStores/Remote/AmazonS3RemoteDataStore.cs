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

namespace SensusService.DataStores.Remote
{
    public class AmazonS3RemoteDataStore : RemoteDataStore
    {
        private S3 _s3;
        private string _bucket;
        private string _folder;
        private string _accessKey;
        private string _secretKey;

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

            DateTimeOffset start = DateTimeOffset.UtcNow;

            foreach (Datum datum in data)
            {
                try
                {
                    _s3.PutObjectAsync(Bucket, _folder.Trim('/') + "/" + datum.GetType().Name + "/" + datum.Id, datum.JSON, contentType:"application/json").Wait();
                    committedData.Add(datum);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Amazon S3 bucket:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Amazon S3 bucket in " + (DateTimeOffset.UtcNow - start).TotalSeconds + " seconds.", LoggingLevel.Verbose, GetType());

            return committedData;
        }
    }
}

