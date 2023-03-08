﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Sensus.Probes.Location;
using Plugin.Geolocator.Abstractions;
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Probes.Movement
{
	public class ListeningSpeedProbe : ListeningProbe
	{
		private EventHandler<PositionEventArgs> _positionChangedHandler;
		private Position _previousPosition;

		private readonly object _locker = new object();

		[JsonIgnore]
		protected override bool DefaultKeepDeviceAwake
		{
			get
			{
				return true;
			}
		}

		[JsonIgnore]
		protected override string DeviceAwakeWarning
		{
			get
			{
				return "This setting does not affect iOS. Android devices will use additional power to report all updates.";
			}
		}

		[JsonIgnore]
		protected override string DeviceAsleepWarning
		{
			get
			{
				return "This setting does not affect iOS. Android devices will sleep and pause updates.";
			}
		}

		/// <summary>
		/// This <see cref="Probe"/> uses continuous GPS listening and will have a significant negative impact on battery life.
		/// </summary>
		protected override bool WillHaveSignificantNegativeImpactOnBattery => true;

		public sealed override string DisplayName
		{
			get
			{
				return "Speed";
			}
		}

		public sealed override Type DatumType
		{
			get
			{
				return typeof(SpeedDatum);
			}
		}

		public ListeningSpeedProbe()
		{
			_positionChangedHandler = async (o, e) =>
			{
				if (e.Position == null)
				{
					return;
				}

				Datum datum = null;

				lock (_locker)
				{
					SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

					if (_previousPosition == null)
					{
						_previousPosition = e.Position;
					}
					else if (e.Position.Timestamp > _previousPosition.Timestamp)  // it has happened (rarely) that positions come in out of order...drop any such positions.
					{
						datum = new SpeedDatum(e.Position.Timestamp, _previousPosition, e.Position);
						_previousPosition = e.Position;
					}
				}

				if (datum != null)
				{
					await StoreDatumAsync(datum);
				}
			};
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() != PermissionStatus.Granted)
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Geolocation is not permitted on this device. Cannot start speed probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}

			_previousPosition = null;
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			await GpsReceiver.Get().AddListenerAsync(_positionChangedHandler, false);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			await GpsReceiver.Get().RemoveListenerAsync(_positionChangedHandler);

			_previousPosition = null;
		}

		public override async Task ResetAsync()
		{
			await base.ResetAsync();

			_previousPosition = null;
		}

		protected override ChartSeries GetChartSeries()
		{
			return new LineSeries();
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as SpeedDatum).KPH);
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			return new DateTimeAxis
			{
				Title = new ChartAxisTitle
				{
					Text = "Time"
				}
			};
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			return new NumericalAxis
			{
				Title = new ChartAxisTitle
				{
					Text = "Speed (KPH)"
				}
			};
		}
	}
}