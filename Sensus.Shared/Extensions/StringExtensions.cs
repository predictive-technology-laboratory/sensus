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
using Newtonsoft.Json;
using Sensus.Context;

namespace Sensus.Extensions
{
    public static class StringExtensions
    {
        public static T DeserializeJson<T>(this string json) where T : class
        {
            Exception deserializeException = null;

            // deserialize objects on the main thread, since a looper is required to deserialize protocols on android.
            T deserializedObject = SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    // disable flash notifications when deserializing so we don't get any messages that result from properties 
                    // being set. this is particularly likely when deserializing protocols, which have several property settings
                    // that may trigger flash notifications.
                    SensusServiceHelper.Get().FlashNotificationsEnabled = false;

                    return JsonConvert.DeserializeObject<T>(json, SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                }
                catch (Exception ex)
                {
                    deserializeException = ex;
                    return null;
                }
                finally
                {
                    // ensure that flash notifications get reenabled
                    SensusServiceHelper.Get().FlashNotificationsEnabled = true;
                }
            });

            if (deserializeException != null)
            {
                throw deserializeException;
            }

            return deserializedObject;
        }
    }
}
