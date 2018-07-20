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
using Sensus.Exceptions;

namespace Sensus
{
    public class PushNotificationRequest
    {
        public static string GetFormatString(PushNotificationRequestFormat format)
        {
            if (format == PushNotificationRequestFormat.FirebaseCloudMessaging)
            {
                return "gcm";
            }
            else if (format == PushNotificationRequestFormat.ApplePushNotificationService)
            {
                return "apple";
            }
            else
            {
                SensusException.Report("Unrecognized push notification request format:  " + format);
                return "";
            }
        }

        private Protocol _protocol;
        private string _message;
        private PushNotificationRequestFormat _format;
        private DateTimeOffset _time;

        public string JSON
        {
            get
            {
                return "{" +
                           "\"device\":\"" + SensusServiceHelper.Get().DeviceId + "\"," +
                           "\"protocol\":\"" + _protocol.Id + "\"," +
                           "\"message\":\"" + _message + "\"," +
                           "\"format\":\"" + GetFormatString(_format) + "\"," +
                           "\"time\":\"" + _time.ToUnixTimeSeconds() + "\"" +
                       "}";
            }
        }

        public PushNotificationRequest(Protocol protocol, string message, PushNotificationRequestFormat format, DateTimeOffset time)
        {
            _protocol = protocol;
            _message = message;
            _format = format;
            _time = time;
        }
    }
}