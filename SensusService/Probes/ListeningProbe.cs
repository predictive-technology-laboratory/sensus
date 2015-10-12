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

using SensusUI.UiProperties;
using System;
using System.Linq;

namespace SensusService.Probes
{
    public abstract class ListeningProbe : Probe
    {
        private float _maxDataStoresPerSecond;
        private bool _wakeLockAcquired;

        private readonly object _locker = new object();

        [EntryFloatUiProperty("Max Data / Second:", true, int.MaxValue)]
        public float MaxDataStoresPerSecond
        {
            get { return _maxDataStoresPerSecond; }
            set { _maxDataStoresPerSecond = value; }
        }

        protected override float? RawParticipation
        {
            get
            {
                
                #if __ANDROID__
                long dayMS = 60000 * 60 * 24;
                long participationHorizonMS = Protocol.ParticipationHorizonDays * dayMS;
                float fullParticipationHealthTests = participationHorizonMS / (float)SensusServiceHelper.HEALTH_TEST_DELAY_MS;
                return SuccessfulHealthTestTimes.Count(healthTestTime => healthTestTime >= Protocol.ParticipationHorizon) / fullParticipationHealthTests;
                #elif __IOS__
                // on ios, we cannot rely on the health test times to tell us how long and consistently the probe has been running. this is
                // because, unlike in android, ios does not let local notifications return to the app when the app is in the background. instead, 
                // the ios user must tap a notification or otherwise open the app in order for the health test to run. so the best we can do 
                // is keep track of when the probe was started (StartDateTime) and compute participation based on how long the probe has been in 
                // a running state. it is theoretically possible that the probe is in this running state but is somehow faulty and failing the 
                // health tests. thus, the approach is not perfect, but it's the best we can do on ios.
                if (StartDateTime == null)
                    return 0;
                else
                {
                    double runningSeconds = (DateTime.Now - StartDateTime.GetValueOrDefault()).TotalSeconds;
                    double participationHorizonSeconds = new TimeSpan(Protocol.ParticipationHorizonDays, 0, 0, 0).TotalSeconds;
                    return (float)(runningSeconds / participationHorizonSeconds);
                }
                #else
                #error "Unrecognized platform."
                #endif
            }
        }

        protected ListeningProbe()
        {
            _maxDataStoresPerSecond = 1;
            _wakeLockAcquired = false;
        }

        public sealed override void Start()
        {
            lock (_locker)
            {
                base.Start();

                StartListening();

                SensusServiceHelper.Get().KeepDeviceAwake();  // listening probes are inherently energy inefficient, since the device must stay awake to listen for them.
                _wakeLockAcquired = true;
            }
        }

        protected abstract void StartListening();

        public sealed override void Stop()
        {
            lock (_locker)
            {
                if (_wakeLockAcquired)
                {
                    SensusServiceHelper.Get().LetDeviceSleep();  // we can sleep now...whew!
                    _wakeLockAcquired = false;
                }
                
                base.Stop();

                StopListening();
            }
        }

        protected abstract void StopListening();

        public sealed override void StoreDatum(Datum datum)
        {
            float storesPerSecond = 1 / (float)(DateTimeOffset.UtcNow - MostRecentStoreTimestamp).TotalSeconds;
            if (storesPerSecond <= _maxDataStoresPerSecond)
                base.StoreDatum(datum);
        }

        public override void ClearForSharing()
        {
            base.ClearForSharing();

            _wakeLockAcquired = false;
        }
    }
}
