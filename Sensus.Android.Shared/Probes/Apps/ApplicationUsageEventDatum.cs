using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Android.Probes.Apps
{
	public class ApplicationUsageEventDatum : Datum
	{
		public override string DisplayDetail
        {
            get { return "Application Usage Events"; }
        }

        public override object StringPlaceholderValue => throw new NotImplementedException();

		public ApplicationUsageEventDatum(string packageName, string applicationName, string eventType, DateTimeOffset timestamp) : base(timestamp)
		{
			PackageName = packageName;
			ApplicationName = applicationName;
			EventType = eventType;
		}

		[Anonymizable("Package Name:", typeof(StringHashAnonymizer), false)]
		public string PackageName { get; set; }
		[Anonymizable("Application Name:", typeof(StringHashAnonymizer), false)]
		public string ApplicationName { get; set; }
		public string EventType { get; set; }
	}
}
