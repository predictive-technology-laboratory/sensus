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
using Sensus.Extensions;

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
            Id     = Guid.NewGuid().ToString();
            Inputs = NewObservableCollection();
            Geotag = false;
        }

        public InputGroup(InputGroup old)
        {
            Id     = old.Id;
            Name   = old.Name;
            Geotag = old.Geotag;

            Inputs   = JsonConvert.DeserializeObject<ObservableCollection<Input>>(JsonConvert.SerializeObject(old.Inputs, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
            //Inputs = old.Inputs.Select(i => i.Copy()).ToObservableCollection(CollectionChanged);
        }

        [JsonConstructor]
        private InputGroup(string id, ObservableCollection<Input> inputs)
        {
            Id = id;
            Inputs = inputs;
        }
        #endregion

        #region Public Methods
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