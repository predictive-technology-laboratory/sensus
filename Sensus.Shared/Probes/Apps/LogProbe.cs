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
	/// <summary>
	/// Collects keystroke information as <see cref="LogDatum"/>
	/// </summary>
	public class LogProbe : PollingProbe
	{
		private class LogProbeTextWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;
			private readonly StringBuilder _buffer;
			private readonly List<LogDatum> _data;

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
