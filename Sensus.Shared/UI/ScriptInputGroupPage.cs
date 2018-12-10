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

using Xamarin.Forms;
using Sensus.UI.Inputs;
using Sensus.UI.UiProperties;
using System.Collections.Generic;

namespace Sensus.UI
{
    public class ScriptInputGroupPage : ContentPage
    {
        public ScriptInputGroupPage(InputGroup inputGroup, List<InputGroup> previousInputGroups)
        {
            Title = "Input Group";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(inputGroup))
                contentLayout.Children.Add(stack);

            Button editInputsButton = new Button
            {
                Text = "Edit Inputs",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editInputsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptInputsPage(inputGroup, previousInputGroups));
            };

            contentLayout.Children.Add(editInputsButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
