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

using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SensusService.Probes.Apps
{
    public abstract class RunningAppsProbe : PollingProbe
    {
        private int _maxAppsPerPoll;

        [EntryIntegerUiProperty("Max Apps / Poll:", true, 3)]
        public int MaxAppsPerPoll
        {
            get { return _maxAppsPerPoll; }
            set
            {
                if (value != _maxAppsPerPoll)
                {
                    _maxAppsPerPoll = value;
                    OnPropertyChanged();
                }
            }
        }

        protected sealed override string DefaultDisplayName
        {
            get { return "Running Applications"; }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get { return 1000 * 60; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(RunningAppsDatum); }
        }

        public RunningAppsProbe()
        {
            _maxAppsPerPoll = 10;
        }

        protected abstract List<RunningAppsDatum> GetRunningAppsData();

        protected sealed override IEnumerable<Datum> Poll()
        {
            List<RunningAppsDatum> data = GetRunningAppsData();

            if (data != null && data.Count > _maxAppsPerPoll)
                data = data.GetRange(0, _maxAppsPerPoll);

            return data;
        }
    }
}
