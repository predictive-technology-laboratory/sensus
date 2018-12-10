//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
