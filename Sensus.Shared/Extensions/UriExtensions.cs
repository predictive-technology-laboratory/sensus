﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Net.Http;
using System.Threading.Tasks;

namespace Sensus.Extensions
{
    public static class UriExtensions
    {
        public static async Task<byte[]> DownloadBytes(this Uri uri)
        {
            byte[] downloadedBytes = null;

            using (HttpClient client = new HttpClient())
            {
                downloadedBytes = await client.GetByteArrayAsync(uri);
            }

            return downloadedBytes;
        }

        public static async Task<string> DownloadString(this Uri uri)
        {
            string downloadedString = null;

            using (HttpClient client = new HttpClient())
            {
                downloadedString = await client.GetStringAsync(uri);
            }

            return downloadedString;
        }
    }
}
