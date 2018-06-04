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

using System.Collections.Generic;
using Sensus.Context;
using Sensus.UI.Inputs;
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

            SensusDetailPageItem accountItem = new SensusDetailPageItem
            {
                Title = "Log In",
                IconSource = "account.png"
            };

            accountItem.Action = () =>
            {
                if (accountItem.Title == "Log Out")
                {
                    SensusContext.Current.IamRegion = null;
                    SensusContext.Current.IamAccessKey = null;
                    SensusContext.Current.IamAccessKeySecret = null;
                    accountItem.Title = "Log In";
                }
                else
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        Input input = await SensusServiceHelper.Get().PromptForInputAsync("Log In", new QrCodeInput(QrCodePrefix.IAM_CREDENTIALS, "Account:  ", true, "Please scan your account barcode."), null, true, null, null, null, null, false);

                        if (input == null)
                        {
                            return;
                        }

                        string error = null;

                        string credentials = input.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(credentials))
                        {
                            error = "Empty credentials barcode.";
                        }
                        else
                        {
                            string[] parts = credentials.Split(':');
                            if (parts.Length == 3)
                            {
                                SensusContext.Current.IamRegion = parts[0];
                                SensusContext.Current.IamAccessKey = parts[1];
                                SensusContext.Current.IamAccessKeySecret = parts[2];
                            }
                            else
                            {
                                error = "Invalid credentials barcode.";
                            }
                        }

                        if (error == null)
                        {
                            accountItem.Title = "Log Out";
                            await SensusServiceHelper.Get().FlashNotificationAsync("Logged in.");
                        }
                        else
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync(error);
                        }
                    });
                }
            };

            detailPageItems.Add(accountItem);

            _masterPageItemsListView = new ListView(ListViewCachingStrategy.RecycleElement)
            {
                ItemsSource = detailPageItems,
                ItemTemplate = new DataTemplate(() =>
                {
                    Grid grid = new Grid { Padding = new Thickness(5, 10) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                    Image image = new Image();
                    image.SetBinding(Image.SourceProperty, nameof(SensusDetailPageItem.IconSource));

                    Label label = new Label { VerticalOptions = LayoutOptions.FillAndExpand };
                    label.SetBinding(Label.TextProperty, nameof(SensusDetailPageItem.Title));

                    grid.Children.Add(image);
                    grid.Children.Add(label, 1, 0);

                    return new ViewCell { View = grid };
                }),
                
                SeparatorVisibility = SeparatorVisibility.None
            };

            Icon = "hamburger.png";
            Title = "Sensus";

            Content = new StackLayout
            {
                Children = { _masterPageItemsListView }
            };
        }
    }
}
