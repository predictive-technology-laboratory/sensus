using System;

namespace Sensus.Probes.Apps
{
	public class LogDatum : Datum
	{
		private string _logMessage;

		public override string DisplayDetail
		{
			get
			{
				return "(Log data)";
			}
		}

		public override object StringPlaceholderValue => "(Log data)";

		public LogDatum(string logMessage, DateTimeOffset timestamp) : base(timestamp)
		{
			_logMessage = logMessage;
		}

		public string LogMessage
		{
			get
			{
				return _logMessage;
			}
			set
			{
				_logMessage = value;
			}
		}
	}
}
