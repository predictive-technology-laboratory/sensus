using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Apps
{
	public class LogDatum : Datum
	{
		public override string DisplayDetail => LogMessage;

		public override object StringPlaceholderValue => throw new NotImplementedException();

		public LogDatum(string logLine, DateTimeOffset timestamp) : base(timestamp)
		{
			LogMessage = logLine;
		}

		public string LogMessage { get; set; }
	}
}
