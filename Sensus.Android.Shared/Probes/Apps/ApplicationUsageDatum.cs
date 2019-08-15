using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Android.Probes.Apps
{
	public class ApplicationUsageDatum : Datum
	{
		public override string DisplayDetail => throw new NotImplementedException();

		public override object StringPlaceholderValue => throw new NotImplementedException();

		public ApplicationUsageDatum(string packageName, string applicationName, string eventType, DateTimeOffset timestamp) : base(timestamp)
		{
			PackageName = packageName;
			ApplicationName = applicationName;
			EventType = eventType;
		}

		public string PackageName { get; set; }
		public string ApplicationName { get; set; }
		public string EventType { get; set; }
	}
}
