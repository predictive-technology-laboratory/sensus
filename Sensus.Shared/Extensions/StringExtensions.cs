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
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Context;

namespace Sensus.Extensions
{
    public static class StringExtensions
    {
        public static async Task<byte[]> DownloadBytes(this string uri)
        {
            byte[] downloadedBytes = null;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    downloadedBytes = await client.GetByteArrayAsync(uri);
                }
            }
            catch (Exception)
            { }

            return downloadedBytes;
        }

        public static async Task<string> DownloadString(this string uri)
        {
            string downloadedString = null;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    downloadedString = await client.GetStringAsync(uri);
                }
            }
            catch (Exception)
            { }

            return downloadedString;
        }

        public static async Task<T> DeserializeJsonAsync<T>(this string json) where T : class
        {
            T deserializedObject = null;

            try
            {
                // always deserialize objects on the main thread (e.g., since a looper is required to deserialize protocols
                // on android). also, disable flash notifications so we don't get any messages that result from properties 
                // being set.
                SensusServiceHelper.Get().FlashNotificationsEnabled = false;
                deserializedObject = SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(json, SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Error while deserializing " + typeof(T).Name + "from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(T));
                        return null;
                    }
                });

                if (deserializedObject == null)
                {
                    throw new Exception("Failed to deserialize " + typeof(T).Name + " from JSON.");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to deserialize " + typeof(T).Name + " from JSON:  " + ex.Message, LoggingLevel.Normal, typeof(T));
                await SensusServiceHelper.Get().FlashNotificationAsync("Failed to unpack " + typeof(T).Name + " from JSON:  " + ex.Message);
            }
            finally
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = true;
            }

            return deserializedObject;
        }
    }
}
