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
