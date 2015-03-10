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
using SensusService.Probes.User;
using SensusUI.UiProperties;
using System.Collections.Generic;

namespace SensusUI
{
    public class ScriptPage : ContentPage
    {
        public ScriptPage(Script script)
        {
            Title = "Script";

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(script);

            Button promptsButton = new Button
            {
                Text = "Edit Prompts",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            promptsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new PromptsPage(script.Prompts));
            };

            stacks.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { promptsButton }
                });

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in stacks)
                contentLayout.Children.Add(stack);

            Content = new ScrollView
            { 
                Content = contentLayout
            };
        }
    }
}


