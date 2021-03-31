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

using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Sensus.UI
{
    public partial class App : Application
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

            InitializeComponent();
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