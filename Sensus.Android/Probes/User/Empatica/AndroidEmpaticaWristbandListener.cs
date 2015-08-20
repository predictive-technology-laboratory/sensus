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
using Com.Empatica.Empalink.Delegates;
using Com.Empatica.Empalink;
using Sensus.Android;
using Android.Content;
using Android.Bluetooth;
using SensusService;
using Com.Empatica.Empalink.Config;
using SensusService.Probes.User.Empatica;
using System.Threading;

namespace Sensus.Android.Probes.User.Empatica
{
    public class AndroidEmpaticaWristbandListener : Java.Lang.Object, IEmpaStatusDelegate, IEmpaDataDelegate
    {
        private AndroidEmpaticaWristbandProbe _probe;
        private EmpaDeviceManager _empaticaDeviceManager;
        private Action<Exception> _authenticateAction;

        public AndroidEmpaticaWristbandListener(AndroidEmpaticaWristbandProbe probe)
        {
            _probe = probe;
        }

        public void Initialize()
        {
            if (_empaticaDeviceManager != null)
            {
                DisconnectDevice();
                _empaticaDeviceManager.CleanUp();
            }

            ManualResetEvent initializeWait = new ManualResetEvent(false);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    _empaticaDeviceManager = new EmpaDeviceManager((SensusServiceHelper.Get() as AndroidSensusServiceHelper).Service, this, this);
                    initializeWait.Set();
                });

            initializeWait.WaitOne();
        }

        public void AuthenticateAsync(string empaticaApiKey, Action<Exception> authenticateAction)
        {
            _authenticateAction = authenticateAction;
            _empaticaDeviceManager.AuthenticateWithAPIKey(empaticaApiKey); // TODO:  Handle uncaught exception from asynctask
        }

        public void DidUpdateStatus(EmpaStatus status)
        {
            if (status == EmpaStatus.Ready)
                _authenticateAction(null);
        }

        public void DiscoverAndConnectDeviceAsync()
        {
            try
            {
                _empaticaDeviceManager.StopScanning();
            }
            catch(Exception)
            {
            }

            try
            {    
                if(SensusServiceHelper.Get().BluetoothEnabled)
                    _empaticaDeviceManager.StartScanning();
                else
                    throw new Exception("Bluetooth is not enabled.");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to start Empatica device discovery:  " + ex.Message);
            }
        }

        public void DidRequestEnableBluetooth()
        {                        
        }

        public void DidDiscoverDevice(BluetoothDevice device, string deviceName, int rssi, bool allowed)
        {
            SensusServiceHelper.Get().Logger.Log("Discovered device \"" + device.Name + "\" (allowed:  " + allowed + ").", LoggingLevel.Normal, GetType());

            if (allowed)
            {
                _empaticaDeviceManager.StopScanning();

                try
                {
                    _empaticaDeviceManager.ConnectDevice(device);
                    SensusServiceHelper.Get().Logger.Log("Connected with Empatica device \"" + device.Name + "\".", LoggingLevel.Normal, GetType());
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to connect with Empatica device \"" + device.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        public void DidUpdateSensorStatus(EmpaSensorStatus sensorStatus, EmpaSensorType sensorType)
        {
        }

        public void DidReceiveAcceleration(int x, int y, int z, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { AccelerationX = x, AccelerationY = y, AccelerationZ = z });
        }

        public void DidReceiveBloodVolumePulse(float bloodVolumePulse, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { BloodVolumePulse = bloodVolumePulse });
        }

        public void DidReceiveBatteryLevel(float level, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { BatteryLevel = level });
        }

        public void DidReceiveGalvanicSkinResponse(float galvanicSkinResponse, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { GalvanicSkinResponse = galvanicSkinResponse });
        }

        public void DidReceiveInterBeatInterval(float interBeatInterval, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { InterBeatInterval = interBeatInterval });
        }

        public void DidReceiveTemperature(float temperature, double timestamp)
        {
            _probe.StoreDatum(new EmpaticaWristbandDatum(timestamp) { Temperature = temperature });
        }

        public void DisconnectDevice()
        {
            try
            {
                _empaticaDeviceManager.Disconnect();
            }
            catch (Exception ex)
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnect Empatica device \"" + _empaticaDeviceManager.ActiveDevice.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                catch (Exception)
                {
                }
            }
        }
    }
}