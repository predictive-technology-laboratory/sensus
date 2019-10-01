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

		/// <summary>
		/// The name of the app package.
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
		/// The name of the app.
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
		/// The start time of the interval of the stats.
		/// </summary>
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
		/// <summary>
		/// The end time of the interval of the stats.
		/// </summary>
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
		/// <summary>
		/// The date the app was last used.
		/// </summary>
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
		/// <summary>
		/// The amount of time the app spent in the foreground over the interval.
		/// </summary>
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
