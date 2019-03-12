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
    /// <summary>
    /// See [here](xref:push_notifications) for an overview of push notifications within Sensus. The <see cref="PushNotificationRequest"/>
    /// class represents push notification requests submitted to the <see cref="DataStores.Remote.RemoteDataStore"/> for processing by the 
    /// push notification backend. See the 
    /// [example](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/example-notification-request.json)
    /// for the format of such requests. See the constant fields in this class for more information on commands that may be sent to Sensus.
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

        /// <summary>
        /// Instruct Sensus to update the policy running within the <see cref="Probes.User.Scripts.ScriptProbe.Agent"/>. 
        /// Upon receipt of this command, Sensus will fetch the policy from the <see cref="DataStores.Remote.RemoteDataStore"/>
        /// via <see cref="DataStores.Remote.RemoteDataStore.GetScriptAgentPolicyAsync(System.Threading.CancellationToken)"/>. 
        /// The content of the returned JSON file will be passed to 
        /// <see cref="Probes.User.Scripts.IScriptProbeAgent.SetPolicyAsync(string)"/>. See 
        /// <see cref="DataStores.Remote.RemoteDataStore.GetScriptAgentPolicyAsync(System.Threading.CancellationToken)"/> for 
        /// information about this JSON file.
        /// </summary>
        public const string COMMAND_UPDATE_SCRIPT_AGENT_POLICY = "UPDATE-EMA-POLICY";

        /// <summary>
        /// Instruct Sensus to retrieve and set <see cref="Protocol"/> updates. Following this value should be a pipe
        /// followed by the update identifier. For example:
        /// 
        /// ```
        /// UPDATE-PROTOCOL|XXXX
        /// ````
        /// 
        /// Where `XXXX` is the update identifier. Upon receipt of this command, Sensus will fetch <see cref="Protocol"/> updates from
        /// the <see cref="DataStores.Remote.RemoteDataStore"/> via 
        /// <see cref="DataStores.Remote.RemoteDataStore.GetProtocolUpdatesAsync(string, System.Threading.CancellationToken)"/>. In the case of a 
        /// <see cref="DataStores.Remote.AmazonS3RemoteDataStore"/>, Sensus will get the S3 object at `BUCKET/protocol-updates/XXXX`, 
        /// where `BUCKET` is the bucket and `XXXX` is the update identifier. If one wishes to deliver this command, then one should first 
        /// upload the protocol updates file to this S3 location and then issue the push notification request with the above command. The 
        /// format of the protocol updates file is shown in the
        /// [example](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/example-protocol-updates.json). 
        /// 
        /// The fields in this file are as follows:
        /// 
        /// <ul>
        ///   <ul>
        ///   <li>
        ///   `property-type`:  The fully qualified name of the type defining the property to be updated. For example, 
        ///   [`Sensus.Probes.PollingProbe`](xref:Sensus.Probes.PollingProbe) contains properties that govern the behavior of all polling-style probes.
        ///   </li>
        ///   <li>
        ///   `property-name`:  The name of the property within the type indicated by `property-type` that should be set. For example, 
        ///   [`Sensus.Probes.PollingProbe.PollingSleepDurationMS`](xref:Sensus.Probes.PollingProbe.PollingSleepDurationMS) determines how long the 
        ///   <see cref="Probes.PollingProbe"/> will sleep between taking successive readings.
        ///   </li>
        ///   <li>
        ///   `target-type`:  The fully qualified name of the type whose instances should be updated. For example, a value of 
        ///   [`Sensus.Probes.Location.PollingLocationProbe`](xref:Sensus.Probes.Location.PollingLocationProbe) would update the property indicated 
        ///   with the previous fields for just the <see cref="Probes.Location.PollingLocationProbe"/>, whereas a value of 
        ///   [`Sensus.Probes.PollingProbe`](xref:Sensus.Probes.PollingProbe) would update the property for all probes that inherit from 
        ///   <see cref="Probes.PollingProbe"/>.
        ///   </li>
        ///   <li>
        ///   `value`:  Quoted string of the new value to be set for the above property and target objects.
        ///   </li>
        ///   </ul>
        /// </ul>
        /// </summary>
        public const string COMMAND_UPDATE_PROTOCOL = "UPDATE-PROTOCOL";

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
                           "\"format\":" + JsonConvert.ToString(GetAzureFormatIdentifier(_format)) + "," +
                           "\"creation-time\":" + _creationTime.ToUnixTimeSeconds() + "," +
                           "\"time\":" + _notificationTime.ToUnixTimeSeconds() +
                       "}";
            }
        }

        public PushNotificationRequest(string id,
                                       Protocol protocol,
                                       string title,
                                       string body,
                                       string sound,
                                       string command,
                                       DateTimeOffset notificationTime,
                                       string deviceId,
                                       PushNotificationRequestFormat format,
                                       Guid backendKey)
        {
            _id = id;
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _title = title;
            _body = body;
            _sound = sound;
            _command = command;
            _notificationTime = notificationTime;
            _deviceId = deviceId;
            _format = format;
            _creationTime = DateTimeOffset.UtcNow;
            _backendKey = backendKey;
        }
    }
}