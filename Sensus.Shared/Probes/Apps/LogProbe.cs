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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	/// <summary>
	/// Collects keystroke information as <see cref="LogDatum"/>
	/// </summary>
	public class LogProbe : PollingProbe, ILogProbe
	{
		private class LogProbeDatumWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;
			private readonly object _writeLock;

			public LogProbeDatumWriter(object writeLock)
			{
				_writeLock = writeLock;
			}

			public string LogPath { get; set; }

			public override void WriteLine(string value)
			{
				string path = Path.Combine(LogPath, $"{Guid.NewGuid()}.txt");

				while (File.Exists(path))
				{
					path = Path.Combine(LogPath, $"{Guid.NewGuid()}.txt");
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
		private readonly LogProbeDatumWriter _writer;

		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		public override string DisplayName => "Log";

		public override Type DatumType => typeof(LogDatum);

		public LogProbe()
		{
			_writeLocker = new();
			_writer = new(_writeLocker);
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

		public void AttachToLogger()
		{
			if (SensusServiceHelper.Get().Logger is Logger logger)
			{
				_path = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Protocol.Id), nameof(LogProbe));

				_writer.LogPath = _path;

				Directory.CreateDirectory(_path);

				logger.AddOtherOutput(_writer);
			}
		}

		protected override async Task ProtectedStartAsync()
		{
			await base.ProtectedStartAsync();
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
