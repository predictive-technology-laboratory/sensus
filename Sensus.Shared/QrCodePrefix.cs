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
        /// Sensus protocols. Following this prefix should be an internet-accessible URL from which to download a protocol file via HTTPS.
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
