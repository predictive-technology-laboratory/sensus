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
using SensusService.Probes;
using SensusUI.UiProperties;
using System.Threading;

namespace SensusService.Probes.User.Empatica
{
    public abstract class EmpaticaWristbandProbe : ListeningProbe
    {
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

        public override Type DatumType
        {
            get
            {
                return typeof(EmpaticaWristbandDatum);
            }
        }

        protected override string DefaultDisplayName
        {
            get
            {
                return "Empatica Wristband";
            }
        }

        public EmpaticaWristbandProbe()
        {
            MaxDataStoresPerSecond = 100000; // empatica has a high data rate...readings will drop below ~5000/sec
        }

        protected override void Initialize()
        {
            base.Initialize();

            #region authentication

            if (string.IsNullOrWhiteSpace(EmpaticaKey))
                throw new Exception("Failed to start Empatica probe:  Empatica API key must be supplied.");

            try
            {
                Authenticate();
            }
            catch (Exception ex)
            {
                string message = "Failed to authenticate with Empatica API key \"" + EmpaticaKey + "\":  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                throw new Exception(message);
            }

            #endregion

            #region bluetooth

            try
            {
                StartBluetooth();
            }
            catch (Exception ex)
            {
                string message = "Failed to start Bluetooth:  " + ex.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                throw new Exception(message);
            }

            #endregion
        }

        protected abstract void Authenticate();

        protected abstract void StartBluetooth();

        protected override void StartListening()
        {
            DiscoverAndConnectDevices();
        }

        public abstract void DiscoverAndConnectDevices();

        protected override void StopListening()
        {
            DisconnectDevices();
        }

        protected abstract void DisconnectDevices();
    }
}