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

using Sensus.Probes;
using System;
using System.Linq;
using System.Collections.Generic;

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
