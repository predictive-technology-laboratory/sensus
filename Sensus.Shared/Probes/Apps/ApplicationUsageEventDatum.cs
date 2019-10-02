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

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		public ApplicationUsageEventDatum()
		{

		}

		public ApplicationUsageEventDatum(string packageName, string applicationName, string eventType, DateTimeOffset timestamp) : base(timestamp)
		{
			_packageName = packageName;
			_applicationName = applicationName;
			_eventType = eventType;
		}

		/// <summary>
		/// The name of the package that created the event.
		/// </summary>
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
		/// <summary>
		/// The name of the application that created the event.
		/// </summary>
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
		/// <summary>
		/// The type of event.
		/// </summary>
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
