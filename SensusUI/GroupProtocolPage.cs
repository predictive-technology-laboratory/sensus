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
using Xamarin.Forms;
using System.Collections.Generic;
using SensusService;
using SensusService.Probes;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using SensusService.Exceptions;
using SensusUI.Inputs;
using System.Linq;

namespace SensusUI
{
    public class GroupProtocolPage : ContentPage
    {
        private class ProtocolTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object selectedProtocols, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return Color.Gray;
                
                Protocol protocol = value as Protocol;
                List<Protocol> protocols = selectedProtocols as List<Protocol>;

                return protocols.Contains(protocol) ? Color.Accent : Color.Gray;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        public GroupProtocolPage(Protocol protocol)
        {   
            Title = "Select Protocols To Group";

            List<Protocol> selectedProtocols = new List<Protocol>();

            // get other protocols that are groupable and haven't been grouped
            List<Protocol> protocols = UiBoundSensusServiceHelper.Get(true).RegisteredProtocols.Where(registeredProtocol => registeredProtocol != protocol && registeredProtocol.Groupable && registeredProtocol.GroupedProtocols.Count == 0).ToList();

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            protocolsList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(".", converter: new ProtocolTextColorValueConverter(), converterParameter: selectedProtocols));
            protocolsList.ItemsSource = protocols;

            protocolsList.ItemTapped += (o, e) =>
            {
                Protocol selectedProtocol = e.Item as Protocol;

                if (selectedProtocols.Contains(selectedProtocol))
                    selectedProtocols.Remove(selectedProtocol);
                else
                    selectedProtocols.Add(selectedProtocol);

                protocolsList.ItemsSource = null;
                protocolsList.ItemsSource = protocols;
            };

            ToolbarItems.Add(new ToolbarItem("OK", null, async () =>
                    {
                        if (selectedProtocols.Count == 0)
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("No protocols grouped.");
                        else
                            protocol.GroupedProtocols.AddRange(selectedProtocols);

                        await Navigation.PopAsync();
                    }));

            Content = protocolsList;
        }
    }
}