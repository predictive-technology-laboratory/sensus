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
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Sensus.UI
{
    public class App : Application
    {
        public Page MasterPage
        {
            get { return (MainPage as SensusMasterDetailPage).Master; }   
        }

        public Page DetailPage
        {
            get { return (MainPage as SensusMasterDetailPage).Detail; }
            set { (MainPage as SensusMasterDetailPage).Detail = value; }
        }

        public App()
        {
            MainPage = new SensusMasterDetailPage();
        }

        protected override void OnStart()
        {
            base.OnStart();

            AppCenter.Start("ios=" + SensusServiceHelper.APP_CENTER_KEY_IOS + ";" + 
                            "android=" + SensusServiceHelper.APP_CENTER_KEY_ANDROID, 
                            typeof(Analytics), 
                            typeof(Crashes));
        }
    }
}
