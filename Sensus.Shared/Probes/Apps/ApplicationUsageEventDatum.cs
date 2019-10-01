using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using System;

namespace Sensus.Probes.Apps
{
	public class ApplicationUsageEventDatum : Datum
	{
		private string _packageName;
		private string _applicationName;
		private string _eventType;

		public override string DisplayDetail
		{
			get
			{
				return ApplicationName;
			}
		}

		public override object StringPlaceholderValue
		{
			get
			{
				return ApplicationName;
			}
		}

		public ApplicationUsageEventDatum(string packageName, string applicationName, string eventType, DateTimeOffset timestamp) : base(timestamp)
		{
			_packageName = packageName;
			_applicationName = applicationName;
			_eventType = eventType;
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
		public string EventType
		{
			get
			{
				return _eventType;
			}
			set
			{
				_eventType = value;
			}
		}

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + $"{_packageName}: {_eventType}";
		}
	}
}
