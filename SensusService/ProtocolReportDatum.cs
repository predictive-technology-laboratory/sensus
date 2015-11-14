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
using System.Collections.Generic;
using SensusService.Probes;
using System.Linq;
using System.Collections.ObjectModel;

namespace SensusService
{
    public class ProtocolReportDatum : Datum
    {
        private string _error;
        private string _warning;
        private string _misc;
        private string _operatingSystem;
        private Dictionary<string, float> _probeParticipation;

        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        public string Warning
        {
            get { return _warning; }
            set { _warning = value; }
        }

        public string Misc
        {
            get { return _misc; }
            set { _misc = value; }
        }

        public string OperatingSystem
        {
            get
            {
                return _operatingSystem; 
            }
            set
            {
                _operatingSystem = value;
            }
        }

        public Dictionary<string, float> ProbeParticipation
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
        /// For JSON deserialization.
        /// </summary>
        private ProtocolReportDatum()
        {
            _probeParticipation = new Dictionary<string, float>();
        }

        public ProtocolReportDatum(DateTimeOffset timestamp, string error, string warning, string misc, Protocol protocol)
            : base(timestamp)
        {
            _error = error == null ? "" : error;
            _warning = warning == null ? "" : warning;
            _misc = misc == null ? "" : misc;
            _operatingSystem = SensusServiceHelper.Get().OperatingSystem;
            _probeParticipation = new Dictionary<string, float>();

            foreach (Probe probe in protocol.Probes.Where(probe => probe.GetParticipation() != null).OrderBy(probe => probe.DisplayName))
                _probeParticipation.Add(probe.DisplayName, probe.GetParticipation().GetValueOrDefault());
        }

        public override string ToString()
        {
            return "Errors:  " + Environment.NewLine + _error + Environment.NewLine +
            "Warnings:  " + Environment.NewLine + _warning + Environment.NewLine +
            "Misc:  " + Environment.NewLine + _misc;
        }
    }
}
