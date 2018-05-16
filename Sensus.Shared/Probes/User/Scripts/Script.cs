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
using System.ComponentModel;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Sensus.Probes.User.Scripts
{
    public class Script : INotifyPropertyChanged, IComparable<Script>
    {
        /// <summary>
        /// Contract resolver for copying <see cref="Script"/>s. This is necessary because each <see cref="Script"/> contains
        /// a reference to its associated <see cref="ScriptRunner"/>, which contains other references that make JSON 
        /// serialization and deserialization an expensive operation. We use JSON serialization/deserialization for <see cref="Script"/>s
        /// because there are complicated objective references between the <see cref="InputGroup"/>s and <see cref="Input"/>s
        /// that are associated with the <see cref="Script"/>. We use the contract resolver to prevent copying of the 
        /// <see cref="ScriptRunner"/>.
        /// </summary>
        private class CopyContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                // copy all properties except the script runner
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => p.PropertyName != nameof(Script.Runner)).ToList();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _submitting;
        private Datum _currentDatum;

        private JsonSerializerSettings _copySettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new CopyContractResolver()
        };

        public string Id { get; set; }
        public ScriptRunner Runner { get; set; }
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

                // update the triggering datum on all inputs
                foreach (InputGroup inputGroup in InputGroups)
                {
                    foreach (Input input in inputGroup.Inputs)
                    {
                        input.TriggeringDatum = _currentDatum;
                    }
                }
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
                // format the runner's name to replace any {0} references with the current datum's placeholder value. there won't be a current datum for
                // scheduled or run-on-start scripts.
                return string.Format(Runner.Name, CurrentDatum?.StringPlaceholderValue.ToString().ToLower()) + (Submitting ? " (Submitting...)" : "");
            }
        }

        [JsonIgnore]
        //Pulling out the display date time from the sub caption in order
        // to be able to sort on this localized time.
        public DateTime DisplayDateTime
        {
            get
            {
                DateTime displayDateTime = Birthdate;

                if (Runner.UseTriggerDatumTimestampInSubcaption && _currentDatum != null)
                {
                    displayDateTime = _currentDatum.Timestamp.ToLocalTime().DateTime;
                }

                return displayDateTime;
            }
        }

        [JsonIgnore]
        public string SubCaption
        {
            get
            {
                return Runner.Probe.Protocol.Name + " - " + DisplayDateTime;
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private Script()
        {
            Id = Guid.NewGuid().ToString();
            InputGroups = new ObservableCollection<InputGroup>();
        }

        public Script(ScriptRunner runner)
            : this()
        {
            Runner = runner;
        }

        private void CaptionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
        }

        /// <summary>
        /// Creates a copy of the current <see cref="Script"/>.
        /// </summary>
        /// <returns>The copy.</returns>
        /// <param name="newId">If set to <c>true</c>, set a new random <see cref="Script.Id"/> on the script. Doing so does not change
        /// the <see cref="InputGroup.Id"/> or <see cref="Input.Id"/> values associated with this <see cref="Script"/>.</param>
        public Script Copy(bool newId)
        {
            // copy the script except for the script runner
            Script copy = JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, _copySettings), _copySettings);

            if (newId)
            {
                copy.Id = Guid.NewGuid().ToString();
            }

            // attach the script runner to the copy
            copy.Runner = Runner;

            return copy;
        }

        //Compare to using the DisplayDateTime.  This is used in order to determine the sort order for placement
        public int CompareTo(Script that)
        {
            return this.DisplayDateTime.CompareTo(that.DisplayDateTime);
        }
    }
}