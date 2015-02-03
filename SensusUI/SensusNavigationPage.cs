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
using SensusService.Probes;
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

            #region main page
            MainPage.ViewProtocolsTapped += async (o, e) =>
                {
                    await PushAsync(new ProtocolsPage());
                };

            MainPage.ViewLogTapped += async (o, e) =>
                {
                    await PushAsync(new ViewTextLinesPage("Log", UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue), () => UiBoundSensusServiceHelper.Get().Logger.Clear()));
                };
            #endregion

            #region protocols page
            ProtocolsPage.EditProtocol += async (o, e) =>
                {
                    await PushAsync(new ProtocolPage(o as Protocol));
                };
            #endregion

            #region protocol page
            ProtocolPage.EditDataStoreTapped += async (protocolPage, args) =>
                {
                    if (args.DataStore != null)
                        await PushAsync(new DataStorePage(args));
                };

            ProtocolPage.CreateDataStoreTapped += async (protocolPage, args) =>
                {                    
                    await PushAsync(new CreateDataStorePage(args));
                };

            ProtocolPage.ViewProbesTapped += async (o, protocol) =>
                {
                    await PushAsync(new ProbesPage(protocol));
                };

            ProtocolPage.DisplayProtocolReport += async (o, report) =>
                {
                    await PushAsync(new ViewTextLinesPage("Protocol Report", report.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null));
                };
            #endregion

            #region probes page
            ProbesPage.ProbeTapped += async (probesPage, probe) =>
                {
                    ProbePage probePage = new ProbePage(probe);

                    probePage.Disappearing += (o, e) => { (probesPage as ProbesPage).Bind(); };  // rebind the probes page to pick up changes in the probe

                    await PushAsync(probePage);
                };
            #endregion

            #region probe page
            ProbePage.ViewScriptTriggersTapped += async (o, scriptProbe) =>
                {
                    await PushAsync(new ScriptTriggersPage(scriptProbe));
                };
            #endregion

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

            #region create data store page
            CreateDataStorePage.CreateTapped += async (o, e) =>
                {
                    await PopAsync();
                    await PushAsync(new DataStorePage(e));
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
