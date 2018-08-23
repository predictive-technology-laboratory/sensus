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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Sensus.UI.UiProperties;

namespace Sensus.Probes.Context
{
    public abstract class BluetoothDeviceProximityProbe : PollingProbe
    {
        public const string DEVICE_ID_CHARACTERISTIC_UUID = "2647AAAE-B7AC-4331-A3FF-0DF73288D3F7";

        private TimeSpan _scanDuration;

        protected List<BluetoothDeviceProximityDatum> EncounteredDeviceData { get; }

        /// <summary>
        /// The length of time to scan for devices in proximity.
        /// </summary>
        /// <value>The scan time ms.</value>
        [TimeUiProperty("Scan Duration (MS):", true, 20, true)]
        public TimeSpan ScanDuration
        {
            get
            {
                return _scanDuration;
            }
            set
            {
                if (value.TotalSeconds < 5)
                {
                    value = TimeSpan.FromSeconds(5);
                }

                _scanDuration = value;
            }
        }

        public sealed override string DisplayName
        {
            get { return "Bluetooth Encounters"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(BluetoothDeviceProximityDatum); }
        }

        public BluetoothDeviceProximityProbe()
        {
            _scanDuration = TimeSpan.FromSeconds(10);
            EncounteredDeviceData = new List<BluetoothDeviceProximityDatum>();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth, which is being used in one of your studies."))
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable BLE in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Bluetooth not enabled. Cannot start Bluetooth probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override void ProtectedStart()
        {
            base.ProtectedStart();

            try
            {
                SensusServiceHelper.Get().Logger.Log("Starting advertising.", LoggingLevel.Normal, GetType());
                StartAdvertising();
            }
            catch(Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while starting advertising:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        protected abstract void StartAdvertising();

        protected sealed override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            try
            {
                SensusServiceHelper.Get().Logger.Log("Scanning...", LoggingLevel.Normal, GetType());
                ScanAsync(cancellationToken).Wait();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while scanning:  " + ex, LoggingLevel.Normal, GetType());
            }
            finally
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("Stopping scan.", LoggingLevel.Normal, GetType());
                    StopScan();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while stopping scan:  " + ex, LoggingLevel.Normal, GetType());
                }
            }

            // create a new list to return any data that have accumulated (prevents cross-thread modification)
            List<BluetoothDeviceProximityDatum> dataToReturn;

            lock (EncounteredDeviceData)
            {
                dataToReturn = EncounteredDeviceData.ToList();
                EncounteredDeviceData.Clear();
            }

            // if we have no new data, return a null datum to signal to the storage system that the poll ran successfully (null won't actually be stored).
            if (dataToReturn.Count == 0)
            {
                dataToReturn.Add(null);
            }

            return dataToReturn;
        }

        protected abstract Task ScanAsync(CancellationToken cancellationToken);

        public sealed override void Stop()
        {
            base.Stop();

            try
            {
                SensusServiceHelper.Get().Logger.Log("Stopping scan.", LoggingLevel.Normal, GetType());
                StopScan();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping scan:  " + ex, LoggingLevel.Normal, GetType());
            }

            try
            {
                SensusServiceHelper.Get().Logger.Log("Stopping advertising.", LoggingLevel.Normal, GetType());
                StopAdvertising();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        protected abstract void StopScan();

        protected abstract void StopAdvertising();

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
