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
using Sensus.UI.Inputs;
using Xamarin.Forms;
using System.Linq;
using System.Collections.Generic;

namespace Sensus.UI
{
    public class TaggingPage : ContentPage
    {
        private List<string> _selectedTags;

        public TaggingPage(Protocol protocol)
        {
            Title = "Tagging";

            string selectTagButtonText = "Tap To Select Tag(s)";
            if (protocol.TaggedEventTags != null)
            {
                List<string> currTags = protocol.TaggedEventTags;
                currTags.Sort();
                selectTagButtonText = GetTagsString(currTags);
            }

            Button selectTagButton = new Button
            {
                Text = selectTagButtonText,
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            selectTagButton.Clicked += async (sender, e) =>
            {
                List<object> tags = protocol.AvailableTags.Cast<object>().ToList();
                tags.Sort();

                ItemPickerPageInput tagPickerInput = await SensusServiceHelper.Get().PromptForInputAsync("Select Tag(s)",
                                                                                                         new ItemPickerPageInput("Available Tags", tags)
                                                                                                         {
                                                                                                             Multiselect = true,
                                                                                                             Required = true
                                                                                                         },
                                                                                                         null, true, "OK", null, null, null, false) as ItemPickerPageInput;

                if (tagPickerInput != null)
                {
                    _selectedTags = (tagPickerInput.Value as List<object>).Cast<string>().ToList();
                    _selectedTags.Sort();
                    selectTagButton.Text = GetTagsString(_selectedTags);
                }
            };

            Button addTagButton = new Button
            {
                Text = "Add Tag",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            addTagButton.Clicked += async (sender, e) =>
            {
                SingleLineTextInput textInput = await SensusServiceHelper.Get().PromptForInputAsync("Add Tag",
                                                                                                    new SingleLineTextInput("Tag:", Keyboard.Text)
                                                                                                    {
                                                                                                        Required = true
                                                                                                    },
                                                                                                    null, true, "Add", null, null, null, false) as SingleLineTextInput;

                string tagToAdd = textInput?.Value?.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(tagToAdd) && !protocol.AvailableTags.Contains(tagToAdd))
                {
                    protocol.AvailableTags.Add(tagToAdd);
                }
            };

            Button startStopButton = new Button
            {
                Text = protocol.TaggedEventId == null ? "Start" : "Stop",
                FontSize = 50,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            selectTagButton.IsEnabled = addTagButton.IsEnabled = protocol.TaggedEventId == null;

            startStopButton.Clicked += async (sender, e) =>
            {
                if (startStopButton.Text == "Start")
                {
                    if (_selectedTags != null && _selectedTags.Count > 0)
                    {
                        // set the tags collection before the id, as the id is our indicator that tagging is active. we
                        // want to avoid the situation where we begin attaching the id to data without yet having set the tags.
                        protocol.TaggedEventTags = _selectedTags;
                        protocol.TaggedEventId = Guid.NewGuid().ToString();

                        startStopButton.Text = "Stop";
                        selectTagButton.IsEnabled = addTagButton.IsEnabled = false;
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Must select 1 or more tags before starting.");
                    }
                }
                else
                {
                    // unset the tags collection before the id, as the id is our indicator that tagging is active. we
                    // want to avoid the situation where we continue attaching null tags.
                    protocol.TaggedEventId = null;
                    protocol.TaggedEventTags = null;

                    startStopButton.Text = "Start";
                    selectTagButton.IsEnabled = addTagButton.IsEnabled = true;
                }
            };

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children =
                    {
                        selectTagButton,
                        addTagButton,
                        startStopButton
                    }
                }
            };
        }

        private string GetTagsString(List<string> tags)
        {
            string tagsString = null;

            if (tags.Count > 0)
            {
                tagsString = ":  " + string.Concat(tags.Select(tag => tag + ", ")).Trim(',', ' ');
            }

            return tags.Count + " Tag" + (tags.Count == 1 ? "" : "s") + tagsString;
        }
    }
}