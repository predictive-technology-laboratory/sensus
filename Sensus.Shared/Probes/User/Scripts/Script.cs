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
using System.Linq;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Sensus.UI.Inputs;
using Sensus.Extensions;

namespace Sensus.Probes.User.Scripts
{
    public class Script
    {
        #region Properties
        public string Id { get; }

        public ScriptRunner Runner { get; }

        public ObservableCollection<InputGroup> InputGroups { get; }

        public DateTimeOffset? ScheduledRunTime { get; set; }

        public DateTimeOffset? RunTime { get; set; }

        public Datum PreviousDatum { get; set; }

        public Datum CurrentDatum { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [JsonIgnore]
        public bool Valid => InputGroups.Count == 0 || InputGroups.All(inputGroup => inputGroup.Valid);

        [JsonIgnore]
        public bool Expired => ExpirationDate < DateTime.Now;

        /// <summary>
        /// Gets the birthdate of the script.
        /// </summary>
        /// <value>The birthdate.</value>
        /// <remarks>
        /// The scheduled time will always be slightly before the run time, depending on latencies in the android/ios alarm/notification systems.
        /// Furthermore, on ios notifications are delivered to the tray and not to the app when the app is backgrounded. The user must open the 
        /// notification in order for the script to run. In this case the scheduled time could significantly precede the run time. In any case, 
        /// the scheduled time is the right thing to use as the script's birthdate.
        /// </remarks>
        [JsonIgnore]
        public DateTime Birthdate => (ScheduledRunTime ?? RunTime).Value.LocalDateTime;
        #endregion

        #region Constructors
        public Script(Script script)
        {
            Id = script.Id;
            Runner = script.Runner;
            InputGroups = script.InputGroups.Select(g => new InputGroup(g)).ToObservableCollection();
            ScheduledRunTime = script.ScheduledRunTime;
            RunTime = script.RunTime;
            PreviousDatum = script.PreviousDatum;
            CurrentDatum = script.CurrentDatum;
            ExpirationDate = script.ExpirationDate;
        }

        public Script(Script script, Guid guid) : this(script)
        {
            Id = guid.ToString();
        }

        public Script(ScriptRunner runner)
        {
            Id = Guid.NewGuid().ToString();
            Runner = runner;
            InputGroups = new ObservableCollection<InputGroup>();
        }

        [JsonConstructor]
        private Script(ScriptRunner runner, string id, ObservableCollection<InputGroup> inputGroups)
        {
            Id = id;
            Runner = runner;
            InputGroups = inputGroups;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks whether the current and another script share a parent script.
        /// </summary>
        /// <returns><c>true</c>, if parent script is shared, <c>false</c> otherwise.</returns>
        public bool SharesParentScriptWith(Script script)
        {
            return Runner.Script.Id == script.Runner.Script.Id;
        }
        #endregion
    }
}