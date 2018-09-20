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
using System.Linq;

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
        private PushNotificationHub _hub;

        public string Id
        {
            get { return _id; }
        }

        public string DeviceId
        {
            get { return _deviceId; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
        }

        public PushNotificationHub Hub
        {
            get { return _hub; }
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
                           "\"time\":" + _time.ToUnixTimeSeconds() + "," +
                           "\"hub\":" + JsonConvert.ToString(_hub.ToString()) +
                       "}";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Notifications.PushNotificationRequest"/> class, targeting another
        /// device (i.e., inter-device push notification request).
        /// </summary>
        /// <param name="protocol">Protocol.</param>
        /// <param name="title">Title.</param>
        /// <param name="body">Body.</param>
        /// <param name="sound">Sound.</param>
        /// <param name="command">Command.</param>
        /// <param name="time">Time.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="format">Format.</param>
        public PushNotificationRequest(Protocol protocol, string title, string body, string sound, string command, DateTimeOffset time, string deviceId, PushNotificationRequestFormat format)
        {
            // not all push notification requests are associated with a protocol (e.g., the app-level health test). because push notifications are
            // currently tied to the remote data store of the protocol, we don't currently support PNRs without a protocol.
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            // configuring azure push notifications supercede firebase
            if (protocol.AzurePushNotificationsEnabled)
            {
                _hub = PushNotificationHub.Azure;
            }
            else if (protocol.EnableFirebaseCloudMessagingPushNotifications)
            {
                _hub = PushNotificationHub.FirebaseCloudMessaging;
            }
            else
            {
                throw new Exception("Neither Azure nor FCM push notification requests are enabled for the protocol.");
            }

            _id = Guid.NewGuid().ToString();
            _protocol = protocol;
            _title = title;
            _body = body;
            _sound = sound;
            _command = command;
            _time = time;
            _deviceId = deviceId;
            _format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Notifications.PushNotificationRequest"/> class, targeting the current 
        /// device (i.e., a callback-style push notification request).
        /// </summary>
        /// <param name="protocol">Protocol.</param>
        /// <param name="title">Title.</param>
        /// <param name="body">Body.</param>
        /// <param name="sound">Sound.</param>
        /// <param name="command">Command.</param>
        /// <param name="time">Time.</param>
        public PushNotificationRequest(Protocol protocol, string title, string body, string sound, string command, DateTimeOffset time)
            : this(protocol, title, body, sound, command, time, SensusServiceHelper.Get().DeviceId, GetLocalFormat())
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