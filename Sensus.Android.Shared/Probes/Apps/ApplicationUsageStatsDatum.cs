using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Android.Probes.Apps
{
	public class ApplicationUsageStatsDatum : Datum
	{
		public override string DisplayDetail
        {
            get { return "Application Usage Stats"; }
        }

        public override object StringPlaceholderValue => throw new NotImplementedException();

		public ApplicationUsageStatsDatum(string packageName, string applicationName, DateTimeOffset intervalStart, DateTimeOffset intervalEnd, DateTimeOffset lastUsed, TimeSpan timeInForeground, DateTimeOffset timestamp) : base(timestamp)
		{
			PackageName = packageName;
			ApplicationName = applicationName;
			IntervalStart = intervalStart;
			IntervalEnd = intervalEnd;
			LastUsed = lastUsed;
			TimeInForeground = timeInForeground;
		}

		[Anonymizable("Package Name:", typeof(StringHashAnonymizer), false)]
		public string PackageName { get; set; }
		[Anonymizable("Application Name:", typeof(StringHashAnonymizer), false)]
		public string ApplicationName { get; set; }
		public DateTimeOffset IntervalStart { get; set; }
		public DateTimeOffset IntervalEnd { get; set; }
		public DateTimeOffset LastUsed { get; set; }
		public TimeSpan TimeInForeground { get; set; }
	}
}
