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
        private ManualResetEvent _authenticateWait;
        private ManualResetEvent _bluetoothWait;
        private global::Android.App.Result _bluetoothResult;
        private ManualResetEvent _connectedWait;
        private ManualResetEvent _disconnectWait;

        public AndroidEmpaticaWristbandListener(AndroidEmpaticaWristbandProbe probe)
        {
            _probe = probe;
            _authenticateWait = new ManualResetEvent(false);
            _bluetoothWait = new ManualResetEvent(false);
            _connectedWait = new ManualResetEvent(false);
            _disconnectWait = new ManualResetEvent(false);
        }

        public void Initialize()
        {
            if (_empaticaDeviceManager == null)
            {
                ManualResetEvent initializeWait = new ManualResetEvent(false);

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        _empaticaDeviceManager = new EmpaDeviceManager((SensusServiceHelper.Get() as AndroidSensusServiceHelper).Service, this, this);
                        initializeWait.Set();
                    });

                initializeWait.WaitOne();
            }
            else
                StopScanning();
        }

        public void Authenticate(string empaticaApiKey)
        {
            // assume bluetooth will be present. if it's not, AuthenticateWithAPIKey will call DidRequestEnableBluetooth to start it.
            _bluetoothWait.Set();
            _bluetoothResult = global::Android.App.Result.Ok;

            _authenticateWait.Reset();
            _empaticaDeviceManager.AuthenticateWithAPIKey(empaticaApiKey); // TODO:  Handle uncaught exception from asynctask
            _authenticateWait.WaitOne();
        }

        public void DidUpdateStatus(EmpaStatus status)
        {
            if (status == EmpaStatus.Ready)
                _authenticateWait.Set();
            else if (status == EmpaStatus.Connected)
                _connectedWait.Set();
            else if (status == EmpaStatus.Disconnected)
                _disconnectWait.Set();
        }

        public void StartBluetooth()
        {
            _bluetoothWait.WaitOne();

            if (_bluetoothResult != global::Android.App.Result.Ok)
                throw new Exception("Bluetooth not enabled.");
        }

        public void DidRequestEnableBluetooth()
        {             
            _bluetoothWait.Reset();

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    mainActivity.GetActivityResultAsync(new Intent(BluetoothAdapter.ActionRequestEnable), AndroidActivityResultRequestCode.StartBluetooth, resultIntent =>
                        {
                            _bluetoothResult = resultIntent.Item1;
                            _bluetoothWait.Set();
                        });
                });
        }

        public void DiscoverAndConnectDeviceAsync()
        {
            StopScanning();
            StartScanning();
        }

        public void DidDiscoverDevice(BluetoothDevice device, string deviceName, int rssi, bool allowed)
        {
            SensusServiceHelper.Get().Logger.Log("Discovered device \"" + device.Name + "\" (allowed:  " + allowed + ").", LoggingLevel.Normal, GetType());

            if (allowed)
            {
                StopScanning();
                DisconnectDevice();
                ConnectDevice(device);
            }
        }

        public void DidUpdateSensorStatus(EmpaSensorStatus sensorStatus, EmpaSensorType sensorType)
        {
        }

        #region data receipt

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

        #endregion

        private void StartScanning()
        {
            try
            {    
                _empaticaDeviceManager.StartScanning();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to start Empatica device discovery:  " + ex.Message);
            }
        }

        private void StopScanning()
        {
            try
            {
                _empaticaDeviceManager.StopScanning();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to stop scanning device manager:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        private void ConnectDevice(BluetoothDevice device)
        {
            try
            {
                _connectedWait.Reset();
                _empaticaDeviceManager.ConnectDevice(device);
                _connectedWait.WaitOne();
                SensusServiceHelper.Get().Logger.Log("Connected with Empatica device \"" + device.Name + "\".", LoggingLevel.Normal, GetType());
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to connect with Empatica device \"" + device.Name + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public void DisconnectDevice()
        {
            try
            {
                _disconnectWait.Reset();
                _empaticaDeviceManager.Disconnect();
                _disconnectWait.WaitOne();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to disconnect device manager:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        private void CleanUp()
        {
            // TODO:  When should this be called?

            try
            {
                _empaticaDeviceManager.CleanUp();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to cleanup device manager:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }
    }
}