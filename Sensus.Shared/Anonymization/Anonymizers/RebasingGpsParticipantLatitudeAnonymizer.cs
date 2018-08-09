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

namespace Sensus.Anonymization.Anonymizers
{
    /// <summary>
    /// Anonymizer that operates by changing the base (origin) of the latitude 
    /// of a GPS coordinate pair. The base is chosen to be participant-specific.
    /// Thus, the resulting coordinates are only meaningful relative to other 
    /// coordinates within a single participant's data set. They have no meaning
    /// in absolute terms, and they have no meaning relative to other participants' 
    /// data.
    /// </summary>
    public class RebasingGpsParticipantLatitudeAnonymizer : RebasingGpsLatitudeAnonymizer
    {
        public override string DisplayText
        {
            get
            {
                return "Participant Level";
            }
        }

        public override double GetOrigin(Protocol protocol)
        {
            return protocol.GpsAnonymizationUserOrigin.Item1;
        }
    }
}