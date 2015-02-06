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

using Android.App;
using Android.Telephony;
using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerProbe : CellTowerProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidCellTowerChangeListener _cellTowerChangeListener;

        public AndroidCellTowerProbe()
        {
            _cellTowerChangeListener = new AndroidCellTowerChangeListener();
            _cellTowerChangeListener.CellTowerChanged += (o, cellTowerLocation) =>
                {
                    StoreDatum(new CellTowerDatum(this, DateTimeOffset.UtcNow, cellTowerLocation));
                };
        }

        protected override void Initialize()
        {
            base.Initialize();

            _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
            if (_telephonyManager == null)
                throw new Exception("No telephony present.");
        }

        protected override void StartListening()
        {
            _telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.CellLocation);
        }

        protected override void StopListening()
        {
            _telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.None);
        }
    }
}