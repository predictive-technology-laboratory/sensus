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
using System.ComponentModel;

namespace Sensus.UI.Inputs
{
	public class InputGroup : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private string _name;
		private string _description;

		public string Id { get; set; }
		public ObservableCollection<Input> Inputs { get; }

		public virtual bool HasInputs => Inputs.Any();

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
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListItemText)));
				}
			}
		}

		/// <summary>
		/// A short description of the <c>InputGroup</c> to distinguish it from <c>InputGroup</c>s with the same name.
		/// </summary>
		/// <value>The description.</value>
		[EntryStringUiProperty(null, true, 1, true)]
		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				if (value != _name)
				{
					_description = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListItemText)));
				}
			}
		}

		[JsonIgnore]
		public string ListItemText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Description))
				{
					return Name;
				}

				return $"{Name} ({Description})";
			}
		}

		/// <summary>
		/// Whether or not to display the built in navigation buttons. If set to true, 
		/// the input group page is responsible for providing a mechanism to navigate
		/// to the next and/or previous page.
		/// </summary>
		/// <value><c>true</c> to hide the navigation buttons; otherwise, <c>false</c>.</value>
		[ListUiProperty("Show Navigation:", true, 2, new object[] { ShowNavigationOptions.Always, ShowNavigationOptions.WhenValid, ShowNavigationOptions.WhenComplete, ShowNavigationOptions.WhenCorrect, ShowNavigationOptions.Never }, false)]
		public ShowNavigationOptions ShowNavigationButtons { get; set; }

		[OnOffUiProperty("Prevent Navigation Backward:", true, 3)]
		public bool HidePreviousButton { get; set; }

		/// <summary>
		/// Whether or not to tag inputs in this group with the device's current GPS location.
		/// </summary>
		/// <value><c>true</c> if geotag; otherwise, <c>false</c>.</value>
		[OnOffUiProperty(null, true, 3)]
		public bool Geotag { get; set; }

		/// <summary>
		/// Whether or not to force valid input values (e.g., all required fields completed, etc.)
		/// before allowing the user to move to the next input group.
		/// </summary>
		/// <value><c>true</c> if force valid inputs; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Force Valid Inputs:", true, 4)]
		public bool ForceValidInputs { get; set; }

		/// <summary>
		/// Whether or not to randomly shuffle the inputs in this group when displaying them to the user.
		/// </summary>
		/// <value><c>true</c> if shuffle inputs; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Shuffle Inputs:", true, 5)]
		public bool ShuffleInputs { get; set; }

		[OnOffUiProperty("Hide Title:", true, 6)]
		public bool HideTitle { get; set; }

		/// <summary>
		/// Override the text for the Previous button.
		/// </summary>
		[EntryStringUiProperty("Previous Button Text:", true, 7, false)]
		public virtual string PreviousButtonText { get; set; }

		/// <summary>
		/// Override the text for the Next button.
		/// </summary>
		[EntryStringUiProperty("Next Button Text:", true, 8, false)]
		public virtual string NextButtonText { get; set; }

		/// <summary>
		/// Override the text for the Submit button.
		/// </summary>
		[EntryStringUiProperty("Submit Button Text:", true, 9, false)]
		public virtual string SubmitButtonText { get; set; }

		/// <summary>
		/// Override the text for the Cancel button.
		/// </summary>
		[EntryStringUiProperty("Cancel Button Text:", true, 10, false)]
		public virtual string CancelButtonText { get; set; }

		[OnOffUiProperty("Hide Required Field Label:", true, 11)]
		public bool HideRequiredFieldLabel { get; set; }

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