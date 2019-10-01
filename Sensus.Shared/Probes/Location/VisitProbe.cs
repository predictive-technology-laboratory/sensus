using Syncfusion.SfChart.XForms;
using System;

namespace Sensus.Probes.Location
{
	public abstract class VisitProbe : ListeningProbe
	{
		protected override bool DefaultKeepDeviceAwake
		{
			get
			{
				return false;
			}
		}

		public override Type DatumType
		{
			get
			{
				return typeof(VisitDatum);
			}
		}

		public override string DisplayName
		{
			get
			{
				return "Visits";
			}
		}

		protected override string DeviceAwakeWarning
		{
			get
			{
				return "";
			}
		}

		protected override string DeviceAsleepWarning
		{
			get
			{
				return "";
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
