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

using Android.Telephony;
using System;
using Android.Telephony.Cdma;
using Android.Telephony.Gsm;

namespace Sensus.Android.Probes.Network
{
    public class AndroidCellTowerChangeListener : PhoneStateListener
    {
        public event EventHandler<string> CellTowerChanged;

        public override void OnCellLocationChanged(CellLocation location)
        {
            if (CellTowerChanged != null)
            {
                string cellTowerInformation = null;

                if (location is CdmaCellLocation)
                {
                    CdmaCellLocation cdmaLocation = location as CdmaCellLocation;
                    cellTowerInformation = "[base_station_id=" + cdmaLocation.BaseStationId + "," +
                                            "base_station_lat=" + cdmaLocation.BaseStationLatitude + "," +
                                            "base_station_lon=" + cdmaLocation.BaseStationLongitude + "," +
                                            "network_id=" + cdmaLocation.NetworkId + "," +
                                            "system_id=" + cdmaLocation.SystemId + "]";
                }
                else if (location is GsmCellLocation)
                {
                    GsmCellLocation gsmLocation = location as GsmCellLocation;
                    cellTowerInformation = "[cid=" + gsmLocation.Cid + ",lac=" + gsmLocation.Lac + "]";
                }
                else
                {
                    cellTowerInformation = location?.ToString();
                }

                CellTowerChanged(this, cellTowerInformation);
            }
        }
    }
}