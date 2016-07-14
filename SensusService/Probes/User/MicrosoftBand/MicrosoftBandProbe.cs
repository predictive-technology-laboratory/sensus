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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using Newtonsoft.Json;
using SensusUI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace SensusService.Probes.User.MicrosoftBand
{
    public abstract class MicrosoftBandProbe<SensorType, ReadingType> : MicrosoftBandProbeBase
        where SensorType : BandSensorBase<ReadingType>
        where ReadingType : IBandSensorReading
    {
        private const int BAND_CONNECT_TIMEOUT_MS = 20000;

        private BandSensorSampleRate _samplingRate;

        [ListUiProperty("Sampling Rate:", true, 5, new object[] { BandSensorSampleRate.Ms16, BandSensorSampleRate.Ms32, BandSensorSampleRate.Ms128 })]
        public BandSensorSampleRate SamplingRate
        {
            get
            {
                return _samplingRate;
            }
            set
            {
                _samplingRate = value;
            }
        }

        [JsonIgnore]
        protected abstract SensorType Sensor { get; }

        protected MicrosoftBandProbe()
        {
            _samplingRate = BandSensorSampleRate.Ms16;
        }

        protected override void Initialize()
        {
            base.Initialize();

            ManualResetEvent connectWait = new ManualResetEvent(false);
            Exception connectException = null;

            Task.Run(async () =>
            {
                try
                {
                    if (BandClient == null || !BandClient.IsConnected)
                    {
                        BandClientManager bandManager = BandClientManager.Instance;
                        BandDeviceInfo band = (await bandManager.GetPairedBandsAsync()).FirstOrDefault();

                        if (band == null)
                            throw new Exception("No Microsoft Bands are paired with this device. Please pair one.");
                        else
                        {
                            BandClient = await bandManager.ConnectAsync(band);

                            if (BandClient.IsConnected)
                            {
                                if (Sensor == null)
                                    throw new Exception("No sensor present.");
                                else
                                {
                                    Sensor.ReadingChanged += (o, args) =>
                                    {
                                        StoreDatum(GetDatumFromReading(args.SensorReading));
                                    };
                                }
                            }
                            else
                                throw new Exception("Disconnected.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to connect to Microsoft Band:  " + ex.Message, LoggingLevel.Normal, GetType());
                    connectException = ex;

                    DisconnectBandClient();
                }
                finally
                {
                    connectWait.Set();
                }
            });

            if (!connectWait.WaitOne(BAND_CONNECT_TIMEOUT_MS))
            {
                connectException = new Exception("Timed out while trying to connect to Microsoft Band.");
                DisconnectBandClient();
            }

            if (connectException != null)
                throw connectException;
        }

        protected override void StartListening()
        {
            Sensor.StartReadingsAsync(_samplingRate).Wait();
        }

        protected abstract Datum GetDatumFromReading(ReadingType reading);

        protected override void StopListening()
        {
            base.StopListening();

            if (Sensor != null)
            {
                try
                {
                    Sensor.StopReadingsAsync().Wait();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to stop readings:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            DisconnectBandClient();
        }

        private void DisconnectBandClient()
        {
            if (BandClient != null)
            {
                try
                {
                    BandClient.DisconnectAsync().Wait();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect client:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    BandClient = null;
                }
            }
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new DateTimeAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Time"
                }
            };
        }
    }
}