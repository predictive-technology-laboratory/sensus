#region copyright
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
#endregion

using SensusService.Probes;
using SensusService.Probes.User;
using SensusUI.UiProperties;
using System;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Displays properties for a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        public static event EventHandler<IScriptProbe> AddTriggerTapped;

        public ProbePage(Probe probe)
        {
            BindingContext = probe;

            SetBinding(TitleProperty, new Binding("DisplayName"));

            StackLayout contentLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(probe))
                contentLayout.Children.Add(stack);

            if (probe is IScriptProbe)
            {
                IScriptProbe scriptProbe = probe as IScriptProbe;

                ListView triggerList = new ListView();
                triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
                triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, ".");
                triggerList.ItemsSource = scriptProbe.Triggers;
                contentLayout.Children.Add(triggerList);

                Button addTriggerButton = new Button
                {
                    Text = "Add Trigger",
                    Font = Font.SystemFontOfSize(20)
                };

                addTriggerButton.Clicked += (o, e) =>
                    {
                        if (AddTriggerTapped != null)
                            AddTriggerTapped(o, scriptProbe);
                    };

                contentLayout.Children.Add(addTriggerButton);

                Button deleteTriggerButton = new Button
                {
                    Text = "Delete Trigger",
                    Font = Font.SystemFontOfSize(20)
                };

                deleteTriggerButton.Clicked += async (o, e) =>
                    {
                        if (triggerList.SelectedItem != null && await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected trigger?", "OK", "Cancel"))
                            scriptProbe.RemoveTrigger(probe, triggerList.SelectedItem as string);
                    };

                contentLayout.Children.Add(deleteTriggerButton);
            }

            Content = contentLayout;
        }
    }
}
