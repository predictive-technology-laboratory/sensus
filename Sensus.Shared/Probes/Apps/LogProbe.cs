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
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	/// <summary>
	/// Collects keystroke information as <see cref="LogDatum"/>
	/// </summary>
	public class LogProbe : PollingProbe
	{
		private class LogProbeDatumWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;
			private readonly string _path;
			private readonly object _writeLock;

			public LogProbeDatumWriter(string path, object writeLock)
			{
				_path = path;
				_writeLock = writeLock;
			}

			public override void WriteLine(string value)
			{
				string path = Path.Combine(_path, $"{Guid.NewGuid()}.txt");

				while (File.Exists(path))
				{
					path = Path.Combine(_path, $"{Guid.NewGuid()}.txt");
				}

				lock (_writeLock)
				{
					using StreamWriter writer = new(path);

					writer.Write(value);
				}
			}
		}

		private readonly object _writeLocker;
		private string _path;
		private LogProbeDatumWriter _writer;

		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		public override string DisplayName => "Log";

		public override Type DatumType => typeof(LogDatum);

		public LogProbe()
		{
			_writeLocker = new();
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
				_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Protocol.Id, nameof(LogProbe));

				Directory.CreateDirectory(_path);

				_writer ??= new(_path, _writeLocker);

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
			List<Datum> data = new();

			lock (_writeLocker)
			{
				string[] logDatumFiles = Directory.GetFiles(_path, "*.txt");

				foreach (string path in logDatumFiles)
				{
					try
					{
						using StreamReader reader = new(path);
						string line = reader.ReadToEnd();

						LogDatum datum = new(line);

						data.Add(datum);

						File.Delete(path);
					}
					catch (Exception ex)
					{
						SensusServiceHelper.Get().Logger.Log("Exception while polling: " + ex, LoggingLevel.Normal, GetType());
					}
				}
			}

			return Task.FromResult(data);
		}
	}
}
