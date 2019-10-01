using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public abstract class CalendarProbe : PollingProbe
	{
		private DateTime _lastPollTime = DateTime.Now.AddHours(-24);

		public override string DisplayName
		{
			get
			{
				return "Calendar Events";
			}
		}

		public override Type DatumType
		{
			get
			{
				return typeof(CalendarDatum);
			}
		}

		public DateTime LastPollTime
		{
			get
			{
				return _lastPollTime;
			}
			set
			{
				_lastPollTime = value;
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
			List<Datum> calendarMetaData = new List<Datum>();

			foreach (Datum calendarDatum in await GetCalendarEventsAsync())
			{
				calendarMetaData.Add(calendarDatum);
			}

			LastPollTime = DateTime.UtcNow;

			return calendarMetaData;
		}

		protected abstract Task<List<CalendarDatum>> GetCalendarEventsAsync();
	}
}
