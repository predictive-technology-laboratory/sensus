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
using Sensus.Probes.Context;
using Android.Bluetooth.LE;
using Android.Bluetooth;
using Android.OS;
using Java.Util;
using System.Collections.Generic;
using System.Text;
using Sensus.Context;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Sensus.Probes;
using Android.App;
using Android.Content;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Context
{
	/// <summary>
	/// Scans for the presence of other devices nearby that are running the current <see cref="Protocol"/>. When
	/// encountered, this Probe will read the device ID of other devices. This Probe also advertises the presence 
	/// of the current device and serves requests for the current device's ID. This Probe reports data in the form 
	/// of <see cref="BluetoothDeviceProximityDatum"/> objects.
	/// 
	/// There are caveats to the conditions under which an Android device running this Probe will detect another
	/// device:
	/// 
	///   * If the other device is an Android device with Sensus running in the foreground or background, detection is possible.
	///   * If the other device is an iOS device with Sensus running in the foreground, detection is possible.
	///   * If the other device is an iOS device with Sensus running in the background, detection is not possible.
	/// 
	/// NOTE:  The value of <see cref="Protocol.Id"/> running on the other device must equal the value of 
	/// <see cref="Protocol.Id"/> running on the current device. When a Protocol is created from within the
	/// Sensus app, it is assigned a unique identifier. This value is maintained or changed depending on what you
	/// do:
	/// 
	///   * When the newly created Protocol is copied on the current device, a new unique identifier is assigned to
	///     it. This breaks the connection between the Protocols.
	/// 
	///   * When the newly created Protocol is shared via the app with another device, its identifier remains 
	///     unchanged. This maintains the connection between the Protocols.
	/// 
	/// Thus, in order for this <see cref="AndroidBluetoothDeviceProximityProbe"/> to operate properly, you must configure
	/// your Protocols in one of the two following ways:
	/// 
	///   * Create your Protocol on one platform (either Android or iOS) and then share it with a device from the other
	///     platform for customization. The <see cref="Protocol.Id"/> values of these Protocols will remain equal
	///     and this <see cref="AndroidBluetoothDeviceProximityProbe"/> will detect encounters across platforms.
	/// 
	///   * Create your Protocols separately on each platform and then set the <see cref="Protocol.Id"/> field on
	///     one platform (using the "Set Study Identifier" button) to match the <see cref="Protocol.Id"/> value
	///     of the other platform (obtained via "Copy Study Identifier").
	/// 
	/// See the iOS subclass of <see cref="BluetoothDeviceProximityProbe"/> for additional information.
	/// </summary>
	public class AndroidBluetoothDeviceProximityProbe : BluetoothDeviceProximityProbe
	{
		private AndroidBluetoothClientScannerCallback _bluetoothScannerCallback;
		private AndroidBluetoothServerAdvertisingCallback _bluetoothAdvertiserCallback;
		private BluetoothAdapter _bluetoothAdapter;
		private BluetoothGattService _deviceIdService;
		private BluetoothGattCharacteristic _deviceIdCharacteristic;
		private AndroidBluetoothDeviceReceiver _bluetoothBroadcastReceiver;
		[JsonIgnore]
		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			// BLE requires location permissions
			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() != PermissionStatus.Granted)
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Geolocation is not permitted on this device. Cannot start Bluetooth probe.";

				await SensusServiceHelper.Get().FlashNotificationAsync(error);

				throw new Exception(error);
			}

			_bluetoothAdapter = AndroidSensusServiceHelper.BluetoothManager?.Adapter;

			if ((_bluetoothAdapter?.IsEnabled ?? false) == false)
			{
				string error = "Bluetooth is not enabled. Cannot start Bluetooth probe.";

				await SensusServiceHelper.Get().FlashNotificationAsync(error);

				throw new Exception(error);
			}

			_deviceIdCharacteristic = new BluetoothGattCharacteristic(UUID.FromString(DEVICE_ID_CHARACTERISTIC_UUID), GattProperty.Read, GattPermission.Read);
			_deviceIdCharacteristic.SetValue(Encoding.UTF8.GetBytes(SensusServiceHelper.Get().DeviceId));

			_deviceIdService = new BluetoothGattService(UUID.FromString(Protocol.Id), GattServiceType.Primary);
			_deviceIdService.AddCharacteristic(_deviceIdCharacteristic);

			_bluetoothAdvertiserCallback = new AndroidBluetoothServerAdvertisingCallback(_deviceIdService, _deviceIdCharacteristic);

			if (ScanMode.HasFlag(BluetoothScanModes.Classic))
			{
				_bluetoothBroadcastReceiver = new AndroidBluetoothDeviceReceiver(this);

				IntentFilter intentFilter = new IntentFilter();
				intentFilter.AddAction(BluetoothDevice.ActionFound);

				Application.Context.RegisterReceiver(_bluetoothBroadcastReceiver, intentFilter);
			}
		}

		protected override Task ProtectedStopAsync()
		{
			Application.Context.UnregisterReceiver(_bluetoothBroadcastReceiver);

			return base.ProtectedStopAsync();
		}

		#region central -- scan
		protected override async Task ScanAsync(CancellationToken cancellationToken)
		{
			// start a scan if bluetooth is present and enabled
			if (_bluetoothAdapter?.IsEnabled ?? false)
			{
				try
				{
					if (ScanMode.HasFlag(BluetoothScanModes.LE))
					{
						List<ScanFilter> scanFilters = new List<ScanFilter>();

						if (DiscoverAll == false)
						{
							ScanFilter scanFilter = new ScanFilter.Builder().SetServiceUuid(new ParcelUuid(_deviceIdService.Uuid)).Build();

							scanFilters.Add(scanFilter);
						}

						ScanSettings.Builder scanSettingsBuilder = new ScanSettings.Builder().SetScanMode(global::Android.Bluetooth.LE.ScanMode.Balanced);

						// return batched scan results periodically if supported on the BLE chip
						if (_bluetoothAdapter.IsOffloadedScanBatchingSupported)
						{
							scanSettingsBuilder.SetReportDelay((long)(ScanDurationMS / 2.0));
						}

						// start a fresh manager delegate to collect/read results
						_bluetoothScannerCallback = new AndroidBluetoothClientScannerCallback(_deviceIdService, _deviceIdCharacteristic, this);

						_bluetoothAdapter.BluetoothLeScanner.StartScan(scanFilters, scanSettingsBuilder.Build(), _bluetoothScannerCallback);
					}

					if (ScanMode.HasFlag(BluetoothScanModes.Classic))
					{
						_bluetoothAdapter.StartDiscovery();
					}

					TaskCompletionSource<bool> scanCompletionSource = new TaskCompletionSource<bool>();

					cancellationToken.Register(() =>
					{
						try
						{
							if (ScanMode.HasFlag(BluetoothScanModes.LE))
							{
								_bluetoothAdapter.BluetoothLeScanner.StopScan(_bluetoothScannerCallback);
							}
							else if (ScanMode.HasFlag(BluetoothScanModes.Classic) && _bluetoothAdapter.IsDiscovering)
							{
								_bluetoothAdapter.CancelDiscovery();
							}
						}
						catch (Exception ex)
						{
							SensusServiceHelper.Get().Logger.Log("Exception while stopping scan:  " + ex.Message, LoggingLevel.Normal, GetType());
						}
						finally
						{
							scanCompletionSource.TrySetResult(true);
						}
					});

					await scanCompletionSource.Task;
				}
				catch (Exception ex)
				{
					SensusServiceHelper.Get().Logger.Log("Exception while scanning:  " + ex.Message, LoggingLevel.Normal, GetType());
				}
			}
		}

		protected async override Task<List<BluetoothDeviceProximityDatum>> ReadPeripheralCharacteristicValuesAsync(CancellationToken cancellationToken)
		{
			if (ScanMode == BluetoothScanModes.LE)
			{
				return await _bluetoothScannerCallback.ReadPeripheralCharacteristicValuesAsync(cancellationToken);
			}
			else
			{
				return await CreateBluetoothData(cancellationToken);
			}
		}

		private async Task<List<BluetoothDeviceProximityDatum>> CreateBluetoothData(CancellationToken cancellationToken)
		{
			List<BluetoothDeviceProximityDatum> bluetoothDeviceProximityData = new List<BluetoothDeviceProximityDatum>();

			// copy list of peripherals to read. note that the same device may be reported more than once. read each once.
			List<AndroidBluetoothDevice> le = _bluetoothScannerCallback.GetDiscoveredDevices();
			List<AndroidBluetoothDevice> classic = _bluetoothBroadcastReceiver.GetDiscoveredDevices();
			List<IGrouping<string, AndroidBluetoothDevice>> scanResults = le.Union(classic)
				.GroupBy(scanResult => scanResult.Device.Address)
				.ToList();

			ReadAttemptCount += scanResults.Count;

			// read characteristic from each peripheral
			foreach (IGrouping<string, AndroidBluetoothDevice> deviceGroup in scanResults)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				AndroidBluetoothClientGattCallback readCallback = null;

				try
				{
					readCallback = new AndroidBluetoothClientGattCallback(_deviceIdService, _deviceIdCharacteristic);
					BluetoothGatt gatt = deviceGroup.First().Device.ConnectGatt(Application.Context, false, readCallback);
					string deviceId = await readCallback.ReadCharacteristicValueAsync(cancellationToken);

					if (DiscoverAll || deviceId != null)
					{
						DateTimeOffset timestamp = deviceGroup.Min(x => x.Timestamp);

						string address = deviceGroup.Key;
						string name = deviceGroup.FirstOrDefault(x => x.Device.Name != null)?.Device.Name;
						int rssi = deviceGroup.Min(x => x.Rssi);
						bool paired = deviceGroup.Any(x => x.Device.BondState != Bond.None);

						bluetoothDeviceProximityData.Add(new BluetoothDeviceProximityDatum(timestamp, deviceId, address, name, rssi, paired, deviceId != null));
						ReadSuccessCount++;
					}
				}
				catch (Exception ex)
				{
					SensusServiceHelper.Get().Logger.Log("Exception while reading peripheral:  " + ex, LoggingLevel.Normal, GetType());
				}
				finally
				{
					readCallback?.DisconnectPeripheral();
				}
			}

			return bluetoothDeviceProximityData;
		}
		#endregion

		#region peripheral -- advertise
		protected override void StartAdvertising()
		{
			try
			{
				if (_bluetoothAdapter?.IsMultipleAdvertisementSupported ?? false)
				{
					AdvertiseSettings advertisingSettings = new AdvertiseSettings.Builder()
																				 .SetAdvertiseMode(AdvertiseMode.Balanced)
																				 .SetTxPowerLevel(AdvertiseTx.PowerLow)
																				 .SetConnectable(true)
																				 .Build();

					AdvertiseData advertisingData = new AdvertiseData.Builder()
																	 .SetIncludeDeviceName(false)
																	 .AddServiceUuid(new ParcelUuid(_deviceIdService.Uuid))
																	 .Build();

					_bluetoothAdapter?.BluetoothLeAdvertiser.StartAdvertising(advertisingSettings, advertisingData, _bluetoothAdvertiserCallback);
				}
				else
				{
					throw new Exception("BLE advertising is not available.");
				}
			}
			catch (Exception ex)
			{
				SensusServiceHelper.Get().Logger.Log("Exception while starting advertiser:  " + ex.Message, LoggingLevel.Normal, GetType());
			}
		}

		protected override void StopAdvertising()
		{
			try
			{
				_bluetoothAdapter?.BluetoothLeAdvertiser.StopAdvertising(_bluetoothAdvertiserCallback);
			}
			catch (Exception ex)
			{
				SensusServiceHelper.Get().Logger.Log("Exception while stopping advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
			}

			_bluetoothAdvertiserCallback.CloseServer();
		}

		public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
		{
			HealthTestResult result = await base.TestHealthAsync(events);

			if (State == ProbeState.Running)
			{
				// if the user disables/enables BT manually, we will no longer be advertising the service. start advertising
				// on each health test to ensure we're advertising.
				StartAdvertising();
			}

			return result;
		}
		#endregion
	}
}