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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using Sensus.Extensions;
using Sensus.UI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Context
{
    public abstract class BluetoothDeviceProximityProbe : PollingProbe
    {
        public const string DEVICE_ID_CHARACTERISTIC_UUID = "2647AAAE-B7AC-4331-A3FF-0DF73288D3F7";

        public static async Task<string> CompleteReadAsync(TaskCompletionSource<string> readCompletionSource, CancellationToken cancellationToken)
        {
            string value = null;

            try
            {
                Task completedTask = await Task.WhenAny(readCompletionSource.Task, Task.Delay(Timeout.Infinite, cancellationToken));

                if (completedTask == readCompletionSource.Task)
                {
                    value = await readCompletionSource.Task;
                }
                else
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                SensusServiceHelper.Get().Logger.Log("BLE read was cancelled:  " + ex.Message, LoggingLevel.Normal, typeof(BluetoothDeviceProximityProbe));
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception during BLE read:  " + ex.Message, LoggingLevel.Normal, typeof(BluetoothDeviceProximityProbe));
            }

            return value;
        }

        private int _scanDurationMS;
        private int _readDurationMS;

        /// <summary>
        /// The length of time to scan for devices in proximity (in milliseconds). The longer the scan, the
        /// more likely it is that devices in the environment will be detected. However, a longer scan also
        /// means that a detected device may go out of range before the scan completes and device identifiers
        /// are read. Also note that, if the <see cref="Protocol"/> is configured to use [push notifications](xref:push_notifications), 
        /// then the combination of <see cref="ScanDurationMS"/> and <see cref="ReadDurationMS"/> should not
        /// exceed 20 seconds, as there is limited time to complete all background processing. It is difficult
        /// to recommend a "best" value, but 10000ms seems like a reasonable scan duration, all things considered.
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

        /// <summary>
        /// The length of time to read identifiers from scanned devices (in milliseconds). The longer the read, the
        /// more likely it is that all scanned devices will be read. However, note that, if the <see cref="Protocol"/> 
        /// is configured to use [push notifications](xref:push_notifications), then the combination of 
        /// <see cref="ScanDurationMS"/> and <see cref="ReadDurationMS"/> should not exceed 20 seconds, as there is 
        /// limited time to complete all background processing. It is difficult to recommend a "best" value, but 
        /// 10000ms seems like a reasonable read duration, all things considered.
        /// </summary>
        /// <value>The read time ms.</value>
        [EntryIntegerUiProperty("Read Duration (MS):", true, 5, true)]
        public int ReadDurationMS
        {
            get
            {
                return _readDurationMS;
            }
            set
            {
                if (value < 5000)
                {
                    value = 5000;
                }

                _readDurationMS = value;
            }
        }

        [JsonIgnore]
        public int ReadAttemptCount { get; set; }

        [JsonIgnore]
        public int ReadSuccessCount { get; set; }

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
            _scanDurationMS = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            _readDurationMS = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ReadAttemptCount = ReadSuccessCount = 0;

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
            List<Datum> dataToReturn = new List<Datum>();

            try
            {
                TimeSpan scanDuration = TimeSpan.FromMilliseconds(ScanDurationMS);
                CancellationTokenSource scanCanceller = new CancellationTokenSource();
                scanCanceller.CancelAfter(scanDuration);
                cancellationToken.Register(scanCanceller.Cancel);
                SensusServiceHelper.Get().Logger.Log("Scanning for " + scanDuration + "...", LoggingLevel.Normal, GetType());
                await ScanAsync(scanCanceller.Token);

                TimeSpan readDuration = TimeSpan.FromMilliseconds(ReadDurationMS);
                CancellationTokenSource readCanceller = new CancellationTokenSource();
                readCanceller.CancelAfter(readDuration);
                cancellationToken.Register(readCanceller.Cancel);
                SensusServiceHelper.Get().Logger.Log("Waiting " + readDuration + " for device identifiers to be read...", LoggingLevel.Normal, GetType());

                try
                {
                    foreach (Tuple<string, DateTimeOffset> deviceIdTimestamp in await ReadPeripheralCharacteristicValuesAsync(cancellationToken))
                    {
                        dataToReturn.Add(new BluetoothDeviceProximityDatum(deviceIdTimestamp.Item2, deviceIdTimestamp.Item1));
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while reading device identifiers:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while polling:  " + ex, LoggingLevel.Normal, GetType());
            }

            // let the system know that we polled but didn't get any data
            if (dataToReturn.Count == 0)
            {
                dataToReturn.Add(null);
            }

            return dataToReturn;
        }

        protected abstract Task ScanAsync(CancellationToken cancellationToken);

        protected abstract Task<List<Tuple<string, DateTimeOffset>>> ReadPeripheralCharacteristicValuesAsync(CancellationToken cancellationToken);

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Read Success", ReadSuccessCount.RoundToWholePercentageOf(ReadAttemptCount, 5).ToString()}
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return result;
        }

        public sealed override async Task StopAsync()
        {
            await base.StopAsync();

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
