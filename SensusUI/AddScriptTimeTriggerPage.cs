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

using SensusService;
using SensusService.Probes;
using SensusService.Probes.User;
using SensusService.Probes.User.ProbeTriggerProperties;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Allows the user to add a script trigger to a script runner.
    /// </summary>
    public class AddScriptTimeTriggerPage : ContentPage
    {
        private ScriptRunner _scriptRunner;

        public AddScriptTimeTriggerPage(ScriptRunner scriptRunner)
        {
            _scriptRunner = scriptRunner;

            Title = "Add Time Trigger";

            StackLayout triggerDefinitionLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Padding = new Thickness(10, 10, 10, 10),
                };

            StackLayout contentLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

            Label infoLabel = new Label
                {
                    Text = "If Start Time and End Time are equal, the script will be triggered at that time. If they are not equal, the script will be triggered randomly within the interval between Start Time and End Time.",
                    FontSize = 15
                };

            contentLayout.Children.Add(infoLabel);

            contentLayout.Children.Add(triggerDefinitionLayout);

            TimePicker startTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };
            TimePicker endTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };

            Label startTimeLabel = new Label
                {
                    Text = "Start Time:",
                    FontSize = 20
                };

            startTimePicker.Time = new TimeSpan(8, 0, 0);

            triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { startTimeLabel, startTimePicker }
                });

            Label endTimeLabel = new Label
                {
                    Text = "End Time:",
                    FontSize = 20
                };

            endTimePicker.Time = new TimeSpan(21, 0, 0);

            triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { endTimeLabel, endTimePicker }
                });
            
            Switch rerunSwitch = new Switch();

            Label rerunLabel = new Label
                {
                    Text = "Rerun daily:",
                    FontSize = 20
                };

            rerunSwitch.IsToggled = false;

            triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { rerunLabel, rerunSwitch }
                });

            Button okButton = new Button
                {
                    Text = "OK",
                    FontSize = 20
                };

            okButton.Clicked += async (o, e) =>
                {
                    try
                    {
                        _scriptRunner.TimeTriggers.Add(new SensusService.Probes.User.TimeTrigger(startTimePicker.Time, endTimePicker.Time, rerunSwitch.IsToggled));
                        await Navigation.PopAsync();
                    }
                    catch (Exception ex)
                    {
                        string message = "Failed to add time trigger:  " + ex.Message;
                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync(message);
                        UiBoundSensusServiceHelper.Get(true).Logger.Log(message, LoggingLevel.Normal, GetType());
                    }
                };

            contentLayout.Children.Add(okButton);

            Content = new ScrollView
                {
                    Content = contentLayout
                };
        }
    }
}