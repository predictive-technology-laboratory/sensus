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
using Sensus.UI.Inputs;
using Xamarin.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Sensus.UI
{
    public class TaggingPage : ContentPage
    {
        public TaggingPage(Protocol protocol)
        {
            Title = "Tagging";

            Button selectTagButton = new Button
            {
                Text = GetSelectTagButtonText(protocol),
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            selectTagButton.Clicked += async (sender, e) =>
            {
                List<object> availableTags = protocol.AvailableTags.Cast<object>().ToList();
                availableTags.Sort();

                ItemPickerPageInput tagPickerInput = await SensusServiceHelper.Get().PromptForInputAsync("Select Tag(s)",
                                                                                                         new ItemPickerPageInput("Available Tags", availableTags)
                                                                                                         {
                                                                                                             Multiselect = true,
                                                                                                             Required = true
                                                                                                         },
                                                                                                         null, true, "OK", null, null, null, false) as ItemPickerPageInput;

                if (tagPickerInput != null)
                {
                    protocol.TaggedEventTags = (tagPickerInput.Value as List<object>).Cast<string>().ToList();
                    protocol.TaggedEventTags.Sort();
                    selectTagButton.Text = GetSelectTagButtonText(protocol);
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

            Button exportTaggingsButton = new Button
            {
                Text = "Export Taggings",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            exportTaggingsButton.Clicked += async (sender, e) =>
            {
                try
                {
                    if (protocol.TaggingsToExport.Count == 0)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("There are no taggings to export. Select tags and tap Start to generate a tagging.");
                    }
                    else
                    {
                        string path = SensusServiceHelper.Get().GetSharePath(".csv");
                        File.WriteAllText(path, "tagged-event-id,start,end,tags" + Environment.NewLine + string.Concat(protocol.TaggingsToExport.Select(tagging => tagging + Environment.NewLine)));
                        await SensusServiceHelper.Get().ShareFileAsync(path, protocol.Name + ":  Taggings", "text/plain");
                    }
                }
                catch (Exception ex)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Failed to export taggings:  " + ex.Message);
                }
            };

            startStopButton.Clicked += async (sender, e) =>
            {
                if (startStopButton.Text == "Start")
                {
                    if (protocol.TaggedEventTags.Count > 0)
                    {
                        // mark the start of the tagging
                        protocol.TaggingStartTimestamp = DateTimeOffset.UtcNow;

                        // set the tagged event id, which signals the data storage routines to apply tags to all data.
                        protocol.TaggedEventId = Guid.NewGuid().ToString();

                        startStopButton.Text = "Stop";
                        selectTagButton.IsEnabled = addTagButton.IsEnabled = exportTaggingsButton.IsEnabled = false;
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Must select 1 or more tags before starting.");
                    }
                }
                else
                {
                    // hang on to the id, as we're about to clear it.
                    string taggedEventId = protocol.TaggedEventId;

                    // clear the tagged event id, which signals the data storage routines to stop applying tags to data.
                    protocol.TaggedEventId = null;

                    // mark the end of the tagging
                    protocol.TaggingEndTimestamp = DateTimeOffset.UtcNow;

                    if (await DisplayAlert("Confirm Tagging", "Would you like to keep the following tagging?" + Environment.NewLine + Environment.NewLine +
                                           "Start:  " + protocol.TaggingStartTimestamp + Environment.NewLine +
                                           "End:  " + protocol.TaggingEndTimestamp + Environment.NewLine +
                                           "Tags:  " + string.Concat(protocol.TaggedEventTags.Select(tag => tag + ", ")).Trim(',', ' '), "Yes", "No"))
                    {
                        protocol.TaggingsToExport.Add(taggedEventId + "," + protocol.TaggingStartTimestamp + "," + protocol.TaggingEndTimestamp + "," + string.Concat(protocol.TaggedEventTags.Select(tag => tag + "|")).Trim('|'));
                        await SensusServiceHelper.Get().FlashNotificationAsync("Tagging kept. Tap the export button when finished with all taggings.");
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Tagging discarded.");
                    }

                    protocol.TaggingStartTimestamp = protocol.TaggingEndTimestamp = null;

                    startStopButton.Text = "Start";
                    selectTagButton.IsEnabled = addTagButton.IsEnabled = exportTaggingsButton.IsEnabled = true;
                }
            };

            // a tagging might have been started on a previous display of this page
            selectTagButton.IsEnabled = addTagButton.IsEnabled = exportTaggingsButton.IsEnabled = protocol.TaggedEventId == null;

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
                        startStopButton,
                        exportTaggingsButton
                    }
                }
            };
        }

        private string GetSelectTagButtonText(Protocol protocol)
        {
            string text = "Tap To Select Tag(s)";

            if (protocol.TaggedEventTags.Count > 0)
            {
                text = protocol.TaggedEventTags.Count + " Tag" + (protocol.TaggedEventTags.Count == 1 ? "" : "s") + ":  " + string.Concat(protocol.TaggedEventTags.Select(tag => tag + ", ")).Trim(',', ' ');
            }

            return text;
        }
    }
}
