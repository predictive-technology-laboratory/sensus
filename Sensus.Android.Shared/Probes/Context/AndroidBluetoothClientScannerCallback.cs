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
using System.Collections.Generic;
using Android.App;
using Android.Bluetooth.LE;
using Android.OS;
using Java.Lang;
using Sensus.Context;
using Android.Bluetooth;
using Sensus.Probes.Context;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// Android BLE client scanner callback. Receives events related to BLE scanning and  
    /// configures a BLE client that requests characteristic values from the server.
    /// </summary>
    public class AndroidBluetoothClientScannerCallback : ScanCallback
    {
        public event EventHandler<BluetoothCharacteristicReadArgs> CharacteristicRead;

        private BluetoothGattService _service;
        private BluetoothGattCharacteristic _characteristic;

        public AndroidBluetoothClientScannerCallback(BluetoothGattService service, BluetoothGattCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
        {
            ProcessScanResult(result);
        }

        public override void OnBatchScanResults(IList<ScanResult> results)
        {
            foreach (ScanResult result in results)
            {
                ProcessScanResult(result);
            }
        }

        private void ProcessScanResult(ScanResult result)
        {
            if (result == null)
            {
                return;
            }

            try
            {
                // get actual timestamp of encounter. this may be earlier than the current time because we do batch reporting.
                long msSinceEpoch = JavaSystem.CurrentTimeMillis() - SystemClock.ElapsedRealtime() + result.TimestampNanos / 1000000;
                DateTimeOffset encounterTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(msSinceEpoch);

                // register client callback
                AndroidBluetoothClientGattCallback clientCallback = new AndroidBluetoothClientGattCallback(_service, _characteristic, encounterTimestamp);

                // relay the client read event to any callers scanning for results
                if (CharacteristicRead != null)
                {
                    clientCallback.CharacteristicRead += CharacteristicRead;
                }

                result.Device.ConnectGatt(Application.Context, false, clientCallback);
            }
            catch (System.Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while connecting to peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}
