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

using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Sensus.Probes;
using Sensus.Anonymization;
using Sensus.UI.UiProperties;
using Sensus.Probes.Location;
using Sensus.Probes.User.Scripts;
using Sensus.Anonymization.Anonymizers;
using Xamarin.Forms;
using Newtonsoft.Json;
using Sensus.UI.Inputs;
using System.Net;
using System;
using System.Threading.Tasks;

namespace Sensus.UI
{
    /// <summary>
    /// Displays a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProbePage"/> class.
        /// </summary>
        /// <param name="probe">Probe to display.</param>
        public ProbePage(Probe probe)
        {
            Title = "Probe";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            string type = "";
            if (probe is ListeningProbe)
            {
                type = "Listening";
            }
            else if (probe is PollingProbe)
            {
                type = "Polling";
            }

            contentLayout.Children.Add(new ContentView
            {
                Content = new Label
                {
                    Text = probe.DisplayName + (type == "" ? "" : " (" + type + ")"),
                    FontSize = 20,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = Color.Accent,
                    HorizontalOptions = LayoutOptions.Center
                },
                Padding = new Thickness(0, 10, 0, 10)
            });

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(probe))
            {
                contentLayout.Children.Add(stack);
            }

            #region script probes
            if (probe is ScriptProbe)
            {
                ScriptProbe scriptProbe = probe as ScriptProbe;

                Button editScriptsButton = new Button
                {
                    Text = "Edit Scripts",
                    FontSize = 20
                };

                contentLayout.Children.Add(editScriptsButton);

                editScriptsButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new ScriptRunnersPage(scriptProbe));
                };

                Button shareScriptButton = new Button
                {
                    Text = "Share Definition",
                    FontSize = 20
                };

                contentLayout.Children.Add(shareScriptButton);

                shareScriptButton.Clicked += async (o, e) =>
                {
                    string sharePath = SensusServiceHelper.Get().GetSharePath(".json");

                    using (StreamWriter shareFile = new StreamWriter(sharePath))
                    {
                        shareFile.WriteLine(JsonConvert.SerializeObject(probe, SensusServiceHelper.JSON_SERIALIZER_SETTINGS));
                    }

                    await SensusServiceHelper.Get().ShareFileAsync(sharePath, "Probe Definition", "application/json");
                };

                Button setAgentButton = new Button
                {
                    Text = "Set Agent" + (scriptProbe.Agent == null ? "" : ":  " + scriptProbe.Agent.Id),
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                setAgentButton.Clicked += async (sender, e) =>
                {
                    await SetAgentButton_Clicked(setAgentButton, scriptProbe);
                };

                contentLayout.Children.Add(setAgentButton);

                Button clearAgentButton = new Button
                {
                    Text = "Clear Agent",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                clearAgentButton.Clicked += async (sender, e) =>
                {
                    if (scriptProbe.Agent != null)
                    {
                        if (await DisplayAlert("Confirm", "Are you sure you wish to clear the survey agent?", "Yes", "No"))
                        {
                            scriptProbe.Agent = null;
                            setAgentButton.Text = "Set Agent";
                        }
                    }

                    await SensusServiceHelper.Get().FlashNotificationAsync("Survey agent cleared.");
                };

                contentLayout.Children.Add(clearAgentButton);
            }
            #endregion

            #region proximity probe
            if (probe is IPointsOfInterestProximityProbe)
            {
                Button editTriggersButton = new Button
                {
                    Text = "Edit Triggers",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                contentLayout.Children.Add(editTriggersButton);

                editTriggersButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new ProximityTriggersPage(probe as IPointsOfInterestProximityProbe));
                };
            }
            #endregion

            #region estimote probe
            if (probe is EstimoteBeaconProbe)
            {
                Button editBeaconsButton = new Button
                {
                    Text = "Edit Beacons",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                contentLayout.Children.Add(editBeaconsButton);

                editBeaconsButton.Clicked += async (sender, e) =>
                {
                    await Navigation.PushAsync(new EstimoteBeaconProbeBeaconsPage(probe as EstimoteBeaconProbe));
                };
            }
            #endregion

            #region anonymization
            List<PropertyInfo> anonymizableProperties = probe.DatumType.GetProperties().Where(property => property.GetCustomAttribute<Anonymizable>() != null).ToList();

            if (anonymizableProperties.Count > 0)
            {
                contentLayout.Children.Add(new Label
                {
                    Text = "Anonymization",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = Color.Accent,
                    HorizontalOptions = LayoutOptions.Center
                });

                List<StackLayout> anonymizablePropertyStacks = new List<StackLayout>();

                foreach (PropertyInfo anonymizableProperty in anonymizableProperties)
                {
                    Anonymizable anonymizableAttribute = anonymizableProperty.GetCustomAttribute<Anonymizable>(true);

                    Label propertyLabel = new Label
                    {
                        Text = anonymizableAttribute.PropertyDisplayName ?? anonymizableProperty.Name + ":",
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalTextAlignment = TextAlignment.Center
                    };

                    // populate a picker of anonymizers for the current property
                    Picker anonymizerPicker = new Picker
                    {
                        Title = "Select Anonymizer",
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    anonymizerPicker.Items.Add("Do Not Anonymize");
                    foreach (Anonymizer anonymizer in anonymizableAttribute.AvailableAnonymizers)
                    {
                        anonymizerPicker.Items.Add(anonymizer.DisplayText);
                    }

                    anonymizerPicker.SelectedIndexChanged += (o, e) =>
                    {
                        Anonymizer selectedAnonymizer = null;
                        if (anonymizerPicker.SelectedIndex > 0)
                        {
                            selectedAnonymizer = anonymizableAttribute.AvailableAnonymizers[anonymizerPicker.SelectedIndex - 1];  // subtract one from the selected index since the JsonAnonymizer's collection of anonymizers start after the "None" option within the picker.
                        }

                        probe.Protocol.JsonAnonymizer.SetAnonymizer(anonymizableProperty, selectedAnonymizer);
                    };

                    // set the picker's index to the current anonymizer (or "Do Not Anonymize" if there is no current)
                    Anonymizer currentAnonymizer = probe.Protocol.JsonAnonymizer.GetAnonymizer(anonymizableProperty);
                    int currentIndex = 0;
                    if (currentAnonymizer != null)
                    {
                        currentIndex = anonymizableAttribute.AvailableAnonymizers.IndexOf(currentAnonymizer) + 1;
                    }

                    anonymizerPicker.SelectedIndex = currentIndex;

                    StackLayout anonymizablePropertyStack = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { propertyLabel, anonymizerPicker }
                    };

                    anonymizablePropertyStacks.Add(anonymizablePropertyStack);
                }

                foreach (StackLayout anonymizablePropertyStack in anonymizablePropertyStacks.OrderBy(s => (s.Children[0] as Label).Text))
                {
                    contentLayout.Children.Add(anonymizablePropertyStack);
                }
            }
            #endregion

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        private async Task SetAgentButton_Clicked(Button setAgentButton, ScriptProbe scriptProbe)
        {
            List<Input> agentSelectionInputs = new List<Input>();

            // show any existing agents
            List<IScriptProbeAgent> currentAgents = null;

#if __ANDROID__
            // try to extract agents from a previously loaded assembly
            try
            {
              currentAgents = ScriptProbe.GetAgents(scriptProbe.AgentAssemblyBytes);
            }
            catch (Exception)
            { }
#elif __IOS__
            currentAgents = ScriptProbe.GetAgents();

            if (currentAgents.Count == 0)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("No agents available.");
                return;
            }
#endif

            ItemPickerPageInput currentAgentsPicker = null;
            if (currentAgents != null && currentAgents.Count > 0)
            {
                currentAgentsPicker = new ItemPickerPageInput("Available agent" + (currentAgents.Count > 1 ? "s" : "") + ":", currentAgents.Cast<object>().ToList(), ".")
                {
                    Required = false
                };

                agentSelectionInputs.Add(currentAgentsPicker);
            }

#if __ANDROID__
            // add option to scan qr code to import a new assembly
            QrCodeInput agentAssemblyUrlQrCodeInput = new QrCodeInput(QrCodePrefix.SURVEY_AGENT, "URL:", false, "Agent URL:")
            {
                Required = false
            };

            agentSelectionInputs.Add(agentAssemblyUrlQrCodeInput);
#endif

            List<Input> completedInputs = await SensusServiceHelper.Get().PromptForInputsAsync("Survey Agent", agentSelectionInputs, null, true, "Set", null, null, null, false);

            if (completedInputs == null)
            {
                return;
            }

            // check for QR code on android
            string agentURL = null;

#if __ANDROID__
            agentURL = agentAssemblyUrlQrCodeInput.Value?.ToString();
#endif

            // if there is no URL, check if the user has selected an agent.
            if (string.IsNullOrWhiteSpace(agentURL))
            {
                if (currentAgentsPicker != null)
                {
                    IScriptProbeAgent selectedAgent = (currentAgentsPicker.Value as List<object>).FirstOrDefault() as IScriptProbeAgent;

                    // set the selected agent, watching out for a null (clearing) selection that needs to be confirmed
                    if (selectedAgent != null || await DisplayAlert("Confirm", "Are you sure you wish to clear the survey agent?", "Yes", "No"))
                    {
                        scriptProbe.Agent = selectedAgent;

                        setAgentButton.Text = "Set Agent" + (scriptProbe.Agent == null ? "" : ":  " + scriptProbe.Agent.Id);

                        if (scriptProbe.Agent == null)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Survey agent cleared.");
                        }
                    }
                }
            }
#if __ANDROID__
            else
            {
                // download agent assembly from scanned QR code
                byte[] downloadedBytes = null;
                string downloadErrorMessage = null;
                try
                {
                    // download the assembly and extract agents
                    downloadedBytes = scriptProbe.AgentAssemblyBytes = await new WebClient().DownloadDataTaskAsync(new Uri(agentURL));
                    List<IScriptProbeAgent> qrCodeAgents = ScriptProbe.GetAgents(downloadedBytes);

                    if (qrCodeAgents.Count == 0)
                    {
                        throw new Exception("No agents were present in the specified file.");
                    }
                }
                catch (Exception ex)
                {
                    downloadErrorMessage = ex.Message;
                }

                // if error message is null, then we have 1 or more agents in the downloaded assembly.
                if (downloadErrorMessage == null)
                {
                    // redisplay the current input prompt including the agents we just downloaded
                    scriptProbe.AgentAssemblyBytes = downloadedBytes;
                    await SetAgentButton_Clicked(setAgentButton, scriptProbe);
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log(downloadErrorMessage, LoggingLevel.Normal, typeof(Protocol));
                    await SensusServiceHelper.Get().FlashNotificationAsync(downloadErrorMessage);
                }
            }
#endif
        }
    }
}