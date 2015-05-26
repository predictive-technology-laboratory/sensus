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
using System.Collections.ObjectModel;
using SensusService.Probes.User;
using Xamarin.Forms;

namespace SensusUI
{
    public class ScriptsPage : ContentPage
    {
        public ScriptsPage(ScriptProbe probe)
        {
            Title = "Scripts";

            ListView scriptRunnersList = new ListView();
            scriptRunnersList.ItemTemplate = new DataTemplate(typeof(TextCell));
            scriptRunnersList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            scriptRunnersList.ItemsSource = probe.ScriptRunners;

            Content = scriptRunnersList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        probe.ScriptRunners.Add(new ScriptRunner("New Script", new Script(), 1, probe, 0));
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", () =>
                    {
                        if (scriptRunnersList.SelectedItem != null)
                        {
                            ScriptRunner scriptRunner = scriptRunnersList.SelectedItem as ScriptRunner;
                            scriptRunner.Stop();
                            probe.ScriptRunners.Remove(scriptRunner);
                        }
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "pencil.png", async () =>
                    {
                        if (scriptRunnersList.SelectedItem != null)
                            await Navigation.PushAsync(new ScriptRunnerPage(scriptRunnersList.SelectedItem as ScriptRunner));
                    }));
        }
    }
}