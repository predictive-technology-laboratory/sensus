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

        private int _scanDurationMS;

        protected List<BluetoothDeviceProximityDatum> EncounteredDeviceData { get; }

        /// <summary>
        /// The length of time to scan for devices in proximity (in milliseconds). Note that, if the <see cref="Protocol"/>
        /// is configured to use [push notifications](xref:push_notifications), then this should not be set above 20 seconds 
        /// on iOS as background execution is limited to 30 seconds total. It should also not be set to less than 10 seconds
        /// as the scan can take at least this long to pick up nearby devices. The recommended value is 20000ms.
        /// </summary>
        /// <value>The scan time ms.</value>
        [EntryIntegerUiProperty("Scan Duration (MS):", true, 5, true)]
        public int ScanDurationMS
        {
            get
            {
                return _scanDurationMS;
            }
            set
            {
                if (value < 5000)
                {
                    value = 5000;
                }

                _scanDurationMS = value;
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
            _scanDurationMS = (int)TimeSpan.FromSeconds(20).TotalMilliseconds;
            EncounteredDeviceData = new List<BluetoothDeviceProximityDatum>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (!await SensusServiceHelper.Get().EnableBluetoothAsync(true, "Sensus uses Bluetooth, which is being used in one of your studies."))
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable BLE in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Bluetooth not enabled. Cannot start Bluetooth probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override async Task ProtectedStartAsync()
        {
            await base.ProtectedStartAsync();

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

        protected sealed override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            try
            {
                // start a new scan
                SensusServiceHelper.Get().Logger.Log("Scanning...", LoggingLevel.Normal, GetType());
                StopScan();
                StartScan();

                // wait for scanning results to arrive. we're not going to stop the scan, as it will continue 
                // in the background on both android and iOS and continue to deliver results, which will be
                // collected upon next poll.
                await Task.Delay(ScanDurationMS, cancellationToken);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while scanning:  " + ex, LoggingLevel.Normal, GetType());
            }

            // create a new list to return any data that have accumulated (prevents cross-thread modification)
            List<Datum> dataToReturn;

            lock (EncounteredDeviceData)
            {
                dataToReturn = EncounteredDeviceData.Cast<Datum>().ToList();
                EncounteredDeviceData.Clear();
            }

            // let the system know that we polled but didn't get any data
            if (dataToReturn.Count == 0)
            {
                dataToReturn.Add(null);
            }

            return dataToReturn;
        }

        protected abstract void StartScan();

        public sealed override async Task StopAsync()
        {
            await base.StopAsync();

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
