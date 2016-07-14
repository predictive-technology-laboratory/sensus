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
    public abstract class MicrosoftBandProbe<SensorType, ReadingType> : ListeningProbe
        where ReadingType : IBandSensorReading
        where SensorType : BandSensorBase<ReadingType>
    {
        private BandClient _bandClient;
        private BandSensorSampleRate _samplingRate;

        [JsonIgnore]
        protected BandClient BandClient
        {
            get
            {
                return _bandClient;
            }
        }

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

            Connect();
        }

        private void Connect()
        {
            ManualResetEvent connectWait = new ManualResetEvent(false);
            Exception connectException = null;

            Task.Run(async () =>
            {
                try
                {
                    if (_bandClient == null || !_bandClient.IsConnected)
                    {
                        BandClientManager bandManager = BandClientManager.Instance;
                        BandDeviceInfo band = (await bandManager.GetPairedBandsAsync()).FirstOrDefault();

                        if (band == null)
                            throw new Exception("No Microsoft Bands are paired with this device. Please pair one.");
                        else
                        {
                            _bandClient = await bandManager.ConnectAsync(band);

                            Sensor.ReadingChanged += (o, args) =>
                            {
                                StoreDatum(GetDatumFromReading(args.SensorReading));
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to connect to a Microsoft Band:  " + ex.Message, LoggingLevel.Normal, GetType());
                    connectException = ex;
                }
                finally
                {
                    connectWait.Set();
                }
            });

            connectWait.WaitOne();

            if (connectException != null)
                throw connectException;
        }

        protected override void StartListening()
        {
            Sensor.StartReadingsAsync(_samplingRate);
        }

        protected abstract Datum GetDatumFromReading(ReadingType reading);

        protected override void StopListening()
        {
            if (_bandClient != null)
            {
                try
                {
                    Sensor.StopReadingsAsync().Wait();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to stop readings:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                try
                {
                    _bandClient.DisconnectAsync().Wait();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect client:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _bandClient = null;
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

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (_bandClient == null || !_bandClient.IsConnected)
                restart = true;

            return restart;
        }
    }
}