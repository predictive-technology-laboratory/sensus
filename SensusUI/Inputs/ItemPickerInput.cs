﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

namespace SensusUI.Inputs
{
    public class ItemPickerInput : Input
    {
        public ItemPickerInput(string label, string tipText, string[] items)
            : base(label)
        {
            Picker picker = new Picker
            {
                Title = tipText,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            foreach (string item in items)
                picker.Items.Add(item);

            View = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { Label, picker }
            };

            ValueRetriever = new Func<object>(() => picker.SelectedIndex >= 0 ? picker.Items[picker.SelectedIndex] : null);
        }
    }
}