//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Sensus.Probes;
using Sensus.Exceptions;
using Xamarin.Forms;

namespace Sensus.UI
{
    public abstract class ProbesPage : ContentPage
    {
        private class ProbeTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (bool)value ? Color.Green : Color.Red;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                SensusException.Report("Invalid call to " + GetType().FullName + ".ConvertBack.");
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

            _probesList = new ListView(ListViewCachingStrategy.RecycleElement);
            _probesList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _probesList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Probe.Caption));
            _probesList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(nameof(Probe.Enabled), converter: new ProbeTextColorValueConverter()));
            _probesList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Probe.SubCaption));
            _probesList.ItemsSource = _protocol.Probes;
            _probesList.ItemTapped += ProbeTapped;

            Content = _probesList;
        }

        protected abstract void ProbeTapped(object sender, ItemTappedEventArgs e);
    }
}
