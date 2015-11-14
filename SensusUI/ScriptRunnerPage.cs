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
    /// <summary>
    /// Displays a script runner, allowing the user to edit its prompts and triggers.
    /// </summary>
    public class ScriptRunnerPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ScriptRunnerPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to display.</param>
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

            Button editInputGroupsButton = new Button
            {
                Text = "Edit Input Groups",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editInputGroupsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptInputGroupsPage(scriptRunner.Script));
            };

            contentLayout.Children.Add(editInputGroupsButton);

            Button editTriggersButton = new Button
            {
                Text = "Edit Probe Triggers",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editTriggersButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptTriggersPage(scriptRunner));
            };

            contentLayout.Children.Add(editTriggersButton);

            Button editTimeTriggersButton = new Button
                {
                    Text = "Edit Time Triggers",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

            editTimeTriggersButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new ScriptTimeTriggersPage(scriptRunner));
                };

            contentLayout.Children.Add(editTimeTriggersButton);

            Content = new ScrollView
            { 
                Content = contentLayout
            };
        }
    }
}