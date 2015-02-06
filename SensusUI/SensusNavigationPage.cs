#region copyright
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
#endregion

using SensusService;
using System;
using System.Linq;
using Xamarin.Forms;

namespace SensusUI
{
    public class SensusNavigationPage : NavigationPage
    {
        private MainPage _mainPage;

        public MainPage MainPage
        {
            get { return _mainPage; }
        }

        public SensusNavigationPage(MainPage mainPage)
            : base(mainPage)
        {
            _mainPage = mainPage;

            #region script triggers page
            ScriptTriggersPage.AddTriggerTapped += async (o, scriptProbe) =>
                {
                    await PushAsync(new AddScriptProbeTriggerPage(scriptProbe));
                };
            #endregion

            #region add script probe trigger
            AddScriptProbeTriggerPage.TriggerAdded += async (o, e) =>
                {
                    await PopAsync();
                };
            #endregion

            #region data store page
            DataStorePage.OkTapped += async (o, e) =>
                {
                    await PopAsync();
                };
            #endregion
        }
    }
}
