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
using Xamarin.Forms;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Sensus.UI.UiProperties;
using Sensus.Exceptions;

namespace Sensus.UI.Inputs
{
    public class ItemPickerPageInput : ItemPickerInput
    {
        private List<object> _items;
        private Dictionary<int, bool> _initialIndexSelected;
        private List<int> _frozenIndices;
        private bool _multiselect;
        private List<object> _selectedItems;
        private string _textBindingPropertyPath;
        private List<Frame> _itemFrames;
        private Label _label;

        public List<object> Items
        {
            get
            {
                return _items;
            }
        }

        /// <summary>
        /// These are the items that the user will have to select from.
        /// </summary>
        /// <value>The string items.</value>
        [EditableListUiProperty("Items:", true, 10, true)]
        [JsonIgnore]
        public List<string> StringItems
        {
            get
            {
                return _items.Select(item => item.ToString()).ToList();
            }
            // need set method so that binding can set the list via the EditableListUiProperty
            set
            {
                _items = value.Cast<object>().ToList();
            }
        }

        public string TextBindingPropertyPath
        {
            get
            {
                return _textBindingPropertyPath;
            }
            set
            {
                _textBindingPropertyPath = value;
            }
        }

        public override object Value
        {
            get
            {
                return _selectedItems;
            }
        }

        /// <summary>
        /// Whether or not to allow the user to select multiple items simultaneously.
        /// </summary>
        /// <value><c>true</c> if multiselect; otherwise, <c>false</c>.</value>
        [OnOffUiProperty(null, true, 11)]
        public bool Multiselect
        {
            get
            {
                return _multiselect;
            }
            set
            {
                _multiselect = value;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _itemFrames.Count == 0 || _itemFrames[0].IsEnabled;
            }
            set
            {
                foreach (Frame itemFrame in _itemFrames)
                {
                    itemFrame.IsEnabled = value;
                }
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Picker (Page)";
            }
        }

        public ItemPickerPageInput()
        {
            Construct();
        }

        public ItemPickerPageInput(string labelText, 
                                   List<object> items, 
                                   Dictionary<int, bool> initialIndexSelected = null, 
                                   List<int> frozenIndices = null,
                                   string textBindingPropertyPath = ".")
            : base(labelText)
        {
            Construct();

            _items = items;
            _initialIndexSelected = initialIndexSelected;
            _frozenIndices = frozenIndices;

            if (!string.IsNullOrWhiteSpace(textBindingPropertyPath))
            {
                _textBindingPropertyPath = textBindingPropertyPath.Trim();
            }
        }

        private void Construct()
        {
            _items = new List<object>();
            _multiselect = false;
            _selectedItems = new List<object>();
            _textBindingPropertyPath = ".";
            _itemFrames = new List<Frame>();
        }

        public override View GetView(int index)
        {
            if (_items.Count == 0)
            {
                return null;
            }

            if (base.GetView(index) == null)
            {
                _selectedItems.Clear();
                _itemFrames.Clear();

                StackLayout itemLabelStack = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Padding = new Thickness(10, 10, 0, 10)
                };

                List<object> itemList = RandomizeItemOrder ? _items.OrderBy(item => Guid.NewGuid()).ToList() : _items;

                // an "other" option only makes sense if the item list contains strings, as that's what the user of the input will assume.
                if (itemList.FirstOrDefault() is string && IncludeOtherOption && !string.IsNullOrWhiteSpace(OtherOptionText) && !itemList.Contains(OtherOptionText))
                {
                    itemList.Add(OtherOptionText);
                }

                for (int i = 0; i < itemList.Count; ++i)
                {
                    object item = itemList[i];

                    Label itemLabel = new Label
                    {
                        FontSize = LabelFontSize,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BindingContext = item

                        // set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
                        , StyleId = Name + " " + i
#endif
                    };

                    // frame the label to indicate selection
                    Frame itemFrame = new Frame
                    {
                        StyleClass = new List<string> { "ItemPickerFrame" },
                        Content = itemLabel,
                        BackgroundColor = Color.Transparent,
                        HasShadow = false,
                        Padding = new Thickness(5)
                    };

                    // needs to be added before taps are registered
                    _itemFrames.Add(itemFrame);

                    itemLabel.SetBinding(Label.TextProperty, _textBindingPropertyPath, stringFormat: "{0}");

                    TapGestureRecognizer tapRecognizer = new TapGestureRecognizer
                    {
                        NumberOfTapsRequired = 1,
                        CommandParameter = i
                    };

                    tapRecognizer.Tapped += async (o, eventArgs) =>
                    {
                        if (!itemLabel.IsEnabled)
                        {
                            return;
                        }

                        // check whether the item is frozen
                        TappedEventArgs tappedEventArgs = eventArgs as TappedEventArgs;
                        int itemIndex = (int)tappedEventArgs.Parameter;
                        bool itemIsFrozen = _frozenIndices?.Contains(itemIndex) ?? false;

                        // bail if user is not allowed to change the item
                        if (itemIsFrozen)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("That item cannot be changed.");
                            return;
                        }

                        ItemTapped(item);
                    };

                    itemLabel.GestureRecognizers.Add(tapRecognizer);

                    // if the item should be initially selected, simulate a user tap. the item's label has not yet been
                    // shown to the user, so this first tap will certainly be the selection tap as opposed to deselection.
                    if (_initialIndexSelected != null && _initialIndexSelected.TryGetValue(i, out bool selected) && selected)
                    {
                        ItemTapped(item);
                    }

                    // add invisible separator between items for fewer tapping errors
                    if (itemLabelStack.Children.Count > 0)
                    {
                        itemLabelStack.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = 5 });
                    }

                    itemLabelStack.Children.Add(itemFrame);
                }

                _label = CreateLabel(index);

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, itemLabelStack }
                });
            }
            else
            {
                _label.Text = GetLabelText(index);  // if the view was already initialized, just update the label since the index might have changed.
            }

            return base.GetView(index);
        }

        private void ItemTapped(object item)
        {
            // update the list of selected items
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
            }
            else
            {
                _selectedItems.Add(item);
            }

            if (!_multiselect)
            {
                _selectedItems.RemoveAll(selectedItem => selectedItem != item);
            }

            // update label background colors according to selected items
            for (int i = 0; i < _itemFrames.Count; ++i)
            {
                Frame itemFrame = _itemFrames[i];

                Color frameBorderColor = Color.Transparent;

                if (_selectedItems.Contains(itemFrame.Content.BindingContext))
                {
                    bool itemIsFrozen = _frozenIndices?.Contains(i) ?? false;

                    if (itemIsFrozen)
                    {
                        frameBorderColor = Color.LightGray;
                    }
                    else
                    {
                        frameBorderColor = Color.Accent;
                    }
                }

                itemFrame.BorderColor = frameBorderColor;
            }

            Complete = (Value as List<object>).Count > 0;
        }

        public override bool ValueMatches(object conditionValue, bool conjunctive)
        {
            // if a list is passed, compare values
            if (conditionValue is List<object>)
            {
                List<object> selectedValueList = Value as List<object>;
                List<object> conditionValueList = conditionValue as List<object>;

                // if the matching condition is conjunctive, then the two lists must be identical.
                if (conjunctive)
                {
                    return selectedValueList.OrderBy(o => o).SequenceEqual(conditionValueList.OrderBy(o => o));
                }
                // if the matching condiction is disjunctive, then any of the condition values may be selected.
                else
                {
                    return conditionValueList.Any(o => selectedValueList.Contains(o));
                }
            }
            else
            {
                SensusException.Report("Called ItemPickerPageInput.ValueMatches with conditionValue that is not a List<object>.");
                return false;
            }
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + (_items.Count + (IncludeOtherOption ? 1 : 0)) + " Items";
        }
    }
}