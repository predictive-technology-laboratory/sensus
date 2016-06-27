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
using SensusService.Exceptions;
using SensusService.Probes;
using System;
using Xamarin.Forms;

namespace SensusUI
{
    public abstract class ProbesPage : ContentPage
    {
        private class ProbeTextValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return "";

                Probe probe = value as Probe;

                string type = "";
                if (probe is ListeningProbe)
                    type = "Listening";
                else if (probe is PollingProbe)
                    type = "Polling";

                return probe.DisplayName + (type == "" ? "" : " (" + type + ")");
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        private class ProbeTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return Color.Default;

                Probe probe = value as Probe;
                return probe.Enabled ? Color.Green : Color.Red;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        private class ProbeDetailValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return "";

                Probe probe = value as Probe;
                Datum mostRecentDatum = probe.MostRecentDatum;
                return mostRecentDatum == null ? "----------" : mostRecentDatum.DisplayDetail + Environment.NewLine + mostRecentDatum.Timestamp;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        private Protocol _protocol;
        private ListView _probesList;

        protected Protocol Protocol
        {
            get
            {
                return _protocol;
            }
        }

        protected ListView ProbesList
        {
            get
            {
                return _probesList;
            }
        }

        public ProbesPage(Protocol protocol, string title)
        {
            _protocol = protocol;

            Title = title;

            _probesList = new ListView
            {
                IsPullToRefreshEnabled = true
            };

            _probesList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _probesList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", converter: new ProbeTextValueConverter()));
            _probesList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(".", converter: new ProbeTextColorValueConverter()));
            _probesList.ItemTemplate.SetBinding(TextCell.DetailProperty, new Binding(".", converter: new ProbeDetailValueConverter()));
            _probesList.ItemTapped += ProbeTapped;

            _probesList.Refreshing += (o, e) =>
            {
                Bind();
                _probesList.IsRefreshing = false;
            };

            Bind();

            Content = _probesList;
        }

        protected abstract void ProbeTapped(object sender, ItemTappedEventArgs e);

        public void Bind()
        {
            _probesList.ItemsSource = null;
            _probesList.ItemsSource = _protocol.Probes;
        }
    }
}