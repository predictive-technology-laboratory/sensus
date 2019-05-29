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
	public abstract class ImageMetadataProbe : PollingProbe
	{
		private int _readDurationMS;

		public override string DisplayName => "Gallery Image Metadata";

		public sealed override Type DatumType => typeof(ImageMetadataDatum);

		[EntryIntegerUiProperty("Read Duration (MS):", true, 5, true)]
		public int ReadDurationMS
		{
			get
			{
				return _readDurationMS;
			}
			set
			{
				if (value < 5000)
				{
					value = 5000;
				}

				_readDurationMS = value;
			}
		}

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

		protected async override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			List<Datum> imageMetadataData = new List<Datum>();

			imageMetadataData.AddRange(await GetImages());

			return imageMetadataData;
		}

		protected abstract Task<List<ImageMetadataDatum>> GetImages();
	}
}
