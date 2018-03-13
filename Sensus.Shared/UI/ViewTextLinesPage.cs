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
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Displays lines of text.
    /// </summary>
    public class ViewTextLinesPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewTextLinesPage"/> class.
        /// </summary>
        /// <param name="title">Title of page.</param>
        /// <param name="lines">Lines to display.</param>
        /// <param name="shareCallback">Called when the user clicks the Share button.</param>
        /// <param name="clearCallback">Called when the user clicks the Clear button.</param>
        public ViewTextLinesPage(string title, List<string> lines, Action shareCallback, Action clearCallback)
        {
            Title = title;

            ListView messageList = new ListView();
            messageList.ItemTemplate = new DataTemplate(typeof(TextCell));
            messageList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", mode: BindingMode.OneWay));
            messageList.ItemsSource = new ObservableCollection<string>(lines);

            StackLayout buttonStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            if (shareCallback != null)
            {
                Button shareButton = new Button
                {
                    Text = "Share",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                shareButton.Clicked += (o, e) =>
                {
                    shareCallback();
                };

                buttonStack.Children.Add(shareButton);
            }

            if (clearCallback != null)
            {
                Button clearButton = new Button
                {
                    Text = "Clear",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                };

                clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Confirm", "Do you wish to clear the list? This cannot be undone.", "Yes", "No"))
                    {
                        clearCallback();
                        messageList.ItemsSource = null;
                    }
                };

                buttonStack.Children.Add(clearButton);
            }

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, buttonStack }
            };
        }
    }
}