using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plugin.Permissions.Abstractions;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	/// <summary>
	/// Collects calendar events as <see cref="CalendarDatum"/>
	/// </summary>
	public abstract class CalendarProbe : PollingProbe
	{
		private DateTime _lastPollTime;

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

		[JsonIgnore]
		public override int DefaultPollingSleepDurationMS
		{
			get
			{
				return (int)TimeSpan.FromDays(1).TotalMilliseconds;
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

		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();

			_lastPollTime = DateTime.Now;

			if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Calendar) != PermissionStatus.Granted)
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Calendar permission is not permitted on this device. Cannot start Calendar probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			List<Datum> calendarMetaData = new List<Datum>();

			foreach (Datum calendarDatum in GetCalendarEventsAsync())
			{
				calendarMetaData.Add(calendarDatum);
			}

			_lastPollTime = DateTime.Now;

			return Task.FromResult(calendarMetaData);
		}

		protected abstract List<CalendarDatum> GetCalendarEventsAsync();
	}
}
