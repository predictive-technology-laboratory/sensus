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

        public override float? Participation
        {
            get
            {
                if (EnabledOnFirstProtocolStart.GetValueOrDefault(false))
                {
                    #if __ANDROID__
                    long dayMS = 60000 * 60 * 24;
                    long participationHorizonMS = Protocol.ParticipationHorizonDays * dayMS;
                    float fullParticipationHealthTests = participationHorizonMS  / (float)SensusServiceHelper.HEALTH_TEST_DELAY_MS;
                    return HealthTestTimes.Count(healthTestTime => healthTestTime >= Protocol.ParticipationHorizon) / fullParticipationHealthTests;
                    #elif __IOS__
                    if (StartDateTime == null)
                        return 0;
                    else
                        return ((float)(DateTime.Now - StartDateTime.GetValueOrDefault()).TotalSeconds) / (float)new TimeSpan(Protocol.ParticipationHorizonDays, 0, 0, 0).TotalSeconds;
                    #else
                    #error "Unimplemented platform"
                    #endif
                }
                else
                    return null;
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
