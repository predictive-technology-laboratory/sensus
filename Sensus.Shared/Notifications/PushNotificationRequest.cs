//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
        private DateTimeOffset _creationTime;
        private DateTimeOffset _notificationTime;

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
                           "\"creation-time\":" + _creationTime.ToUnixTimeSeconds() + "," +
                           "\"time\":" + _notificationTime.ToUnixTimeSeconds() +
                       "}";
            }
        }

        public PushNotificationRequest(Protocol protocol, string title, string body, string sound, string command, DateTimeOffset notificationTime, string deviceId, PushNotificationRequestFormat format)
        {
            _id = Guid.NewGuid().ToString();
            _protocol = protocol;
            _title = title;
            _body = body;
            _sound = sound;
            _command = command;
            _creationTime = DateTimeOffset.UtcNow;
            _notificationTime = notificationTime;
            _deviceId = deviceId;
            _format = format;

            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }
        }

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
