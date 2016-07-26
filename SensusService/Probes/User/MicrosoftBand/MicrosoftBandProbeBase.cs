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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Band.Portable;
using Newtonsoft.Json;
using SensusService.Probes;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Band.Portable.Sensors;
using SensusUI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace SensusService
{
    public abstract class MicrosoftBandProbeBase : ListeningProbe
    {
        #region static members

        private static BandClient BAND_CLIENT;
        private static bool BAND_CLIENT_CONNECTING = false;
        private const int BAND_CLIENT_CONNECT_TIMEOUT_MS = 5000;
        private const int BAND_CLIENT_CONNECT_ATTEMPTS = 5;
        private static ManualResetEvent BAND_CLIENT_CONNECT_WAIT = new ManualResetEvent(false);
        private static object BAND_CLIENT_LOCKER = new object();
        private static List<MicrosoftBandProbeBase> CONFIGURE_PROBES_IF_CONNECTED = new List<MicrosoftBandProbeBase>();
        private static int CLIENT_CONNECT_ATTEMPTS = 0;
        private static int CLIENT_CONNECT_SUCCESSES = 0;
        private static int CLIENT_CONNECT_TIMEOUTS = 0;
        private static int CLIENT_DISCONNECT_ATTEMPTS = 0;
        private static int CLIENT_DISCONNECT_SUCCESSES = 0;

        private static string HEALTH_TEST_CALLBACK_ID;
        private const int HEALTH_TEST_DELAY_MS = 60000;
        private static readonly object HEALTH_TEST_LOCKER = new object();

        private static List<MicrosoftBandProbeBase> BandProbesThatShouldBeRunning
        {
            get
            {
                return SensusServiceHelper.Get().GetRunningProtocols().SelectMany(protocol => protocol.Probes.Where(probe => probe.Enabled && probe is MicrosoftBandProbeBase)).Cast<MicrosoftBandProbeBase>().ToList();
            }
        }

        private static List<MicrosoftBandProbeBase> BandProbesThatAreRunning
        {
            get
            {
                return SensusServiceHelper.Get().RegisteredProtocols.SelectMany(protocol => protocol.Probes.Where(probe => probe.Running && probe is MicrosoftBandProbeBase)).Cast<MicrosoftBandProbeBase>().ToList();
            }
        }

        [JsonIgnore]
        private static BandClient BandClient
        {
            get
            {
                // the client can become disposed (at least on android) when the user turns bluetooth off/on. the client won't be null,
                // so we risk an object disposed when referencing the client. test this out below and set the client to null if it's disposed.
                try
                {
                    if (BAND_CLIENT?.IsConnected ?? false)
                        SensusServiceHelper.Get().Logger.Log("Client is connected.", LoggingLevel.Debug, typeof(MicrosoftBandProbeBase));
                }
                catch (ObjectDisposedException)
                {
                    SensusServiceHelper.Get().Logger.Log("Client is disposed.", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                    BAND_CLIENT = null;
                }

                return BAND_CLIENT;
            }
            set
            {
                BAND_CLIENT = value;
            }
        }

        protected static void ConnectClient(MicrosoftBandProbeBase configureProbeIfConnected = null)
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to collect data from your Microsoft Band, which is being used in one of your studies."))
                return;

            if (configureProbeIfConnected != null)
            {
                lock (CONFIGURE_PROBES_IF_CONNECTED)
                {
                    CONFIGURE_PROBES_IF_CONNECTED.Add(configureProbeIfConnected);
                }
            }

            lock (BAND_CLIENT_LOCKER)
            {
                if (!BAND_CLIENT_CONNECTING)
                {
                    BAND_CLIENT_CONNECTING = true;
                    BAND_CLIENT_CONNECT_WAIT.Reset();

                    Task.Run(async () =>
                    {
                        try
                        {
                            // if we already have a connection, configure any waiting probes
                            if (BandClient?.IsConnected ?? false)
                            {
                                lock (CONFIGURE_PROBES_IF_CONNECTED)
                                {
                                    foreach (MicrosoftBandProbeBase probe in CONFIGURE_PROBES_IF_CONNECTED)
                                    {
                                        try
                                        {
                                            probe.Configure(BandClient);
                                        }
                                        catch (Exception ex)
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Failed to configure probe on existing connection:  " + ex.Message, LoggingLevel.Normal, probe.GetType());
                                        }
                                    }

                                    CONFIGURE_PROBES_IF_CONNECTED.Clear();
                                }
                            }
                            // otherwise, attempt to connect
                            else
                            {
                                int connectAttemptsLeft = BAND_CLIENT_CONNECT_ATTEMPTS;

                                while (connectAttemptsLeft-- > 0 && (BandClient == null || !BandClient.IsConnected))
                                {
                                    BandClientManager bandManager = BandClientManager.Instance;
                                    BandDeviceInfo band = (await bandManager.GetPairedBandsAsync()).FirstOrDefault();
                                    if (band == null)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("No Bands connected. Retrying...", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                        Thread.Sleep(BAND_CLIENT_CONNECT_TIMEOUT_MS);
                                    }
                                    else
                                    {
                                        ++CLIENT_CONNECT_ATTEMPTS;
                                        Task<BandClient> connectTask = bandManager.ConnectAsync(band);

                                        if (await Task.WhenAny(connectTask, Task.Delay(BAND_CLIENT_CONNECT_TIMEOUT_MS)) == connectTask)
                                        {
                                            BandClient = await connectTask;
                                            ++CLIENT_CONNECT_SUCCESSES;
                                        }
                                        else
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Timed out while connecting. Retrying...", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                            ++CLIENT_CONNECT_TIMEOUTS;
                                        }
                                    }
                                }

                                // if we connected successfully, use the new client to (re)configure all probes that should be running.
                                if (BandClient?.IsConnected ?? false)
                                {
                                    lock (CONFIGURE_PROBES_IF_CONNECTED)
                                    {
                                        foreach (MicrosoftBandProbeBase probe in BandProbesThatShouldBeRunning)
                                        {
                                            try
                                            {
                                                probe.Configure(BandClient);
                                            }
                                            catch (Exception ex)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Failed to start readings for Band probe:  " + ex.Message, LoggingLevel.Normal, probe.GetType());
                                            }
                                        }

                                        CONFIGURE_PROBES_IF_CONNECTED.Clear();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to connect to Microsoft Band:  " + ex.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                        }
                        finally
                        {
                            BAND_CLIENT_CONNECT_WAIT.Set();
                            BAND_CLIENT_CONNECTING = false;
                        }
                    });
                }
            }

            BAND_CLIENT_CONNECT_WAIT.WaitOne();

            if (BandClient == null || !BandClient.IsConnected)
                throw new Exception("Failed to connect to Microsoft Band.");
        }

        public static Task TestBandClientAsync(string callbackId, CancellationToken cancellationToken, Action letDeviceSleepCallback)
        {
            return Task.Run(() =>
            {
                // if no band probes should be running, then ignore the current test and unschedule the test callback.
                if (BandProbesThatShouldBeRunning.Count == 0)
                {
                    CancelHealthTest();
                    return;
                }

                // ensure that client is connected
                try
                {
                    ConnectClient();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to connect Band client:  " + ex.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                }

                // it's possible that the device was re-paired, resulting in the client being connected but the
                // readings being disrupted. ensure that readings are coming by starting them every time we test
                // the probe. if the readings are already coming this will have no effect. if they were disrupted
                // the readings will be restarted.
                foreach (MicrosoftBandProbeBase probe in BandProbesThatShouldBeRunning)
                {
                    try
                    {
                        probe.StartReadings();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to start readings for Band probe:  " + ex.Message, LoggingLevel.Normal, probe.GetType());
                    }
                }
            });
        }

        private static void CancelHealthTest()
        {
            lock (HEALTH_TEST_LOCKER)
            {
                if (HEALTH_TEST_CALLBACK_ID != null)
                {
                    SensusServiceHelper.Get().Logger.Log("Canceling health test.", LoggingLevel.Verbose, typeof(MicrosoftBandProbeBase));
                    SensusServiceHelper.Get().UnscheduleCallback(HEALTH_TEST_CALLBACK_ID);
                    HEALTH_TEST_CALLBACK_ID = null;
                }
            }
        }

        #endregion

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
                return "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return null;
            }
        }

        protected MicrosoftBandProbeBase()
        {
            _samplingRate = BandSensorSampleRate.Ms16;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // we expect this probe to start successfully, but an exception may occur if no bands are paired with the device of if the
            // connection with a paired band fails. so schedule a static repeating callback to check on all band probes and restart them 
            // if needed/possible. this is better than a non-static callback for each band probe because there are many band probes and 
            // the callbacks would be redundant, frequent, and power-hungry.
            lock (HEALTH_TEST_LOCKER)
            {
                // only schedule the callback if we haven't done so already. the callback should be global across all band probes.
                if (HEALTH_TEST_CALLBACK_ID == null)
                {
                    ScheduledCallback callback = new ScheduledCallback(TestBandClientAsync, "Microsoft Band Health Test", TimeSpan.FromMinutes(5));
                    HEALTH_TEST_CALLBACK_ID = SensusServiceHelper.Get().ScheduleRepeatingCallback(callback, HEALTH_TEST_DELAY_MS, HEALTH_TEST_DELAY_MS, false);
                }
            }

            ConnectClient(this);
        }

        protected abstract void Configure(BandClient bandClient);

        protected override void StartListening()
        {
            StartReadings();
        }

        protected abstract void StartReadings();

        protected override void StopListening()
        {
            StopReadings();

            // only cancel the static health test if none of the band probes should be running.
            if (BandProbesThatShouldBeRunning.Count == 0)
                CancelHealthTest();

            // disconnect the client if no band probes are actually running.
            if (BandProbesThatAreRunning.Count == 0 && (BandClient?.IsConnected ?? false))
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("All Band probes have stopped. Disconnecting client.", LoggingLevel.Normal, GetType());
                    ++CLIENT_DISCONNECT_ATTEMPTS;
                    BandClient.DisconnectAsync().Wait();
                    BandClient = null;
                    ++CLIENT_DISCONNECT_SUCCESSES;
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect client:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        protected abstract void StopReadings();

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            if (CLIENT_CONNECT_ATTEMPTS != CLIENT_CONNECT_SUCCESSES ||
                CLIENT_DISCONNECT_ATTEMPTS != CLIENT_DISCONNECT_SUCCESSES)
            {
                string name = GetType().Name;

                misc += "Client connect attempts [" + name + "]:  " + CLIENT_CONNECT_ATTEMPTS + Environment.NewLine +
                        "Client connnect successes [" + name + "]:  " + CLIENT_CONNECT_SUCCESSES + Environment.NewLine +
                        "Client connect timeouts [" + name + "]:  " + CLIENT_CONNECT_TIMEOUTS + Environment.NewLine +
                        "Client disconnect attempts [" + name + "]:  " + CLIENT_DISCONNECT_ATTEMPTS + Environment.NewLine +
                        "Client disconnect successes [" + name + "]:  " + CLIENT_DISCONNECT_SUCCESSES + Environment.NewLine;
            }

            return false;
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