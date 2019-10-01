using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using System;

namespace Sensus.Probes.Apps
{
	public class ApplicationUsageStatsDatum : Datum
	{
		private string _packageName;
		private string _applicationName;
		private DateTimeOffset _intervalStart;
		private DateTimeOffset _intervalEnd;
		private DateTimeOffset _lastUsed;
		private TimeSpan _timeInForeground;

		public override string DisplayDetail => ApplicationName;

		public override object StringPlaceholderValue => ApplicationName;

		public ApplicationUsageStatsDatum(string packageName, string applicationName, DateTimeOffset intervalStart, DateTimeOffset intervalEnd, DateTimeOffset lastUsed, TimeSpan timeInForeground, DateTimeOffset timestamp) : base(timestamp)
		{
			_packageName = packageName;
			_applicationName = applicationName;
			_intervalStart = intervalStart;
			_intervalEnd = intervalEnd;
			_lastUsed = lastUsed;
			_timeInForeground = timeInForeground;
		}

		[Anonymizable("Package Name:", typeof(StringHashAnonymizer), false)]
		public string PackageName
		{
			get
			{
				return _packageName;
			}
			set
			{
				_packageName = value;
			}
		}
		[Anonymizable("Application Name:", typeof(StringHashAnonymizer), false)]
		public string ApplicationName
		{
			get
			{
				return _applicationName;
			}
			set
			{
				_applicationName = value;
			}
		}
		public DateTimeOffset IntervalStart
		{
			get
			{
				return _intervalStart;
			}
			set
			{
				_intervalStart = value;
			}
		}
		public DateTimeOffset IntervalEnd
		{
			get
			{
				return _intervalEnd;
			}
			set
			{
				_intervalEnd = value;
			}
		}
		public DateTimeOffset LastUsed
		{
			get
			{
				return _lastUsed;
			}
			set
			{
				_lastUsed = value;
			}
		}
		public TimeSpan TimeInForeground
		{
			get
			{
				return _timeInForeground;
			}
			set
			{
				_timeInForeground = value;
			}
		}

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + $"{_packageName} for {_timeInForeground} between {_intervalStart} and {_intervalEnd}";
		}
	}
}
