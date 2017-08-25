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
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Sensus.Context;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothAdvertisingCallback : AdvertiseCallback
    {
        private AndroidBluetoothDeviceProximityProbe _probe;
        private BluetoothGattServer _bluetoothGattServer;

        public AndroidBluetoothAdvertisingCallback(AndroidBluetoothDeviceProximityProbe probe)
        {
            _probe = probe;    
        }

        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            SensusServiceHelper.Get().Logger.Log("Started advertising.", LoggingLevel.Normal, GetType());

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // the following should never happen, but just in case there is an existing server, close it.
                if(_bluetoothGattServer != null)
                {
                    CloseServer();
                }

                // open gatt server to service read requests from central clients
                try
                {
                    BluetoothManager bluetoothManager = Application.Context.GetSystemService(global::Android.Content.Context.BluetoothService) as BluetoothManager;
                    AndroidBluetoothGattServerCallback serverCallback = new AndroidBluetoothGattServerCallback(_probe);
                    _bluetoothGattServer = bluetoothManager.OpenGattServer(Application.Context, serverCallback);

                    // set server on callback for responding to requests
                    serverCallback.Server = _bluetoothGattServer;

                    // add service 
                    _bluetoothGattServer.AddService(_probe.DeviceIdService);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting server:  " + ex, LoggingLevel.Normal, GetType());
                    CloseServer();
                }
            });
        }

        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            SensusServiceHelper.Get().Logger.Log("Failed to start advertising:" + errorCode, LoggingLevel.Normal, GetType());
        }

        public void CloseServer()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // remove the service
                try
                {
                    _bluetoothGattServer?.RemoveService(_probe.DeviceIdService);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while removing service:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                // close the server
                try
                {
                    _bluetoothGattServer?.Close();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while closing GATT server:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _bluetoothGattServer = null;
                }
            });
        }
    }
}