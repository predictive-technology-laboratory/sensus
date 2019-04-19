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
using System.Globalization;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class SensingAgentPage : ContentPage
    {
        private class SensingAgentStateColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                SensingAgentState state = (SensingAgentState)value;
                SensingAgentState label = (SensingAgentState)parameter;
                return state == label ? Color.Green : Color.Gray;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public SensingAgentPage(SensingAgent sensingAgent)
        {
            Label idleLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Text = SensingAgentState.Idle.ToString(),
                BindingContext = sensingAgent
            };

            idleLabel.SetBinding(BackgroundColorProperty, new Binding(nameof(SensingAgent.State), converter: new SensingAgentStateColorConverter(), converterParameter: SensingAgentState.Idle));

            Label directObservationLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Text = SensingAgentState.ActiveObservation.ToString(),
                BindingContext = sensingAgent
            };

            directObservationLabel.SetBinding(BackgroundColorProperty, new Binding(nameof(SensingAgent.State), converter: new SensingAgentStateColorConverter(), converterParameter: SensingAgentState.ActiveObservation));

            Label directControlLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Text = SensingAgentState.ActiveControl.ToString(),
                BindingContext = sensingAgent
            };

            directControlLabel.SetBinding(BackgroundColorProperty, new Binding(nameof(SensingAgent.State), converter: new SensingAgentStateColorConverter(), converterParameter: SensingAgentState.ActiveControl));

            Label opportunisticControlLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Text = SensingAgentState.OpportunisticControl.ToString(),
                BindingContext = sensingAgent
            };

            opportunisticControlLabel.SetBinding(BackgroundColorProperty, new Binding(nameof(SensingAgent.State), converter: new SensingAgentStateColorConverter(), converterParameter: SensingAgentState.OpportunisticControl));

            Label endingControlLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Text = SensingAgentState.EndingControl.ToString(),
                BindingContext = sensingAgent
            };

            endingControlLabel.SetBinding(BackgroundColorProperty, new Binding(nameof(SensingAgent.State), converter: new SensingAgentStateColorConverter(), converterParameter: SensingAgentState.EndingControl));

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children =
                    {
                        idleLabel,
                        directObservationLabel,
                        directControlLabel,
                        opportunisticControlLabel,
                        endingControlLabel
                    }
                }
            };
        }
    }
}
