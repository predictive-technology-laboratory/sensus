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

using Newtonsoft.Json;
using Microsoft.Band.Portable;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Band.Portable.Sensors;
using Syncfusion.SfChart.XForms;
using Sensus.UI.UiProperties;
using Sensus.Context;
using Sensus.Callbacks;

namespace Sensus.Probes.User.MicrosoftBand
{
    public abstract class MicrosoftBandProbeBase : ListeningProbe
    {
        #region static members

        private static BandClient BAND_CLIENT;
        private static bool BAND_CLIENT_CONNECTING = false;
        private const int BAND_CLIENT_CONNECT_TIMEOUT_MS = 10000;
        private const int BAND_CLIENT_CONNECT_ATTEMPTS = 3;
        private static ManualResetEvent BAND_CLIENT_CONNECT_WAIT = new ManualResetEvent(false);
        private static object BAND_CLIENT_LOCKER = new object();
        private static ScheduledCallback HEALTH_TEST_CALLBACK;
        private readonly TimeSpan HEALTH_TEST_DELAY = TimeSpan.FromSeconds(60);
        private readonly TimeSpan HEALTH_TEST_TIMEOUT = TimeSpan.FromSeconds(60);
        private static readonly object HEALTH_TEST_LOCKER = new object();
        private static bool REENABLE_BLUETOOTH_IF_NEEDED = true;

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
                    {
                        SensusServiceHelper.Get().Logger.Log("Client is connected.", LoggingLevel.Debug, typeof(MicrosoftBandProbeBase));
                    }
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

        protected static void ConnectClient(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!SensusServiceHelper.Get().EnableBluetooth(true, "Sensus uses Bluetooth to collect data from your Microsoft Band, which is being used in one of your studies."))
            {
                throw new MicrosoftBandClientConnectException("Bluetooth not enabled.");
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
                            if (BandClient?.IsConnected ?? false)
                            {
                                return;
                            }

                            int connectAttempt = 0;

                            while (++connectAttempt <= BAND_CLIENT_CONNECT_ATTEMPTS && (BandClient == null || !BandClient.IsConnected) && !cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    SensusServiceHelper.Get().Logger.Log("Connect attempt " + connectAttempt + " of " + BAND_CLIENT_CONNECT_ATTEMPTS + ".", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));

                                    BandClientManager bandManager = BandClientManager.Instance;
                                    BandDeviceInfo band = (await bandManager.GetPairedBandsAsync()).FirstOrDefault();
                                    if (band == null)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("No paired Bands.", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                        Thread.Sleep(BAND_CLIENT_CONNECT_TIMEOUT_MS);
                                    }
                                    else
                                    {
                                        Task<BandClient> connectTask = bandManager.ConnectAsync(band);

                                        if (await Task.WhenAny(connectTask, Task.Delay(BAND_CLIENT_CONNECT_TIMEOUT_MS)) == connectTask)
                                        {
                                            BandClient = await connectTask;

                                            if (BandClient.IsConnected)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Connected.", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                            }
                                            else
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Could not connect.", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                            }
                                        }
                                        else
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Timed out.", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Exception while connecting to Band:  " + ex.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to reuse/establish connected Band client:  " + ex.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
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
            {
                throw new MicrosoftBandClientConnectException("Failed to connect to Band.");
            }
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

                try
                {
                    // ensure that client is connected
                    ConnectClient(cancellationToken);

                    // we've successfully connected. if we fail at some point in the future, allow the system to reenable bluetooth.
                    REENABLE_BLUETOOTH_IF_NEEDED = true;
                }
                catch (Exception connectException)
                {
                    SensusServiceHelper.Get().Logger.Log("Band client failed to connect:  " + connectException.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));

                    // we failed to connect. try reenabling bluetooth if we haven't already tried.
                    if (!cancellationToken.IsCancellationRequested && REENABLE_BLUETOOTH_IF_NEEDED)
                    {
                        SensusServiceHelper.Get().Logger.Log("Reenabling Bluetooth...", LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));

                        try
                        {
                            SensusServiceHelper.Get().DisableBluetooth(true, true, "Sensus uses Bluetooth to collect data from your Microsoft Band, which is being used in one of your studies.");
                        }
                        catch (Exception reenableBluetoothException)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to reenable Bluetooth:  " + reenableBluetoothException.Message, LoggingLevel.Normal, typeof(MicrosoftBandProbeBase));
                        }
                        finally
                        {
                            REENABLE_BLUETOOTH_IF_NEEDED = false;
                        }
                    }
                }

                // it's possible that the device was re-paired, resulting in the client being connected but the
                // readings being disrupted. ensure that readings are coming by starting them every time we test
                // the probe. if the readings are already coming this will have no effect. if they were disrupted
                // the readings will be restarted.
                foreach (MicrosoftBandProbeBase probe in BandProbesThatShouldBeRunning)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        // ensure that the probe is configured on the current client.
                        probe.Configure(BandClient);

                        // if the probe is already running, start readings to ensure that we're using the potentially newly configured
                        // sensor on the band client. this could be a probe that was previous started successfully, or the first Band
                        // probe that faulted when trying to start the protocol.
                        if (probe.Running)
                        {
                            // only start readings if they haven't been stopped due to non-contact. acquire lock to prevent race
                            // condition with contact changed event, which might attempt to stop readings on loss of contact.
                            lock (HEALTH_TEST_LOCKER)
                            {
                                if (!probe._stoppedBecauseNotWorn)
                                {
                                    probe.StartReadings();
                                }
                            }
                        }
                        // if we attempted to start several band probes upon protocol start up and failed, we would have bailed out after
                        // the first band probe failed to start. this would leave the other band probes enabled but not running at the time
                        // of this health test. start such probes now.
                        else
                        {
                            probe.Start();
                        }
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
                if (HEALTH_TEST_CALLBACK != null)
                {
                    SensusServiceHelper.Get().Logger.Log("Canceling health test.", LoggingLevel.Verbose, typeof(MicrosoftBandProbeBase));
                    SensusContext.Current.CallbackScheduler.UnscheduleCallback(HEALTH_TEST_CALLBACK.Id);
                    HEALTH_TEST_CALLBACK = null;
                }
            }
        }

        #endregion

        private BandSensorSampleRate _samplingRate;
        private bool _stopWhenNotWorn;
        private bool _stoppedBecauseNotWorn;

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

        [OnOffUiProperty("Stop When Not Worn (Must Enable Contact Probe):", true, 6)]
        public bool StopWhenNotWorn
        {
            get
            {
                return _stopWhenNotWorn;
            }
            set
            {
                _stopWhenNotWorn = value;
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

            // non-contact band probes should stop when the band is not being worn. if
            // the user sets _stopWhenNotWorn to true on the contact probe, nothing will 
            // happen (i.e., the contact probe will continue running) because we don't
            // hook up the contact event below.
            _stopWhenNotWorn = !(this is MicrosoftBandContactProbe);
            _stoppedBecauseNotWorn = false;
        }

        protected abstract void Configure(BandClient bandClient);

        protected override void Initialize()
        {
            base.Initialize();

            _stoppedBecauseNotWorn = false;
        }

        protected override void StartListening()
        {
            // we expect this probe to start successfully, but an exception may occur if no bands are paired with the device of if the
            // connection with a paired band fails. so schedule a static repeating callback to check on all band probes and restart them 
            // if needed/possible. this is better than a non-static callback for each band probe because there are many band probes and 
            // the callbacks would be redundant, frequent, and power-hungry.
            lock (HEALTH_TEST_LOCKER)
            {
                // only schedule the callback if we haven't done so already. the callback should be global across all band probes.
                if (HEALTH_TEST_CALLBACK == null)
                {
                    // the band health test is static, so it has no domain other than sensus.
                    HEALTH_TEST_CALLBACK = new ScheduledCallback(TestBandClientAsync, "BAND-HEALTH-TEST", null, null, HEALTH_TEST_TIMEOUT);
                    SensusContext.Current.CallbackScheduler.ScheduleRepeatingCallback(HEALTH_TEST_CALLBACK, HEALTH_TEST_DELAY, HEALTH_TEST_DELAY, false);
                }
            }

            // hook up the contact event for non-contact probes. need to do this before the calls below because they might throw
            // a band connect exception. such an exception will leave the probe in a running state in anticipation that the user
            // might pair a band later. the band health test will start readings later for all band probes, but it will not use
            // Start to do so (it will simply start the readings). so this is our only chance to hook up the contact event.
            if (!(this is MicrosoftBandContactProbe))
            {
                MicrosoftBandContactProbe contactProbe = Protocol.Probes.Single(probe => probe is MicrosoftBandContactProbe) as MicrosoftBandContactProbe;
                contactProbe.ContactStateChanged += ContactStateChanged;
            }

            ConnectClient();
            Configure(BandClient);
            StartReadings();
        }

        protected abstract void StartReadings();

        protected override void StopListening()
        {
            // disconnect the contact event for non-contact probes. this probe has already been marked as non-running, so
            // there's no risk of a race condition in which the contact state changes to worn and the change event attempts
            // to restart this probe after it has been stopped (we check Running during the change event).
            if (!(this is MicrosoftBandContactProbe))
            {
                MicrosoftBandContactProbe contactProbe = Protocol.Probes.Single(probe => probe is MicrosoftBandContactProbe) as MicrosoftBandContactProbe;
                contactProbe.ContactStateChanged -= ContactStateChanged;
            }

            StopReadings();

            // only cancel the static health test if none of the band probes should be running.
            if (BandProbesThatShouldBeRunning.Count == 0)
            {
                CancelHealthTest();
            }

            // disconnect the client if no band probes are actually running.
            if (BandProbesThatAreRunning.Count == 0 && (BandClient?.IsConnected ?? false))
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("All Band probes have stopped. Disconnecting client.", LoggingLevel.Normal, GetType());
                    BandClient.DisconnectAsync().Wait();
                    BandClient = null;
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect client:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        protected abstract void StopReadings();

        private void ContactStateChanged(object sender, ContactState contactState)
        {
            // contact probe might get a reading before this probe changes to the running state or after it changes
            // to the stopped state.
            if (Running)
            {
                // start readings if band is worn, regardless of whether we're stopping readings when it isn't worn.
                if (contactState == ContactState.Worn)
                {
                    StartReadings();
                    _stoppedBecauseNotWorn = false;
                }
                else if (contactState == ContactState.NotWorn && _stopWhenNotWorn && !_stoppedBecauseNotWorn)
                {
                    // prevent race condition with client health test, which will be trying to start readings on this probe.
                    lock (HEALTH_TEST_LOCKER)
                    {
                        StopReadings();
                        _stoppedBecauseNotWorn = true;
                    }
                }
            }
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
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