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
using System.Linq;
using Xamarin.Forms;

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

                Button loadButton = new Button
                {
                    Text = scriptProbe.Script == null ? "Load Script" : scriptProbe.Script.Name,
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                loadButton.Clicked += async (oo, e) =>
                    {
                        scriptProbe.Script = Script.FromJSON(await UiBoundSensusServiceHelper.Get().PromptForAndReadTextFileAsync("Select Script File"));
                        loadButton.Text = scriptProbe.Script == null ? "Load Script" : scriptProbe.Script.Name;
                    };

                Button viewScriptTriggersButton = new Button
                {
                    Text = "View Triggers",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                viewScriptTriggersButton.Clicked += async (o, e) =>
                    {
                        await Navigation.PushAsync(new ScriptTriggersPage(scriptProbe));
                    };

                contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { loadButton, viewScriptTriggersButton }
                });
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
