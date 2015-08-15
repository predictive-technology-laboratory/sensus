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

            StackLayout contentLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

            StackLayout triggerDefinitionLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Padding = new Thickness(10, 10, 10, 10),
                };

            contentLayout.Children.Add(triggerDefinitionLayout);

            TimePicker startTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };
            TimePicker endTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };

            #region start/end times
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
            #endregion

            Label explanation = new Label
                {
                    Text = "If Start Time and end Time are equal, the script will be triggered at that time. If they are not equal, the script will be triggered randomly within the interval between Start Time and End Time.",
                    FontSize = 15
                };

            triggerDefinitionLayout.Children.Add(explanation);

            Button okButton = new Button
                {
                    Text = "OK",
                    FontSize = 20
                };

            okButton.Clicked += async (o, e) =>
                {
                    try
                    {
                        _scriptRunner.TimeTriggers.Add(new SensusService.Probes.User.TimeTrigger(startTimePicker.Time, endTimePicker.Time));
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