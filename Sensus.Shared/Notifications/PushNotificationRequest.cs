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
    /// See [this article](xref:push_notifications) for an overview of push notifications. This class reflects the representation
    /// of push notification requests submitted to the <see cref="DataStores.Remote.RemoteDataStore"/> for processing by the 
    /// push notification backend. See the [example](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/example-notification-request.json)
    /// for the format of such requests. The following values of the `command` field are available:
    /// 
    /// * <see cref="UPDATE_PROTOCOL_COMMAND"/>:  Upon receipt of this command, Sensus will fetch <see cref="Protocol"/> updates from
    ///   the <see cref="DataStores.Remote.RemoteDataStore"/> via <see cref="DataStores.Remote.RemoteDataStore.GetProtocolUpdatesAsync(System.Threading.CancellationToken)"/>. 
    ///   In the case of a <see cref="DataStores.Remote.AmazonS3RemoteDataStore"/>, Sensus will get the object at `BUCKET/protocol-updates/DEVICE-ID`, where
    ///   `BUCKET` is the bucket and `DEVICE-ID` is the device identifier. So, if one wishes to deliver this command, one should first upload the protocol
    ///   updates file to this location. The format of the file is shown in the [example](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Scripts/ConfigureAWS/example-protocol-updates.json).
    ///   The fields in this file are as follows:
    /// 
    ///     * property-type:  The fully qualified name of the type defining the property to be updated. For example, [Sensus.Probes.PollingProbe] contains
    ///       properties that govern the behavior of all such probes.
    ///     * property-name:  The name of the property within the type indicated by `property-type` that should be set. For example, 
    ///       [Sensus.Probes.PollingProbe.PollingSleepDurationMS] determines how long the [Sensus.Probes.PollingProbe] will sleep between taking
    ///       successive readings.
    ///     * target-type:  The fully qualified name of the type whose instances should be updated. For example, a value of [Sensus.Probes.Location.PollingLocationProbe]
    ///       would update the property indicated with the previous fields for just this particular probe, whereas a value of [Sensus.Probes.PollingProbe]
    ///       would update the property for all such probes.
    ///     * value:  Quoted string of the new value to be set for the above property and target objects.
    /// 
    /// * <see cref="UPDATE_SCRIPT_AGENT_POLICY_COMMAND"/>:  Upon receipt of this command, Sensus will fetch the policy from the <see cref="DataStores.Remote.RemoteDataStore"/>
    ///   via <see cref="DataStores.Remote.RemoteDataStore.GetScriptAgentPolicyAsync(System.Threading.CancellationToken)"/>. The content of the returned
    ///   JSON file will be passed to <see cref="Probes.User.Scripts.IScriptProbeAgent.SetPolicyAsync(string)"/>.
    /// </summary>
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

        /// <summary>
        /// Instruct Sensus to update the policy running within the <see cref="Probes.User.Scripts.ScriptProbe.Agent"/>.
        /// </summary>
        public const string UPDATE_SCRIPT_AGENT_POLICY_COMMAND = "UPDATE-EMA-POLICY";

        /// <summary>
        /// Instruct Sensus to retrieve and set <see cref="Protocol"/> updates.
        /// </summary>
        public const string UPDATE_PROTOCOL_COMMAND = "UPDATE-PROTOCOL";

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