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
using System.ComponentModel;

namespace Sensus.Probes.User.Scripts
{
    public class Script : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _submitting;
        private Datum _currentDatum;
        public string Id { get; }
        public ScriptRunner Runner { get; }
        public ObservableCollection<InputGroup> InputGroups { get; }
        public DateTimeOffset? ScheduledRunTime { get; set; }
        public DateTimeOffset? RunTime { get; set; }
        public Datum PreviousDatum { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public Datum CurrentDatum
        {
            get
            {
                return _currentDatum;
            }
            set
            {
                _currentDatum = value;
                CaptionChanged();
            }
        }

        [JsonIgnore]
        public bool Submitting
        {
            get
            {
                return _submitting;
            }
            set
            {
                _submitting = value;
                CaptionChanged();
            }
        }

        [JsonIgnore]
        public bool Valid => InputGroups.Count == 0 || InputGroups.All(inputGroup => inputGroup.Valid);

        [JsonIgnore]
        public bool Expired => ExpirationDate < DateTime.Now;

        /// <summary>
        /// Gets the birthdate of the script, which is when it was first made available to the user for completion.
        /// </summary>
        /// <value>The birthdate.</value>
        /// <remarks>
        /// The scheduled time will always be slightly before the run time, depending on latencies in the android/ios alarm/notification systems.
        /// Furthermore, on ios notifications are delivered to the tray and not to the app when the app is backgrounded. The user must open the 
        /// notification in order for the script to run. In this case the scheduled time could significantly precede the run time. In any case, 
        /// the scheduled time is the right thing to use as the script's birthdate. On the other hand, not all scripts are scheduled (e.g., those
        /// that are triggered by other probes). For such scripts the only thing we'll have is the run time.
        /// </remarks>
        [JsonIgnore]
        public DateTime Birthdate => (ScheduledRunTime ?? RunTime).Value.LocalDateTime;

        [JsonIgnore]
        public string Caption
        {
            get
            {
                // format the runner's name to replace any {0} references with the current datum's placeholder value.
                return string.Format(Runner.Name, CurrentDatum.StringPlaceholderValue.ToString().ToLower()) + (Submitting ? " (Submitting...)" : "");
            }
        }

        [JsonIgnore]
        public string SubCaption
        {
            get { return Runner.Probe.Protocol.Name + " - " + Birthdate; }
        }

        public Script(Script script)
        {
            Id = script.Id;
            Runner = script.Runner;
            InputGroups = script.InputGroups.Select(g => new InputGroup(g, false)).ToObservableCollection();  // don't reset the group ID or input IDs. we're copying the script to run it.

            // update input object references within any display conditions
            Input[] allInputs = InputGroups.SelectMany(group => group.Inputs).ToArray();
            foreach (InputGroup inputGroup in InputGroups)
            {
                inputGroup.UpdateDisplayConditionInputs(allInputs);
            }

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

        private void CaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
        }
    }
}