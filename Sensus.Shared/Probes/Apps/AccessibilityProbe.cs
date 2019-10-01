using System;
using System.Collections.Generic;
using System.Text;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public abstract class AccessibilityProbe : ListeningProbe
	{
		public override string DisplayName => "Accessibility";

		public override Type DatumType => typeof(AccessibilityDatum);

		protected override bool DefaultKeepDeviceAwake => false;

		protected override string DeviceAwakeWarning => "";

		protected override string DeviceAsleepWarning => "";

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
