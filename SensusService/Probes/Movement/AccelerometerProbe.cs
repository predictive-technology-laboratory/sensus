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
using System.Threading;

namespace SensusService.Probes.Movement
{
    /// <summary>
    /// Probes information about physical acceleration in x, y, and z directions.
    /// </summary>
    public abstract class AccelerometerProbe : ListeningProbe
    {
        private bool _stabilizing;

        protected bool Stabilizing
        {
            get { return _stabilizing; }
        }

        protected sealed override string DefaultDisplayName
        {
            get { return "Accelerometer"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(AccelerometerDatum); }
        }

        protected override void Initialize()
        {
            base.Initialize();

            _stabilizing = true;
        }

        protected override void StartListening()
        {
            // allow the accelerometer to stabilize...the first few readings can be extremely erratic
            new Thread(() =>
                {
                    Thread.Sleep(5000);
                    _stabilizing = false;
                    SensusServiceHelper.Get().Logger.Log("Accelerometer has finished stabilization period.", LoggingLevel.Normal, GetType());

                }).Start();
        }
    }
}