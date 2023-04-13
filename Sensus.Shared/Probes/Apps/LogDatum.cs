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

using System;
using System.Text.RegularExpressions;

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

		public override object StringPlaceholderValue
		{
			get
			{
				return "(Log data)";
			}
		}

		public static DateTime GetTimestamp(string line)
		{
			string lineTimestamp = Regex.Match(line, @"^.*?(?=:\s+)").Value;

			if (string.IsNullOrEmpty(lineTimestamp) == false && DateTime.TryParse(lineTimestamp, out DateTime localTimestamp))
			{
				return TimeZoneInfo.ConvertTimeToUtc(localTimestamp);
			}

			return DateTime.Now;
		}

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		public LogDatum()
		{

		}

		public LogDatum(string message) : base(GetTimestamp(message))
		{
			_logMessage = message;
		}

		/// <summary>
		/// The message that was logged.
		/// </summary>
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

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + _logMessage;
		}
	}
}
