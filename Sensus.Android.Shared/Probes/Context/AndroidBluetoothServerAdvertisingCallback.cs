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
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Sensus.Context;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Android BLE server advertising callback. Receives events related to the starting and 
    /// stopping of BLE advertising. Configures a BLE server that responds to client read requests.
    /// </summary>
    public class AndroidBluetoothServerAdvertisingCallback : AdvertiseCallback
    {
        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;
        private BluetoothGattServer _gattServer;

        public AndroidBluetoothServerAdvertisingCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
        }

        /// <summary>
        /// Called when advertising has successfully started. Sets up a BLE server to handle client requests.
        /// </summary>
        /// <param name="settingsInEffect">Settings in effect.</param>
        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            SensusServiceHelper.Get().Logger.Log("Started advertising.", LoggingLevel.Normal, GetType());

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // the following should never happen, but just in case there is an existing server, close it.
                if (_gattServer != null)
                {
                    CloseServer();
                }

                try
                {
                    // create a callback to receive read requests events from client
                    AndroidBluetoothServerGattCallback serverCallback = new AndroidBluetoothServerGattCallback(_service, _characteristic);

                    // open the server, which contains the substantive connection with a client. this also registers the callback.
                    BluetoothManager bluetoothManager = Application.Context.GetSystemService(global::Android.Content.Context.BluetoothService) as BluetoothManager;
                    _gattServer = bluetoothManager.OpenGattServer(Application.Context, serverCallback);

                    // set the server on callback, so that responses to read requests can be sent back to clients.
                    serverCallback.Server = _gattServer;

                    // declare the BLE service handled by the server, which is the same as the one advertised.
                    _gattServer.AddService(_service);
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
                    _gattServer?.RemoveService(_service);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while removing service:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                // close the server
                try
                {
                    _gattServer?.Close();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while closing GATT server:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
                finally
                {
                    _gattServer = null;
                }
            });
        }
    }
}
