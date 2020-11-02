using Syncfusion.SfChart.XForms;
using System;

namespace Sensus.Probes.User.Health
{
	public abstract class HealthDataProbe : PollingProbe
	{
		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromDays(1).TotalMilliseconds;

		public override string DisplayName => "Health";

		public override Type DatumType => typeof(HealthDatum);

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
