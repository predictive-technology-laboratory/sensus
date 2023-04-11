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
using Sensus.Probes;
using Sensus.Exceptions;
using Xamarin.Forms;
using System.Collections.ObjectModel;

namespace Sensus.UI
{
	public abstract class ProbesPage : ContentPage
	{
		private class ProbeTextColorValueConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				Application.Current.Resources.TryGetValue("ProbeOnColor", out object onColor);
				Application.Current.Resources.TryGetValue("ProbeOffColor", out object offColor);

				return (bool)value ? (onColor ?? Color.Green) : (offColor ?? Color.Red);
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
			_probesList.ItemTemplate = new DataTemplate(typeof(DarkModeCompatibleTextCell));
			_probesList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Probe.Caption));
			_probesList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(nameof(Probe.Enabled), converter: new ProbeTextColorValueConverter()));
			_probesList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Probe.SubCaption));
			_probesList.ItemsSource = new ObservableCollection<Probe>(_protocol.Probes);
			_probesList.ItemTapped += ProbeTappedAsync;

			Content = _probesList;
		}

		protected abstract void ProbeTappedAsync(object sender, ItemTappedEventArgs e);
	}
}
