using Sensus.Context;
using Syncfusion.SfChart.XForms;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Probes.Apps
{
	public class ListeningLogProbe : ListeningProbe
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

				Task.Run(async () => await _probe.StoreDatumAsync(logDatum));
			}
		}

		private LogProbeDatumWriter _writer;

		public override string DisplayName => "Log";

		public override Type DatumType => typeof(LogDatum);

		protected override bool DefaultKeepDeviceAwake => false;

		protected override string DeviceAwakeWarning => "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";

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

		protected override async Task ProtectedStartAsync()
		{
			await base.ProtectedStartAsync();

			if (SensusServiceHelper.Get().Logger is Logger logger)
			{
				_writer ??= new(this);

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
	}
}
