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

using SensusService;
using SensusService.Exceptions;
using SensusService.Probes;
using System;
using Xamarin.Forms;

namespace SensusUI
{
    public class ProbesPage : ContentPage
    {
        private class ProbeTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (bool)value ? Color.Green : Color.Red;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        private class ProbeDetailValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                Datum mostRecent = value as Datum;
                return mostRecent == null ? "----------" : mostRecent.DisplayDetail + Environment.NewLine + mostRecent.Timestamp;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        public static event EventHandler<Probe> ProbeTapped;

        private Protocol _protocol;
        private ListView _probesList;

        public ProbesPage(Protocol protocol)
        {
            _protocol = protocol;

            _probesList = new ListView();
            _probesList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _probesList.ItemTemplate.SetBinding(TextCell.TextProperty, "DisplayName");
            _probesList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding("Enabled", converter: new ProbeTextColorValueConverter()));
            _probesList.ItemTemplate.SetBinding(TextCell.DetailProperty, new Binding("MostRecentDatum", converter: new ProbeDetailValueConverter()));
            _probesList.ItemTapped += (o, e) =>
                {
                    _probesList.SelectedItem = null;
                    ProbeTapped(this, e.Item as Probe);
                };

            Bind();

            Content = _probesList;
        }

        public void Bind()
        {
            Title = _protocol.Name + "'s Probes";

            _probesList.ItemsSource = null;
            _probesList.ItemsSource = _protocol.Probes;
        }
    }
}
