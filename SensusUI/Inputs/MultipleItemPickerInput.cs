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
using SensusUI.UiProperties;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SensusUI.Inputs
{
    public class MultipleItemPickerInput : Input
    {
        private string _tipText;
        private string _response;
        private List<string> _optionsList;
        private bool[] _selected;
        Android.App.AlertDialog _dialog;
        Button _makeSelectionButton;
//        Sensus.Android.AndroidMultipleItemPickerInput _multiPicker;

        [EntryStringUiProperty("Tip Text:", true, 10)]
        public string TipText
        {
            get
            {
                return _tipText;
            }
            set
            {
                _tipText = value;
            }
        }

        [EditableListUiProperty(null, true, 11)]
        public List<string> OptionsList
        {
            get
            {
                return _optionsList;
            }
            // need set method so auto-binding can set the list via the EditableListUiProperty
            set
            {
                _optionsList = value;
            }
        }

        public override View View
        {
            get
            {
                if (base.View == null && _optionsList.Count > 0)
                {
//                    // TODO add call to method in android specific multi picker class
//
////                    ListView _selectedView = new ListView();
////                    ListView _optionsView = new ListView();
////
////                    _optionsView.ItemsSource = _optionsList;
////                    _selectedView.ItemsSource = _selectedList;
////                   
////                    _optionsView.ItemTapped += async (sender, e) =>
////                    {
////                        var item = e.Item as string;
////                        if (!_selectedList.Contains(item))
////                            _selectedList.Add(item);
////                        else
////                            _selectedList.Remove(item);
////                    };
//
                    _makeSelectionButton = new Button
                    {
                        Text = "Please Make Selection",
                        FontSize = 17,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = Color.Gray,
                    };

                    _response = "";
                    _selected = new bool[_optionsList.Count];

                    _makeSelectionButton.Clicked += (o, e) =>
                        {
                            _dialog = new Android.App.AlertDialog.Builder(Forms.Context)
                                .SetTitle(_tipText)
                                .SetMultiChoiceItems(_optionsList.ToArray(), null, (ob, ev) =>
                                    {
                                        _selected[ev.Which] = ev.IsChecked;
                                    })
                                .SetPositiveButton("OK", (obj, eve) =>
                                    {
                                        for (int i = 0; i < _selected.Length; i++)
                                            _response += _selected[i].ToString();
                                        Console.Out.WriteLine(_response);
                                    })
                                .SetNegativeButton("Cancel", (obje, even) =>
                                    {
                                    })
                                .Create();
////                            _multiPicker = new Sensus.Android.AndroidMultipleItemPickerInput(_tipText, _optionsList);
////                            _response = "";
////                            _response = _multiPicker.Display();
                            _dialog.Show();
                        };

                    base.View = new StackLayout
                        {
                            Orientation = StackOrientation.Vertical,
                            VerticalOptions = LayoutOptions.Fill,
                            Padding = new Thickness (10, 10, 10, 10),
                            Children = { Label, _makeSelectionButton }
                        };
                }

                return base.View;
            }
        }

        public override object Value
        {
            get
            {
                return _response;
            }
        }

        public override bool Enabled
        {
            get
            {
                return _dialog.IsShowing;
            }
            set
            {
                if (value)
                    _dialog.Show();
                else
                    _dialog.Dismiss();
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Multi-Picker";
            }
        }

        public MultipleItemPickerInput()
        {
            Construct("Please Make Selection", new List<string>());
        }

        public MultipleItemPickerInput(string labelText, string tipText, List<string> items)
            : base(labelText)
        {
            Construct(tipText, items);
        }

        public MultipleItemPickerInput(string name, string labelText, string tipText, List<string> items)
            : base(name, labelText)
        {
            Construct(tipText, items);      
        }

        private void Construct(string tipText, List<string> items)
        {
            _tipText = tipText;
            _optionsList = items;   
        }           

        public override string ToString()
        {
            return base.ToString() + " -- " + _optionsList.Count + " Items";
        }
    }
}