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
using Newtonsoft.Json;

namespace Sensus.Notifications
{
    public class PushNotificationRequest
    {
        private static PushNotificationRequestFormat GetLocalFormat()
        {
#if __ANDROID__
            return PushNotificationRequestFormat.FirebaseCloudMessaging;
#elif __IOS__
            return PushNotificationRequestFormat.ApplePushNotificationService;
#else
#error "Unrecognized platform."
#endif
        }

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

        private string _id;
        private string _deviceId;
        private Protocol _protocol;
        private string _title;
        private string _body;
        private string _sound;
        private string _command;
        private PushNotificationRequestFormat _format;
        private DateTimeOffset _time;

        public string Id
        {
            get { return _id; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
        }

        public string JSON
        {
            get
            {
                return "{" +
                           "\"id\":" + JsonConvert.ToString(_id) + "," +
                           "\"device\":" + JsonConvert.ToString(_deviceId) + "," +
                           "\"protocol\":" + JsonConvert.ToString(_protocol.Id) + "," +
                           "\"title\":" + JsonConvert.ToString(_title) + "," +
                           "\"body\":" + JsonConvert.ToString(_body) + "," +
                           "\"sound\":" + JsonConvert.ToString(_sound) + "," +
                           "\"command\":" + JsonConvert.ToString(_command) + "," +
                           "\"format\":" + JsonConvert.ToString(GetFormatString(_format)) + "," +
                           "\"time\":" + _time.ToUnixTimeSeconds() +
                       "}";
            }
        }

        public PushNotificationRequest(string id, Protocol protocol, string title, string body, string sound, string command, DateTimeOffset time, string deviceId, PushNotificationRequestFormat format)
        {
            _id = id;
            _protocol = protocol;
            _title = title;
            _body = body;
            _sound = sound;
            _command = command;
            _time = time;
            _deviceId = deviceId;
            _format = format;
        }

        public PushNotificationRequest(string id, Protocol protocol, string title, string body, string sound, string command, DateTimeOffset time)
            : this(id, protocol, title, body, sound, command, time, SensusServiceHelper.Get().DeviceId, GetLocalFormat())
        {
        }

        public override bool Equals(object obj)
        {
            PushNotificationRequest other = obj as PushNotificationRequest;

            if (other == null)
            {
                return false;
            }
            else
            {
                return _id == other.Id;
            }
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}