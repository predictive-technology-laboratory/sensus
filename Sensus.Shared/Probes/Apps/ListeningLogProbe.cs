using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Probes.Apps
{
	public class ListeningLogProbe : ListeningProbe, ILogProbe
	{
		private class LogProbeDatumWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;
			private readonly ListeningLogProbe _probe;

			public LogProbeDatumWriter(ListeningLogProbe probe)
			{
				_probe = probe;
			}

			public override void WriteLine(string value)
			{
				LogDatum logDatum = new(value);

				// do not use Probe.StoreDatumAsync() because:
				// - it only adds datum records while the protocol is in the running state and would miss messages logged while the protocol is in the Starting and Stopping states
				// - it can log messages and lead to an infinite loop
				// - and to a lesser extent, because it is async
				_probe.StoreDatum(logDatum);
			}
		}

		private readonly LogProbeDatumWriter _writer;

		public override string DisplayName => "Log";

		public override Type DatumType => typeof(LogDatum);

		public ListeningLogProbe()
		{
			_writer = new(this);
		}

		protected override bool DefaultKeepDeviceAwake => false;

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

		public void AttachToLogger()
		{
			if (SensusServiceHelper.Get().Logger is Logger logger)
			{
				logger.AddOtherOutput(_writer);
			}
		}

		protected override async Task ProtectedStartAsync()
		{
			AttachToLogger();

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

		public void StoreDatum(LogDatum datum)
		{
			// store non-null data
			if (StoreData && datum != null)
			{
				try
				{
					// set properties that we were unable to set within the datum constructor.
					datum.ProtocolId = Protocol.Id;
					datum.ParticipantId = Protocol.ParticipantId;

					// tag the data if we're in tagging mode, indicated with a non-null event id on the protocol. avoid 
					// any race conditions related to starting/stopping a tagging by getting the required values and
					// then checking both for validity. we need to guarantee that any tagged datum has both an id and tags.
					string taggedEventId = Protocol.TaggedEventId;
					List<string> taggedEventTags = Protocol.TaggedEventTags.ToList();
					if (!string.IsNullOrWhiteSpace(taggedEventId) && taggedEventTags.Count > 0)
					{
						datum.TaggedEventId = taggedEventId;
						datum.TaggedEventTags = taggedEventTags;
					}

					// write datum to local data store. catch any exceptions, as the caller (e.g., a listening 
					// probe) could very well be unprotected on the UI thread. throwing an exception here can crash the app.
					Protocol.LocalDataStore.WriteDatum(datum, CancellationToken.None);
				}
				catch
				{
					// we can't log this exception since it would cause this method to be called again and might cause an infinite loop
				}
			}
		}
	}
}
