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
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;

namespace Sensus.Probes.Apps
{
    public class KeystrokeDatum : Datum
    {
        private string _key;
        private string _app;

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		public KeystrokeDatum()
		{

		}

        public KeystrokeDatum(DateTimeOffset timestamp, string key, string app) : base(timestamp)
        {
            _key = key == null ? "" : key;
            _app = app == null ? "" : app;
        }

        public override string DisplayDetail
        {
            get
            {
                return "(Keystroke Data)";
            }
        }

		/// <summary>
		/// The key pressed.
		/// </summary>
		[StringProbeTriggerProperty]
        [Anonymizable(null, new[] { typeof(StringHashAnonymizer), typeof(RegExAnonymizer) }, -1)]
        public string Key { get => _key; set => _key = value; }

		/// <summary>
		/// The app that received the keystroke.
		/// </summary>
		[StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string App { get => _app; set => _app = value; }

		public override object StringPlaceholderValue
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                      "Key:  " + _key + Environment.NewLine +
                      "App:  " + _app + Environment.NewLine;
        }
    }
}
