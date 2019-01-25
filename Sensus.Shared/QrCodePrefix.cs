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

namespace Sensus
{
    /// <summary>
    /// Prefixes to use when generating QR codes used by Sensus.
    /// </summary>
    public static class QrCodePrefix
    {
        /// <summary>
        /// Participation barcodes.
        /// </summary>
        public const string SENSUS_PARTICIPATION = "sensus-participation:";

        /// <summary>
        /// Sensus protocols. There are two types of Sensus protocol QR codes:
        /// 
        /// * Direct URL:  Following the prefix should be an internet-accessible URL from which to download a protocol file directly via HTTPS.
        /// 
        /// * Managed URL:  Following the prefix should be the following:
        ///   ```
        ///   managed:BASEURL:PARTICIPANT_ID
        ///   ```
        ///   where BASEURL is the URL to a Sensus authentication server (e.g., https://some.host.com or https://some.host.com:12345), and 
        ///   PARTICIPANT_ID is an (optional) participant ID. Read more about Sensus authentication servers [here](xref:authentication_servers).
        /// </summary>
        public const string SENSUS_PROTOCOL = "sensus-protocol:";

        /// <summary>
        /// Sensus participant identifiers. Following this prefix should be an arbitrary string denoting a participant.
        /// </summary>
        public const string SENSUS_PARTICIPANT_ID = "sensus-participant:";

        /// <summary>
        /// IAM credentials. Following this prefix should be REGION:ACCESSKEY:SECRET, denoting an IAM user.
        /// </summary>
        public const string IAM_CREDENTIALS = "iam-credentials:";

        /// <summary>
        /// Survey agents (see <see cref="Sensus.Probes.User.Scripts.ScriptProbe.Agent"/>). Following this prefix should be an internet-accessible URL from which to download an agent library via HTTPS.
        /// </summary>
        public const string SURVEY_AGENT = "survey-agent:";
    }
}
