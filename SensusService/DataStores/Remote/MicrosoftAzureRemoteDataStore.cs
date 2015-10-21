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

using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusService.Probes.Apps;
using SensusService.Probes.Communication;
using SensusService.Probes.Context;
using SensusService.Probes.Device;
using SensusService.Probes.Location;
using SensusService.Probes.Movement;
using SensusService.Probes.Network;
using SensusService.Probes.User;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SensusService.DataStores.Remote
{
    public class MicrosoftAzureRemoteDataStore : RemoteDataStore
    {
        private MobileServiceClient _client;
        private string _url;
        private string _key;

        private IMobileServiceTable<FacebookDatum> _facebookTable;
        private IMobileServiceTable<SmsDatum> _smsTable;
        private IMobileServiceTable<TelephonyDatum> _telephonyTable;
        private IMobileServiceTable<BluetoothDeviceProximityDatum> _bluetoothTable;
        private IMobileServiceTable<LightDatum> _lightTable;
        private IMobileServiceTable<SoundDatum> _soundTable;
        private IMobileServiceTable<BatteryDatum> _batteryTable;
        private IMobileServiceTable<ScreenDatum> _screenTable;
        private IMobileServiceTable<AltitudeDatum> _altitudeTable;
        private IMobileServiceTable<CompassDatum> _compassTable;
        private IMobileServiceTable<LocationDatum> _locationTable;
        private IMobileServiceTable<PointOfInterestProximityDatum> _pointOfInterestTable;
        private IMobileServiceTable<AccelerometerDatum> _accelerometerTable;
        private IMobileServiceTable<SpeedDatum> _speedTable;
        private IMobileServiceTable<CellTowerDatum> _cellTowerTable;
        private IMobileServiceTable<WlanDatum> _wlanTable;
        private IMobileServiceTable<ScriptDatum> _scriptTable;
        private IMobileServiceTable<ProtocolReportDatum> _protocolReportTable;

        private readonly object _locker = new object();

        [EntryStringUiProperty("URL:", true, 2)]
        public string URL
        {
            get { return _url; }
            set { _url = value; }
        }

        [EntryStringUiProperty("Key:", true, 2)]
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public override string DisplayName
        {
            get { return "Microsoft Azure"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return false; }
        }

        public override void Start()
        {
            lock (_locker)
            {
                _client = new MobileServiceClient(_url, _key);

                _facebookTable = _client.GetTable<FacebookDatum>();
                _smsTable = _client.GetTable<SmsDatum>();
                _telephonyTable = _client.GetTable<TelephonyDatum>();
                _bluetoothTable = _client.GetTable<BluetoothDeviceProximityDatum>();
                _lightTable = _client.GetTable<LightDatum>();
                _soundTable = _client.GetTable<SoundDatum>();
                _batteryTable = _client.GetTable<BatteryDatum>();
                _screenTable = _client.GetTable<ScreenDatum>();
                _altitudeTable = _client.GetTable<AltitudeDatum>();
                _compassTable = _client.GetTable<CompassDatum>();
                _locationTable = _client.GetTable<LocationDatum>();
                _pointOfInterestTable = _client.GetTable<PointOfInterestProximityDatum>();
                _accelerometerTable = _client.GetTable<AccelerometerDatum>();
                _speedTable = _client.GetTable<SpeedDatum>();
                _cellTowerTable = _client.GetTable<CellTowerDatum>();
                _wlanTable = _client.GetTable<WlanDatum>();
                _scriptTable = _client.GetTable<ScriptDatum>();

                _protocolReportTable = _client.GetTable<ProtocolReportDatum>();

                base.Start();
            }
        }

        protected override List<Datum> CommitData(List<Datum> data, CancellationToken cancellationToken)
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;

            List<Datum> committedData = new List<Datum>();

            foreach (Datum datum in data)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    if(datum is FacebookDatum)
                        _facebookTable.InsertAsync(datum as FacebookDatum).Wait();
                    else if (datum is SmsDatum)
                        _smsTable.InsertAsync(datum as SmsDatum).Wait();
                    else if (datum is TelephonyDatum)
                        _telephonyTable.InsertAsync(datum as TelephonyDatum).Wait();
                    else if (datum is BluetoothDeviceProximityDatum)
                        _bluetoothTable.InsertAsync(datum as BluetoothDeviceProximityDatum).Wait();
                    else if (datum is LightDatum)
                        _lightTable.InsertAsync(datum as LightDatum).Wait();
                    else if (datum is SoundDatum)
                        _soundTable.InsertAsync(datum as SoundDatum).Wait();
                    else if (datum is BatteryDatum)
                        _batteryTable.InsertAsync(datum as BatteryDatum).Wait();
                    else if (datum is ScreenDatum)
                        _screenTable.InsertAsync(datum as ScreenDatum).Wait();
                    else if (datum is AltitudeDatum)
                        _altitudeTable.InsertAsync(datum as AltitudeDatum).Wait();
                    else if (datum is CompassDatum)
                        _compassTable.InsertAsync(datum as CompassDatum).Wait();
                    else if (datum is LocationDatum)
                        _locationTable.InsertAsync(datum as LocationDatum).Wait();
                    else if (datum is PointOfInterestProximityDatum)
                        _pointOfInterestTable.InsertAsync(datum as PointOfInterestProximityDatum).Wait();
                    else if (datum is AccelerometerDatum)
                        _accelerometerTable.InsertAsync(datum as AccelerometerDatum).Wait();
                    else if (datum is SpeedDatum)
                        _speedTable.InsertAsync(datum as SpeedDatum).Wait();
                    else if (datum is CellTowerDatum)
                        _cellTowerTable.InsertAsync(datum as CellTowerDatum).Wait();
                    else if (datum is WlanDatum)
                        _wlanTable.InsertAsync(datum as WlanDatum).Wait();
                    else if (datum is ScriptDatum)
                        _scriptTable.InsertAsync(datum as ScriptDatum).Wait();
                    else if (datum is ProtocolReportDatum)
                        _protocolReportTable.InsertAsync(datum as ProtocolReportDatum).Wait();
                    else
                        throw new DataStoreException("Unrecognized Azure table:  " + datum.GetType().FullName);

                    committedData.Add(datum);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Error: Could not insert the item because an item with that id already exists.")
                        committedData.Add(datum);
                    else
                        SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Azure table:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Azure tables in " + (DateTimeOffset.UtcNow - start).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());

            return committedData;
        }

        public override void Stop()
        {
            lock (_locker)
            {
                // stop the commit thread
                base.Stop();

                // close azure client
                _client.Dispose();
            }
        }
    }
}
