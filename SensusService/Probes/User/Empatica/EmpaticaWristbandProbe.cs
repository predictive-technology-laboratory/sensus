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
            MaxDataStoresPerSecond = 100000; // empatica has a high data rate
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrWhiteSpace(EmpaticaKey))
                throw new Exception("Failed to start Empatica probe:  Empatica API key must be supplied.");

            ManualResetEvent authenticationWait = new ManualResetEvent(false);
            Exception authenticationException = null;

            try
            {
                AuthenticateAsync(ex =>
                    {
                        authenticationException = ex;
                        authenticationWait.Set();
                    });
            }
            catch (Exception ex)
            {
                authenticationException = new Exception("Failed to start Empatica authentication:  " + ex.Message);
                authenticationWait.Set();
            }

            authenticationWait.WaitOne();

            if (authenticationException != null)
            {
                SensusServiceHelper.Get().Logger.Log(authenticationException.Message, LoggingLevel.Normal, GetType());
                throw authenticationException;
            } 
        }

        protected abstract void AuthenticateAsync(Action<Exception> callback);

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