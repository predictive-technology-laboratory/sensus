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

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Sensus.Exceptions;
using System;

namespace Sensus.Android.Probes.Context
{
    /// <summary>
    /// A general-purpose broadcast receiver for monitoring BLE states.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { BluetoothDevice.ActionFound, BluetoothAdapter.ActionStateChanged }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidBluetoothBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<BluetoothDevice> DEVICE_FOUND;
        public static event EventHandler<State> STATE_CHANGED;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                if (intent.Action == BluetoothDevice.ActionFound && DEVICE_FOUND != null)
                {
                    BluetoothDevice device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                    DEVICE_FOUND(this, device);
                }
                else if (intent.Action == BluetoothAdapter.ActionStateChanged && STATE_CHANGED != null)
                {
                    int stateInt = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);

                    if (stateInt != -1)
                    {
                        STATE_CHANGED(this, (State)stateInt);
                    }
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in BLE broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}
