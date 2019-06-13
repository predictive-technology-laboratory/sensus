using Android.Bluetooth;
using Android.Bluetooth.LE;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Android.Probes.Context
{
	public class AndroidBluetoothDevice
	{
		public BluetoothDevice Device { get; set; }
		public int Rssi { get; set; }
		public DateTimeOffset Timestamp { get; set; }
	}
}
