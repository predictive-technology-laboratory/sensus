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
using System.Collections.Specialized;
using Sensus.UI.UiProperties;
using Newtonsoft.Json;

namespace Sensus.UI.Inputs
{
    public class InputGroup
    {
        #region Properties     
        public string Id { get; }

        public ObservableCollection<Input> Inputs { get; }

        [EntryStringUiProperty(null, true, 0)]
        public string Name { get; set; }

        [OnOffUiProperty(null, true, 1)]
        public bool Geotag { get; set; }

        [OnOffUiProperty("Force Valid Inputs:", true, 2)]
        public bool ForceValidInputs { get; set; }

        [OnOffUiProperty("Shuffle Inputs:", true, 3)]
        public bool ShuffleInputs { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="InputGroup"/> is valid.
        /// A valid input group is one in which each <see cref="Input"/> in the group is valid.
        /// An input group with no inputs is deemed valid by default.
        /// </summary>
        [JsonIgnore]
        public bool Valid => Inputs.All(i => i?.Valid ?? true);
        #endregion

        #region Constructors
        public InputGroup()
        {
            Id = Guid.NewGuid().ToString();
            Inputs = NewObservableCollection();
            Geotag = false;
            ForceValidInputs = false;
            ShuffleInputs = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.UI.Inputs.InputGroup"/> class as a copy of another. WARNING:  You must call
        /// UpdateDisplayConditionInputs on the resulting object to ensure that all display conditions are properly set up.
        /// </summary>
        public InputGroup(InputGroup inputGroup, bool newGroupId)
        {
            Id = inputGroup.Id;
            Name = inputGroup.Name;
            Geotag = inputGroup.Geotag;
            ForceValidInputs = inputGroup.ForceValidInputs;
            ShuffleInputs = inputGroup.ShuffleInputs;

            Inputs = JsonConvert.DeserializeObject<ObservableCollection<Input>>(JsonConvert.SerializeObject(inputGroup.Inputs, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

            if (newGroupId)
            {
                Id = Guid.NewGuid().ToString();

                // update all inputs to have the new group ID. because we are creating a new group, all inputs should get new IDs to break
                // all connections with the passed-in group.
                foreach (Input input in Inputs)
                {
                    input.GroupId = Id;
                    input.Id = Guid.NewGuid().ToString();
                }
            }
        }

        [JsonConstructor]
        private InputGroup(string id, ObservableCollection<Input> inputs)
        {
            Id = id;
            Inputs = inputs;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Updates any display conditions contained on any input in this group to reference inputs contained in the passed array.
        /// This is necessary after copying an input group, because we use serialization/deserialization to copy each individual
        /// input, which then breaks the object reference from input display conditions to their target inputs.
        /// </summary>
        /// <param name="inputs">Inputs.</param>
        public void UpdateDisplayConditionInputs(Input[] inputs)
        {
            foreach (Input input in Inputs)
            {
                foreach (InputDisplayCondition displayCondition in input.DisplayConditions)
                {
                    displayCondition.Input = inputs.Single(i => i.Id == displayCondition.Input.Id);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
        #endregion

        #region Private Methods
        private ObservableCollection<Input> NewObservableCollection()
        {
            var collection = new ObservableCollection<Input>();

            //I've tested this and the closure still looks up Id by reference.
            //That means we don't need to update this handler when Id changes. 
            collection.CollectionChanged += CollectionChanged;

            return collection;
        }

        private void CollectionChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Input input in e.NewItems)
                {
                    input.GroupId = Id;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Input input in e.OldItems)
                {
                    input.GroupId = null;
                }
            }
        }
        #endregion
    }
}