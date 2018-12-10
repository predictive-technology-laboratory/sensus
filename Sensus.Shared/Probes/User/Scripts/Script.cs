//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
    public class Script : INotifyPropertyChanged, IComparable<Script>, IScript
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
        public IScriptRunner IRunner { get => Runner; }  // for NuGet interfacing
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
                FireCaptionChanged();

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
                FireCaptionChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Sensus.Probes.User.Scripts.Script"/> is valid. A valid <see cref="Script"/> is
        /// one in which each <see cref="InputGroup"/> is <see cref="InputGroup.Valid"/>.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
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

        private void FireCaptionChanged()
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

        public int CompareTo(Script script)
        {
            return DisplayDateTime.CompareTo(script.DisplayDateTime);
        }
    }
}
