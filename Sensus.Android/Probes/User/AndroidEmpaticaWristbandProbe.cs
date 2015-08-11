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
using SensusService.Probes.User;
using Com.Empatica.Empalink;
using Com.Empatica.Empalink.Delegates;
using SensusUI.UiProperties;

namespace Sensus.Android.Probes.User
{
    public class AndroidEmpaticaWristbandProbe : EmpaticaWristbandProbe
    {
        private AndroidEmpaticaWristbandListener _listener;
        private string _empaticaKey;

        [EntryStringUiProperty("Empatica Key:", true, 10)]
        public string EmpaticaKey
        {
            get
            {
                return _empaticaKey;
            }
            set
            {
                _empaticaKey = value;
            }
        }

        public AndroidEmpaticaWristbandProbe()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            _listener = new AndroidEmpaticaWristbandListener();
        }

        protected override void StartListening()
        {
            _listener.Start(_empaticaKey);
        }

        protected override void StopListening()
        {
            _listener.Stop();
        }
    }
}