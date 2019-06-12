using Android.Bluetooth;
using Android.Content;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sensus.Android.Probes.Context
{
	public class AndroidBluetoothDeviceReceiver : BroadcastReceiver
	{
		private AndroidBluetoothDeviceProximityProbe _probe;
		private List<AndroidBluetoothDevice> _devices;

		public AndroidBluetoothDeviceReceiver(AndroidBluetoothDeviceProximityProbe probe)
		{
			_probe = probe;
			_devices = new List<AndroidBluetoothDevice>();
		}

		public override void OnReceive(global::Android.Content.Context context, Intent intent)
		{
			try
			{
				if (intent == null)
				{
					throw new ArgumentNullException(nameof(intent));
				}

				if (intent.Action == BluetoothDevice.ActionFound)
				{
					AndroidBluetoothDevice device = new AndroidBluetoothDevice
					{
						Device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice,
						Rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue),
						Timestamp = DateTimeOffset.UtcNow
					};

					lock (_devices)
					{
						_devices.Add(device);
					}
				}
			}
			catch (Exception ex)
			{
				SensusException.Report("Exception in BLE broadcast receiver:  " + ex.Message, ex);
			}
		}

		public List<AndroidBluetoothDevice> GetDiscoveredDevices()
		{
			List<AndroidBluetoothDevice> devices = new List<AndroidBluetoothDevice>();

			lock (_devices)
			{
				devices = _devices.ToList();
			}

			return devices;
		}
	}
}
