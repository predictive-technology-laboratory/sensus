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
using System.Reflection;
using SensusUI.UiProperties;

namespace SensusService.Probes.User
{
    public class TimeTrigger
    {
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private TimeSpan _randomTriggerTime;
        private string _Id;
        private bool _repeatDaily;
        private bool _resetRerunsAfterWindow;
        private bool _complete;

        public TimeSpan StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }

        public TimeSpan EndTime
        {
            get { return _endTime; }
            set { _endTime = value; }
        }

        public TimeSpan RandomTriggerTime
        {
            get { return _randomTriggerTime; }
            set { _randomTriggerTime = value; }
        }

        public string ID
        {
            get { return _Id; }
            set { _Id = value; }
        }
            
        public bool RepeatDaily
        {
            get { return _repeatDaily; }
            set { _repeatDaily = value; }
        }

        public bool ResetRerunsAfterWindow
        {
            get { return _resetRerunsAfterWindow; }
            set { _resetRerunsAfterWindow = value; }
        }

        public bool Complete
        {
            get { return _complete; }
            set { _complete = value; }
        }

        public TimeTrigger(TimeSpan startTime, TimeSpan endTime, bool repeatDaily, bool resetRerunsAfterWindow)
        {
            _startTime = startTime;
            _endTime = endTime;
            _repeatDaily = repeatDaily;
            _resetRerunsAfterWindow = resetRerunsAfterWindow;
        }
    }
}

