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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class SensusMasterPage : ContentPage
    {
        private ListView _masterPageItemsListView;

        public ListView MasterPageItemsListView 
        { 
            get 
            { 
                return _masterPageItemsListView;
            } 
        }

        public SensusMasterPage()
        {
            List<SensusDetailPageItem> detailPageItems = new List<SensusDetailPageItem>();

            detailPageItems.Add(new SensusDetailPageItem
            {
                Title = "Studies",
                IconSource = "studies.png",
                TargetType = typeof(ProtocolsPage)
            });

            detailPageItems.Add(new SensusDetailPageItem
            {
                Title = "Surveys",
                IconSource = "surveys.png",
                TargetType = typeof(PendingScriptsPage)
            });

            detailPageItems.Add(new SensusDetailPageItem
            {
                Title = "Privacy Policy",
                IconSource = "privacy.png",
                TargetType = typeof(PrivacyPolicyPage)
            });

            _masterPageItemsListView = new ListView
            {
                ItemsSource = detailPageItems,
                ItemTemplate = new DataTemplate(() =>
                {
                    var grid = new Grid { Padding = new Thickness(5, 10) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                    var image = new Image();
                    image.SetBinding(Image.SourceProperty, "IconSource");
                    var label = new Label { VerticalOptions = LayoutOptions.FillAndExpand };
                    label.SetBinding(Label.TextProperty, "Title");

                    grid.Children.Add(image);
                    grid.Children.Add(label, 1, 0);

                    return new ViewCell { View = grid };
                }),
                BackgroundColor = Color.Gray,
                SeparatorVisibility = SeparatorVisibility.None
            };

            Icon = "hamburger.png";
            Title = "Sensus";
            Content = new StackLayout
            {
                Children = { _masterPageItemsListView }
            };

            ToolbarItems.Add(new ToolbarItem("View Device ID", null, async () =>
            {
                await DisplayAlert("Device ID", SensusServiceHelper.Get().DeviceId, "Close");

            }, ToolbarItemOrder.Secondary));

            ToolbarItems.Add(new ToolbarItem("Share Log", null, async () =>
            {
                await Task.Run(() =>
                {
                    string sharePath = null;
                    try
                    {
                        sharePath = SensusServiceHelper.Get().GetSharePath(".txt");
                        SensusServiceHelper.Get().Logger.CopyTo(sharePath);
                    }
                    catch (Exception)
                    {
                        sharePath = null;
                    }

                    if (sharePath != null)
                    {
                        SensusServiceHelper.Get().ShareFileAsync(sharePath, "Log:  " + Path.GetFileName(sharePath), "text/plain");
                    }
                });

            }, ToolbarItemOrder.Secondary));

            ToolbarItems.Add(new ToolbarItem("Clear Share Directory", null, async () =>
            {
                await Task.Run(() =>
                {
                    foreach (string sharePath in Directory.GetFiles(SensusServiceHelper.SHARE_DIRECTORY))
                    {
                        try
                        {
                            File.Delete(sharePath);
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = "Failed to delete shared file \"" + Path.GetFileName(sharePath) + "\":  " + ex.Message;
                            SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                            SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                        }
                    }
                });

            }, ToolbarItemOrder.Secondary));

#if __ANDROID__
            ToolbarItems.Add(new ToolbarItem("Stop Sensus", null, async () =>
            {
                if (await DisplayAlert("Confirm", "Are you sure you want to stop Sensus? This will end your participation in all studies.", "Stop Sensus", "Go Back"))
                {
                    SensusServiceHelper.Get().StopProtocols();
                    (SensusServiceHelper.Get() as Android.IAndroidSensusServiceHelper)?.StopAndroidSensusService();
                }

            }, ToolbarItemOrder.Secondary));
#endif

            ToolbarItems.Add(new ToolbarItem("About Sensus", null, async () =>
            {
                await DisplayAlert("About Sensus", "Version:  " + SensusServiceHelper.Get().Version, "OK");

            }, ToolbarItemOrder.Secondary));
        }
    }
}
