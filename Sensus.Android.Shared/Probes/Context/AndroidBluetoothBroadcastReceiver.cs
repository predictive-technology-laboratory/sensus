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
    [BroadcastReceiver(Exported = false)]
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