using System;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public abstract class ApplicationUsageStatsProbe : PollingProbe
	{
		public override int DefaultPollingSleepDurationMS
		{
			get
			{
				return (int)TimeSpan.FromHours(1).TotalMilliseconds;
			}
		}

		public override string DisplayName
		{
			get
			{
				return "Application Stats";
			}
		}

		public override Type DatumType
		{
			get
			{
				return typeof(ApplicationUsageEventDatum);
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
	}
}
