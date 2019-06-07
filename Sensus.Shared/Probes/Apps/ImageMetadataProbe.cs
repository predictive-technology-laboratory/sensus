using System;
using System.Collections.Generic;
using System.Text;
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

using System.Threading;
using System.Threading.Tasks;
using Sensus.UI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public abstract class ImageMetadataProbe : ListeningProbe
	{
		protected override bool DefaultKeepDeviceAwake => false;

		public override Type DatumType => typeof(ImageMetadataDatum);

		public override string DisplayName => "Gallery Image Metadata";

		protected override string DeviceAwakeWarning => "";

		protected override string DeviceAsleepWarning => "";

		[OnOffUiProperty("Store images:", true, 2)]
		public bool StoreImages { get; set; }
		[OnOffUiProperty("Store videos:", true, 3)]
		public bool StoreVideos { get; set; }

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			throw new NotImplementedException();
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override ChartSeries GetChartSeries()
		{
			throw new NotImplementedException();
		}
	}
}
