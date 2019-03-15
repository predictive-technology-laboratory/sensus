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
using Newtonsoft.Json.Linq;

namespace Sensus.Notifications
{
    /// <summary>
    /// See [here](xref:push_notifications) for an overview of push notifications within Sensus. The <see cref="PushNotificationRequest"/>
    /// class represents push notification requests submitted to the <see cref="DataStores.Remote.RemoteDataStore"/> for processing by the 
    /// push notification backend. See the 
    /// [example](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/example-notification-request.json)
    /// for the JSON format of such requests.
    /// </summary>
    public class PushNotificationRequest
    {
        /// <summary>
        /// Gets the local format used by the device for push notifications.
        /// </summary>
        /// <returns>The local format.</returns>
        public static PushNotificationRequestFormat LocalFormat
        {
            get
            {
#if __ANDROID__
                return PushNotificationRequestFormat.FirebaseCloudMessaging;
#elif __IOS__
                return PushNotificationRequestFormat.ApplePushNotificationService;
#else
#error "Unrecognized platform."
#endif
            }
        }

        /// <summary>
        /// Gets the Azure format identifier for the native push notification service.
        /// </summary>
        /// <returns>The Azure format identifier.</returns>
        /// <param name="format">Format.</param>
        public static string GetAzureFormatIdentifier(PushNotificationRequestFormat format)
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
        private PushNotificationUpdate _update;
        private PushNotificationRequestFormat _format;
        private DateTimeOffset _creationTime;
        private DateTimeOffset _notificationTime;
        private Guid _backendKey;

        public string DeviceId
        {
            get { return _deviceId; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
        }

        public Guid BackendKey
        {
            get { return _backendKey; }
        }

        public JObject JSON
        {
            get
            {
                return JObject.Parse("{" +
                                         "\"id\":" + JsonConvert.ToString(_id) + "," +
                                         "\"device\":" + JsonConvert.ToString(_deviceId) + "," +
                                         "\"protocol\":" + JsonConvert.ToString(_protocol.Id) + "," +
                                         (_title == null ? "" : "\"title\":" + JsonConvert.ToString(_title) + ",") +
                                         (_body == null ? "" : "\"body\":" + JsonConvert.ToString(_body) + ",") +
                                         (_sound == null ? "" : "\"sound\":" + JsonConvert.ToString(_sound) + ",") +
                                         (_update == null ? "" : "\"update\":" + _update.JSON.ToString(Formatting.None) + ",") +
                                         "\"format\":" + JsonConvert.ToString(GetAzureFormatIdentifier(_format)) + "," +
                                         "\"creation-time\":" + _creationTime.ToUnixTimeSeconds() + "," +
                                         "\"time\":" + _notificationTime.ToUnixTimeSeconds() +
                                     "}");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Notifications.PushNotificationRequest"/> class. The
        /// intent of this instance will be to deliver user-targeted information to the device in the form of a 
        /// notification with a message title, body, etc.
        /// </summary>
        /// <param name="id">Identifier of the request. To update a previously issued request, submit a new request
        /// with the same identifier.</param>
        /// <param name="deviceId">Identifier of target device.</param>
        /// <param name="protocol">Target protocol.</param>
        /// <param name="title">Title.</param>
        /// <param name="body">Body.</param>
        /// <param name="sound">Sound.</param>
        /// <param name="format">Format.</param>
        /// <param name="notificationTime">Time to deliver the notification.</param>
        /// <param name="backendKey">Backend storage key. It is reasonable to supply a new <see cref="Guid"/> for each
        /// <see cref="PushNotificationRequest"/> instance.</param>
        public PushNotificationRequest(string id,
                                       string deviceId,
                                       Protocol protocol,
                                       string title,
                                       string body,
                                       string sound,
                                       PushNotificationRequestFormat format,
                                       DateTimeOffset notificationTime,
                                       Guid backendKey)
        {
            _id = id;
            _deviceId = deviceId;
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _title = title;
            _body = body;
            _sound = sound;
            _format = format;
            _notificationTime = notificationTime;
            _backendKey = backendKey;
            _creationTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.Notifications.PushNotificationRequest"/> class. The 
        /// intent of this instance will be to deliver a <see cref="PushNotificationUpdate"/> to the app. This update
        /// does not necessarily have any user-targeted content.
        /// </summary>
        /// <param name="id">Identifier of the request. To update a previously issued request, submit a new request
        /// with the same identifier.</param>
        /// <param name="deviceId">Identifier of target device.</param>
        /// <param name="protocol">Target protocol.</param>
        /// <param name="update">Update.</param>
        /// <param name="format">Format.</param>
        /// <param name="notificationTime">Time to deliver the notification.</param>
        /// <param name="backendKey">Backend storage key. It is reasonable to supply a new <see cref="Guid"/> for each
        /// <see cref="PushNotificationRequest"/> instance.</param>
        public PushNotificationRequest(string id,
                                       string deviceId,
                                       Protocol protocol,
                                       PushNotificationUpdate update,
                                       PushNotificationRequestFormat format,
                                       DateTimeOffset notificationTime,
                                       Guid backendKey)
            : this(id, deviceId, protocol, null, null, null, format, notificationTime, backendKey)
        {
            _update = update;
        }
    }
}