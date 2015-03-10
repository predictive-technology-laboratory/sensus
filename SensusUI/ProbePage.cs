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

using SensusService.Probes;
using SensusService.Probes.User;
using SensusUI.UiProperties;
using Xamarin.Forms;
using System.Collections.Generic;
using System;

namespace SensusUI
{
    /// <summary>
    /// Displays properties for a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        public ProbePage(Probe probe)
        {
            Title = "Probe";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(probe))
                contentLayout.Children.Add(stack);

            if (probe is ScriptProbe)
            {
                ScriptProbe scriptProbe = probe as ScriptProbe;

                Button editScriptButton = new Button
                {
                    Text = "Edit Script",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                contentLayout.Children.Add(editScriptButton);

                editScriptButton.Clicked += async (oo, e) =>
                    {
                        await Navigation.PushAsync(new ScriptPage(scriptProbe.Script));
                    };

                Button viewScriptTriggersButton = new Button
                {
                    Text = "Edit Triggers",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                contentLayout.Children.Add(viewScriptTriggersButton);

                viewScriptTriggersButton.Clicked += async (o, e) =>
                    {
                        await Navigation.PushAsync(new ScriptTriggersPage(scriptProbe));
                    };
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
