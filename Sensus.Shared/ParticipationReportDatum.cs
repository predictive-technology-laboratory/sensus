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

using Sensus.Probes;
using System;
using System.Linq;
using System.Collections.Generic;
//using Newtonsoft.Json;

namespace Sensus
{
    /// <summary>
    /// Provides participation rates on a per-<see cref="Probe"/> basis. For example, for a <see cref="PollingProbe"/>
    /// set to poll 10 times per day with a <see cref="Protocol.ParticipationHorizon"/> of 10 days, we would expect to
    /// see 100 pollings at full participation. If the device only records 67 pollings, then the participation rate
    /// would be 67%. The definition of participation rate is different for <see cref="ListeningProbe"/>s, which are
    /// designed to be running continuously. Here, participation is defined to be the fraction of the 
    /// <see cref="Protocol.ParticipationHorizon"/> (e.g., 10 days) for which the <see cref="ListeningProbe"/> was 
    /// turned on.
    /// </summary>
    public class ParticipationReportDatum : Datum
    {
        private Dictionary<string, double> _probeParticipation;

        //[JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, double> ProbeParticipation
        {
            get
            {
                return _probeParticipation;
            }
            set
            {
                _probeParticipation = value;
            }
        }

        public override string DisplayDetail
        {
            get { return ""; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is always empty.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ParticipationReportDatum()
        {
            _probeParticipation = new Dictionary<string, double>();
        }

        public ParticipationReportDatum(DateTimeOffset timestamp, Protocol protocol)
            : base(timestamp)
        {
            _probeParticipation = new Dictionary<string, double>();
            ProtocolId = protocol.Id;
            ParticipantId = protocol.ParticipantId;

            List<Tuple<Probe, double?>> probeParticipations = protocol.Probes.Select(probe => new Tuple<Probe, double?>(probe, probe.GetParticipation()))
                                                                            .Where(probeParticipation => probeParticipation.Item2.HasValue)
                                                                            .OrderBy(probeParticipation => probeParticipation.Item1.GetType().FullName).ToList();

            foreach (Tuple<Probe, double?> probeParticipation in probeParticipations)
            {
                _probeParticipation.Add(probeParticipation.Item1.GetType().FullName, probeParticipation.Item2.Value);
            }
        }
    }
}
