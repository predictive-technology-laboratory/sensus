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

namespace Sensus.UI
{
    public class PrivacyPolicyPage : ContentPage
    {
        public PrivacyPolicyPage()
        {
            Title = "Privacy Policy";

            Content = new ScrollView
            {
                Content = new Label { Text = "Immediately following installation, Sensus will not collect, store, or upload any personal information from the device on which it is running. Sensus will upload reports when the app crashes. These reports contain information about the state of the app when it crashed, and Sensus developers will use these crash reports to improve Sensus. These reports do not contain any personal information. After you load a study into Sensus, Sensus will begin collecting data as defined by the study. You will be notified when the study is loaded and is about to start, and you will be asked to confirm that you wish to start the study. This confirmation will summarize the types of data to be collected. You may quit a study and/or uninstall Sensus at any time. Be aware that Sensus is publicly available and that anyone can use Sensus to design a study, which they can then share with others. Studies have the ability to collect personal information, and you should exercise caution when loading any study that you receive." }
            };
        }
    }
}
