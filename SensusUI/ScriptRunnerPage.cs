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
    public class ScriptRunnerPage : ContentPage
    {
        public ScriptRunnerPage(ScriptRunner scriptRunner)
        {
            Title = "Script";                  

            StackLayout contentLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(scriptRunner))
                contentLayout.Children.Add(stack);

            Button editPromptsButton = new Button
            {
                Text = "Edit Prompts",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editPromptsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new PromptsPage(scriptRunner.Script.Prompts));
            };

            contentLayout.Children.Add(editPromptsButton);

            Button editTriggersButton = new Button
            {
                Text = "Edit Triggers",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editTriggersButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptTriggersPage(scriptRunner));
            };

            contentLayout.Children.Add(editTriggersButton);

            Content = new ScrollView
            { 
                Content = contentLayout
            };
        }
    }
}