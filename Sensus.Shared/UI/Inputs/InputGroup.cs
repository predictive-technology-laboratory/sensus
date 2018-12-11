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
using System.Collections.Specialized;
using Sensus.UI.UiProperties;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Sensus.UI.Inputs
{
    public class InputGroup : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;

        public string Id { get; set; }
        public ObservableCollection<Input> Inputs { get; }

        /// <summary>
        /// Name of the input group.
        /// </summary>
        /// <value>The name.</value>
        [EntryStringUiProperty(null, true, 0, true)]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        /// <summary>
        /// Whether or not to tag inputs in this group with the device's current GPS location.
        /// </summary>
        /// <value><c>true</c> if geotag; otherwise, <c>false</c>.</value>
        [OnOffUiProperty(null, true, 1)]
        public bool Geotag { get; set; }

        /// <summary>
        /// Whether or not to force valid input values (e.g., all required fields completed, etc.)
        /// before allowing the user to move to the next input group.
        /// </summary>
        /// <value><c>true</c> if force valid inputs; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Force Valid Inputs:", true, 2)]
        public bool ForceValidInputs { get; set; }

        /// <summary>
        /// Whether or not to randomly shuffle the inputs in this group when displaying them to the user.
        /// </summary>
        /// <value><c>true</c> if shuffle inputs; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Shuffle Inputs:", true, 3)]
        public bool ShuffleInputs { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="InputGroup"/> is valid.
        /// A valid input group is one in which each <see cref="Input"/> in the group is valid.
        /// An input group with no inputs is deemed valid by default.
        /// </summary>
        [JsonIgnore]
        public bool Valid => Inputs.All(input => input?.Valid ?? true);

        public InputGroup()
        {
            Id = Guid.NewGuid().ToString();
            Inputs = NewObservableCollection();
            Geotag = false;
            ForceValidInputs = false;
            ShuffleInputs = false;
        }

        private ObservableCollection<Input> NewObservableCollection()
        {
            ObservableCollection<Input> collection = new ObservableCollection<Input>();

            // I've tested this and the closure still looks up Id by reference.
            // That means we don't need to update this handler when Id changes. 
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

        /// <summary>
        /// Creates a copy of this <see cref="InputGroup"/>.
        /// </summary>
        /// <returns>The copy.</returns>
        /// <param name="newId">If set to <c>true</c>, set new random <see cref="Id"/> and <see cref="Input.Id"/> values for the current group
        /// and <see cref="Input"/>s associated with this <see cref="InputGroup"/>.</param>
        public InputGroup Copy(bool newId)
        {
            InputGroup copy = JsonConvert.DeserializeObject<InputGroup>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

            if (newId)
            {
                copy.Id = Guid.NewGuid().ToString();

                // update all inputs to have the new group ID and new IDs themselves.
                foreach (Input input in copy.Inputs)
                {
                    input.GroupId = copy.Id;
                    input.Id = Guid.NewGuid().ToString();
                }
            }

            return copy;
        }
    }
}
