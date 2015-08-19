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

namespace Sensus.Android.Probes.User.Empatica
{
    public class AndroidEmpaticaWristbandListener : Java.Lang.Object, IEmpaDataDelegate, IEmpaStatusDelegate
    {
        private EmpaDeviceManager _empaticaDeviceManager;
        private Action<Exception> _authenticateAction;

        public AndroidEmpaticaWristbandListener()
        {
            _empaticaDeviceManager = new EmpaDeviceManager((SensusServiceHelper.Get() as AndroidSensusServiceHelper).Service, this, this);
        }

        public void AuthenticateAsync(string empaticaApiKey, Action<Exception> authenticateAction)
        {
            _authenticateAction = authenticateAction;
            _empaticaDeviceManager.AuthenticateWithAPIKey(empaticaApiKey); // TODO:  How is invalid API key indicated?
        }

        public void DidUpdateStatus(EmpaStatus status)
        {
            if (status == EmpaStatus.Ready)
                _authenticateAction(null);
        }

        public void DidRequestEnableBluetooth()
        {            
            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    mainActivity.GetActivityResultAsync(new Intent(BluetoothAdapter.ActionRequestEnable), AndroidActivityResultRequestCode.StartBluetooth, resultIntent =>
                        {
                            if (resultIntent.Item1 == global::Android.App.Result.Canceled)
                            {
                                // TODO:  Do something
                            }
                        });
                });
        }

        public void DidDiscoverDevice(BluetoothDevice device, string deviceName, int rssi, bool allowed)
        {
            _empaticaDeviceManager.ConnectDevice(device);
        }

        public void DidUpdateSensorStatus(EmpaSensorStatus p0, EmpaSensorType p1)
        {
        }

        public void Stop()
        {
            if (_empaticaDeviceManager != null)
                _empaticaDeviceManager.Disconnect();
        }

        public void DidReceiveBatteryLevel(float p0, double p1)
        {
        }

        public void DidReceiveAcceleration(int p0, int p1, int p2, double p3)
        {
        }

        public void DidReceiveBVP(float p0, double p1)
        {
        }            

        public void DidReceiveGSR(float p0, double p1)
        {
        }

        public void DidReceiveIBI(float p0, double p1)
        {
        }

        public void DidReceiveTemperature(float p0, double p1)
        {
        }            
    }
}