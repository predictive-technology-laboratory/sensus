using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public class LogProbe : PollingProbe
	{
		private class LogProbeTextWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;
			private StringBuilder _buffer;
			private List<LogDatum> _data;

			public LogProbeTextWriter(List<LogDatum> data)
			{
				_buffer = new StringBuilder();
				_data = data;
			}

			public override void Write(char value)
			{
				lock (_buffer)
				{
					_buffer.Append(value);
				}
			}

			public override void WriteLine(string value)
			{
				base.Write(value);
				WriteLine();
			}

			public override void WriteLine()
			{
				string line = null;
				DateTimeOffset timestamp = DateTimeOffset.UtcNow;

				lock (_buffer)
				{
					line = _buffer.ToString();
					string lineTimestamp = Regex.Match(line, @"^.*?(?=:\s+)").Value;

					if (string.IsNullOrEmpty(lineTimestamp) == false && DateTime.TryParse(lineTimestamp, out DateTime localTimestamp))
					{
						timestamp = TimeZoneInfo.ConvertTimeToUtc(localTimestamp);
					}

					_buffer.Clear();
				}

				lock (_data)
				{
					if (string.IsNullOrEmpty(line) == false)
					{
						_data.Add(new LogDatum(line, timestamp));
					}
				}
			}
		}

		private readonly List<LogDatum> _data;
		private readonly LogProbeTextWriter _writer;

		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		public override string DisplayName => "Log";

		public override Type DatumType => typeof(LogDatum);

		public LogProbe()
		{
			_data = new List<LogDatum>();
			_writer = new LogProbeTextWriter(_data);
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

		protected override async Task ProtectedStartAsync()
		{
			await base.ProtectedStartAsync();

			if (SensusServiceHelper.Get().Logger is Logger logger)
			{
				logger.AddOtherOutput(_writer);
			}
		}

		protected override async Task ProtectedStopAsync()
		{
			await base.ProtectedStopAsync();

			if (SensusServiceHelper.Get().Logger is Logger logger)
			{
				logger.RemoveOtherOutput(_writer);
			}
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			lock (_data)
			{
				List<Datum> data = _data.Cast<Datum>().ToList();

				_data.Clear();

				return Task.FromResult(data);
			}
		}
	}
}
