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
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;
using Xamarin.Essentials;

namespace Sensus.Probes.Network
{
	/// <summary>
	/// Probes information about WLAN access points.
	/// </summary>
	public abstract class PollingWlanProbe : PollingProbe
	{
		public sealed override string DisplayName
		{
			get { return "Wireless LAN Binding"; }
		}

		public sealed override Type DatumType
		{
			get { return typeof(WlanDatum); }
		}

		public sealed override int DefaultPollingSleepDurationMS
		{
			get
			{
				return 60000 * 15; // every 15 minutes
			}
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if ((await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.NetworkState>()) != PermissionStatus.Granted)
			{
				throw new PermissionException($"Failed to get {nameof(Permissions.NetworkState)} permission");
			}
		}

		protected override ChartSeries GetChartSeries()
		{
			return null;
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			return null;
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}
	}
}