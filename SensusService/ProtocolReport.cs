#region copyright
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
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService
{
    public class ProtocolReport : Datum
    {
        private string _error;
        private string _warning;
        private string _misc;

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

        public override string DisplayDetail
        {
            get { return ""; }
        }

        public ProtocolReport(DateTimeOffset timestamp, string error, string warning, string misc)
            : base(null, timestamp)
        {
            _error = error;
            _warning = warning;
            _misc = misc;
        }

        public override string ToString()
        {
            return "Errors:  " + Environment.NewLine + _error + Environment.NewLine +
                   "Warnings:  " + Environment.NewLine + _warning + Environment.NewLine +
                   "Misc:  " + Environment.NewLine + _misc;
        }
    }
}
