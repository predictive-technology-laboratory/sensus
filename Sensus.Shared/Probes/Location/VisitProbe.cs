using Syncfusion.SfChart.XForms;
using System;

namespace Sensus.Probes.Location
{
	public abstract class VisitProbe : ListeningProbe
	{
		protected override bool DefaultKeepDeviceAwake => false;

		public override Type DatumType => typeof(VisitDatum);

		public override string DisplayName => "Visits";

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
