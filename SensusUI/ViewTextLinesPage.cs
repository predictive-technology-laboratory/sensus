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
using Xamarin.Forms;

namespace SensusUI
{
    public class ViewTextLinesPage : ContentPage
    {
        public ViewTextLinesPage(string title, List<string> lines, Action clearCallback)
        {
            Title = title;

            ListView messageList = new ListView();
            messageList.ItemTemplate = new DataTemplate(typeof(TextCell));
            messageList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", mode: BindingMode.OneWay));
            messageList.ItemsSource = new ObservableCollection<string>(lines);

            Button shareButton = new Button
            {
                Text = "Share",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            shareButton.Clicked += (o, e) =>
                {
                    string path = null;
                    try
                    {
                        path = UiBoundSensusServiceHelper.Get(true).GetSharePath(".txt");
                        using (StreamWriter file = new StreamWriter(path))
                        {
                            foreach (string line in lines)
                                file.WriteLine(line);

                            file.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to write lines to temp file for sharing:  " + ex.Message, SensusService.LoggingLevel.Normal);
                        path = null;
                    }

                    if (path != null)
                        UiBoundSensusServiceHelper.Get(true).ShareFileAsync(path, title + ":  " + Path.GetFileName(path));
                };

            Button clearButton = new Button
            {
                Text = "Clear",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = clearCallback != null
            };

            if (clearCallback != null)
                clearButton.Clicked += async (o, e) =>
                    {
                        if (await DisplayAlert("Confirm", "Do you wish to clear the list? This cannot be undone.", "OK", "Cancel"))
                        {
                            clearCallback();
                            messageList.ItemsSource = null;
                        }
                    };

            StackLayout shareClearStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { shareButton, clearButton }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, shareClearStack }
            };
        }
    }
}
