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
