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
using Newtonsoft.Json;
using Sensus.Context;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Context
{
    public abstract class BluetoothDeviceProximityProbe : ListeningProbe
    {
        public const string SERVICE_UUID = "AF2FB88A-9A79-4748-8DB6-9AC1F8F41B2B";
        public const string DEVICE_ID_CHARACTERISTIC_UUID = "2647AAAE-B7AC-4331-A3FF-0DF73288D3F7";

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will use additional power to report all updates.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will sleep and pause updates.";
            }
        }

        public sealed override string DisplayName
        {
            get { return "Bluetooth Encounters"; }
        }

        public override string CollectionDescription
        {
            get
            {
                return "Nearby Bluetooth Devices:  Upon encounter.";
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(BluetoothDeviceProximityDatum); }
        }

        protected sealed override void StartListening()
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth, which is being used in one of your studies."))
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable BLE in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Bluetooth not enabled. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // attempt to start the central. don't bail if this fails, since we might still be able to start the peripheral.
                try
                {
                    StartCentral();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting central:  " + ex.Message, LoggingLevel.Normal, GetType());
                    StopCentral();
                }

                // attempt to start the peripheral.
                try
                {
                    StartPeripheral();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
                    StopPeripheral();
                }
            });
        }

        protected abstract void StartCentral();

        protected abstract void StartPeripheral();

        protected sealed override void StopListening()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    StopCentral();
                }
                catch(Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping central:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                try
                {
                    StopPeripheral();
                }
                catch(Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping peripheral:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        protected abstract void StopCentral();

        protected abstract void StopPeripheral();

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }
    }
}
