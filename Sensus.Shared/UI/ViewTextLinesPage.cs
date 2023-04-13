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
using System.IO;
using Sensus.Exceptions;
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
        /// <param name="clearCallback">Called when the user clicks the Clear button.</param>
        public ViewTextLinesPage(string title, List<string> lines, Action clearCallback = null)
        {
            Title = title;

            ListView messageList = new ListView(ListViewCachingStrategy.RecycleElement);
            messageList.ItemTemplate = new DataTemplate(typeof(DarkModeCompatibleTextCell));
            messageList.ItemTemplate.SetBinding(TextCell.TextProperty, ".");
            messageList.ItemsSource = new ObservableCollection<string>(lines);
            messageList.ItemTapped += async (sender, e) =>
            {
                await DisplayAlert("Message", e.Item.ToString(), "Close");
            };

            StackLayout buttonStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            Button shareButton = new Button
            {
                Text = "Share",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            shareButton.Clicked += async (o, e) =>
            {
                try
                {
                    string sharePath = SensusServiceHelper.Get().GetSharePath(".txt");
                    File.WriteAllLines(sharePath, lines);
                    await SensusServiceHelper.Get().ShareFileAsync(sharePath, Path.GetFileName(sharePath), "text/plain");
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to share text lines.", ex);
                    await SensusServiceHelper.Get().FlashNotificationAsync("Failed to share:  " + ex.Message);
                }
            };

            buttonStack.Children.Add(shareButton);

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