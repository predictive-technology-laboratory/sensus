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
using SensusUI.UiProperties;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;

namespace SensusUI
{
    /// <summary>
    /// First thing the user sees.
    /// </summary>
    public class SensusMainPage : ContentPage
    {
        private List<StackLayout> _serviceHelperStacks;

        public SensusMainPage()
        {
            Title = "Sensus";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Button protocolsButton = new Button
            {
                Text = "Protocols",
                FontSize = 20
            };

            protocolsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ProtocolsPage());
            };

            contentLayout.Children.Add(protocolsButton);

            Button studyParticipationButton = new Button
            {
                Text = "Study Participation",
                FontSize = 20
            };

            studyParticipationButton.Clicked += async (o, e) =>
            {
                if (UiBoundSensusServiceHelper.Get(true).RegisteredProtocols.Count == 0)
                    UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("You have not yet added any studies to Sensus.");
                else
                {
                    string[] protocolNames = UiBoundSensusServiceHelper.Get(true).RegisteredProtocols.Select((protocol, index) => (index + 1) + ") " + protocol.Name).ToArray();
                    string cancelButtonName = "Cancel";
                    string selectedProtocolName = await DisplayActionSheet("Select Study", cancelButtonName, null, protocolNames);
                    if (!string.IsNullOrWhiteSpace(selectedProtocolName) && selectedProtocolName != cancelButtonName)
                    {
                        Protocol selectedProtocol = UiBoundSensusServiceHelper.Get(true).RegisteredProtocols[int.Parse(selectedProtocolName.Substring(0, selectedProtocolName.IndexOf(")"))) - 1];

                        if (selectedProtocol.Running)
                            await Navigation.PushAsync(new ParticipationReportPage(selectedProtocol));
                        else if (await DisplayAlert("Begin Study", "You are not currently participating in this study. Would you like to begin participating?", "Yes", "No"))
                            selectedProtocol.StartWithUserAgreementAsync(null);
                    }
                }
            };
            
            contentLayout.Children.Add(studyParticipationButton);

            Button pointsOfInterestButton = new Button
            {
                Text = "Points of Interest",
                FontSize = 20
            };

            pointsOfInterestButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new PointsOfInterestPage(
                        UiBoundSensusServiceHelper.Get(true).PointsOfInterest,
                        () => UiBoundSensusServiceHelper.Get(true).SaveAsync()));
            };

            contentLayout.Children.Add(pointsOfInterestButton);

            Button logButton = new Button
            {
                Text = "Log",
                FontSize = 20
            };

            logButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ViewTextLinesPage("Log", UiBoundSensusServiceHelper.Get(true).Logger.Read(200, true), () => UiBoundSensusServiceHelper.Get(true).Logger.Clear()));
            };

            contentLayout.Children.Add(logButton);

            Button stopSensusButton = new Button
            {
                Text = "Stop Sensus",
                FontSize = 20
            };

            stopSensusButton.Clicked += async (o, e) =>
            {
                if (await DisplayAlert("Stop Sensus?", "Are you sure you want to stop Sensus?", "OK", "Cancel"))
                    UiBoundSensusServiceHelper.Get(true).StopAsync();
            };

            contentLayout.Children.Add(stopSensusButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        public void DisplayServiceHelper(SensusServiceHelper serviceHelper)
        {
            _serviceHelperStacks = UiProperty.GetPropertyStacks(serviceHelper);

            foreach (StackLayout serviceStack in _serviceHelperStacks)
                ((Content as ScrollView).Content as StackLayout).Children.Add(serviceStack);
        }

        public void RemoveServiceHelper()
        {
            if (_serviceHelperStacks != null)
                foreach (StackLayout serviceStack in _serviceHelperStacks)
                    ((Content as ScrollView).Content as StackLayout).Children.Remove(serviceStack);
        }
    }
}
