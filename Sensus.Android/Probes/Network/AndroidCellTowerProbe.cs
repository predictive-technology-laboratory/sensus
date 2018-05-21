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

using Android.App;
using Android.Telephony;
using Sensus.Probes.Network;
using System;
using Sensus;
using Plugin.Permissions.Abstractions;

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
                StoreDatum(new CellTowerDatum(DateTimeOffset.UtcNow, cellTowerLocation));
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) == PermissionStatus.Granted)
            {
                _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
                if (_telephonyManager == null)
                {
                    throw new NotSupportedException("No telephony present.");
                }
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Cell tower location is not permitted on this device. Cannot start cell tower probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
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
