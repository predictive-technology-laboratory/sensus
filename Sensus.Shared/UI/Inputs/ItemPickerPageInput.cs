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
using Sensus;
using Sensus.Probes;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using Sensus.Exceptions;
using Sensus.UI.Inputs;
using System.Linq;
using System.Globalization;
using Sensus.UI.UiProperties;
using Xamarin;

namespace Sensus.UI.Inputs
{
    public class ItemPickerPageInput : ItemPickerInput
    {
        private List<object> _items;
        private bool _multiselect;
        private List<object> _selectedItems;
        private string _textBindingPropertyPath;
        private List<Label> _itemLabels;
        private Label _label;

        public List<object> Items
        {
            get
            {
                return _items;
            }
        }

        /// <summary>
        /// Provides a means of setting the items via a UI property (e.g., when configuring this input for a script).
        /// </summary>
        /// <value>The string items.</value>
        [EditableListUiProperty("Items:", true, 10)]
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
                return _itemLabels.Count == 0 ? true : _itemLabels[0].IsEnabled;
            }
            set
            {
                foreach (Label itemLabel in _itemLabels)
                    itemLabel.IsEnabled = value;
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

        public ItemPickerPageInput(string labelText, List<object> items, string textBindingPropertyPath = ".")
            : base(labelText)
        {
            Construct();

            _items = items;

            if (!string.IsNullOrWhiteSpace(textBindingPropertyPath))
                _textBindingPropertyPath = textBindingPropertyPath.Trim();
        }

        private void Construct()
        {
            _items = new List<object>();
            _multiselect = false;
            _selectedItems = new List<object>();
            _textBindingPropertyPath = ".";
            _itemLabels = new List<Label>();
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {       
                _selectedItems.Clear();
                _itemLabels.Clear();

                StackLayout itemLabelStack = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Padding = new Thickness(30, 10, 0, 10)
                };

                List<object> itemList = RandomizeItemOrder ? _items.OrderBy(item => Guid.NewGuid()).ToList() : _items;

                for (int i = 0; i < itemList.Count; ++i)
                {
                    object item = itemList[i];

                    Label itemLabel = new Label
                    {
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BindingContext = item
                                
                        // set the style ID on the view so that we can retrieve it when unit testing
                        #if UNIT_TESTING
                        , StyleId = Name + " " + i
                        #endif
                    };

                    _itemLabels.Add(itemLabel);

                    itemLabel.SetBinding(Label.TextProperty, _textBindingPropertyPath, stringFormat: "{0}");

                    TapGestureRecognizer tapRecognizer = new TapGestureRecognizer
                    {
                        NumberOfTapsRequired = 1
                    };

                    Color defaultBackgroundColor = itemLabel.BackgroundColor;

                    tapRecognizer.Tapped += (o, e) =>
                    {
                        if (!itemLabel.IsEnabled)
                            return;
                            
                        if (_selectedItems.Contains(item))
                            _selectedItems.Remove(item);
                        else
                            _selectedItems.Add(item);

                        if (!_multiselect)
                            _selectedItems.RemoveAll(selectedItem => selectedItem != item);

                        foreach (Label label in _itemLabels)
                            label.BackgroundColor = _selectedItems.Contains(label.BindingContext) ? Color.Accent : defaultBackgroundColor;

                        Complete = (Value as List<object>).Count > 0;
                    };
                    
                    itemLabel.GestureRecognizers.Add(tapRecognizer);

                    // add invisible separator between items for fewer tapping errors
                    if (itemLabelStack.Children.Count > 0)
                        itemLabelStack.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = 5 });
                    
                    itemLabelStack.Children.Add(itemLabel);
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
                _label.Text = GetLabelText(index);  // if the view was already initialized, just update the label since the index might have changed.

            return base.GetView(index);
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
                    return selectedValueList.OrderBy(o => o).SequenceEqual(conditionValueList.OrderBy(o => o));
                // if the matching condiction is disjunctive, then any of the condition values may be selected.
                else
                    return conditionValueList.Any(o => selectedValueList.Contains(o));
            }
            else
            {
                try
                {
                    Insights.Report(new Exception("Called ItemPickerPageInput.ValueMatches with conditionValue that is not a List<object>."), Insights.Severity.Critical);
                }
                catch (Exception)
                {
                }

                return false;
            }
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + _items.Count + " Items";
        }
    }
}