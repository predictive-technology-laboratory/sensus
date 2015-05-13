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
using System.Reflection;
using System.Linq;
using SensusService;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.Location;

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

            #region script probes
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

                editScriptButton.Clicked += async (o, e) =>
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
                        HorizontalOptions = LayoutOptions.Start
                    };
                       
                    // populate a picker of anonymizers for the current property
                    Picker anonymizerPicker = new Picker
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };
                
                    anonymizerPicker.Items.Add("Do Not Anonymize");
                    foreach (Anonymizer anonymizer in anonymizableAttribute.AvailableAnonymizers)
                        anonymizerPicker.Items.Add(anonymizer.DisplayText);

                    anonymizerPicker.SelectedIndexChanged += (o, e) =>
                        {
                            Anonymizer selectedAnonymizer = null;
                            if (anonymizerPicker.SelectedIndex > 0)
                                selectedAnonymizer = anonymizableAttribute.AvailableAnonymizers[anonymizerPicker.SelectedIndex - 1];  // subtract one from the selected index since the JsonAnonymizer's collection of anonymizers start after the "None" option within the picker.

                            probe.Protocol.JsonAnonymizer.SetAnonymizer(anonymizableProperty, selectedAnonymizer);
                        };
                    
                    // set the picker's index to the current anonymizer (or "Do Not Anonymize" if there is no current)
                    Anonymizer currentAnonymizer = probe.Protocol.JsonAnonymizer.GetAnonymizer(anonymizableProperty);
                    int currentIndex = 0;
                    if(currentAnonymizer != null)
                        currentIndex = anonymizableAttribute.AvailableAnonymizers.IndexOf(currentAnonymizer) + 1;
                    
                    anonymizerPicker.SelectedIndex = currentIndex;

                    StackLayout anonymizablePropertyStack = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { propertyLabel, anonymizerPicker }
                    };

                    anonymizablePropertyStacks.Add(anonymizablePropertyStack);
                }

                foreach(StackLayout anonymizablePropertyStack in anonymizablePropertyStacks.OrderBy(s => (s.Children[0] as Label).Text))
                    contentLayout.Children.Add(anonymizablePropertyStack);
            }
            #endregion

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}