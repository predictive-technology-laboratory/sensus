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
            messageList.ItemTemplate = new DataTemplate(typeof(TextCell));
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
