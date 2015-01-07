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
        public SensusNavigationPage(SensusServiceHelper service)
            : base(new MainPage(service))
        {
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
            ProtocolPage.EditDataStoreTapped += async (o, e) =>
                {
                    if (e.DataStore != null)
                        await PushAsync(new DataStorePage(e));
                };

            ProtocolPage.CreateDataStoreTapped += async (o, e) =>
                {
                    await PushAsync(new CreateDataStorePage(e));
                };

            ProtocolPage.ProbeTapped += async (o, e) =>
                {
                    await PushAsync(new ProbePage(e.Item as Probe));
                };

            ProtocolPage.DisplayProtocolReport += async (o, report) =>
                {
                    await PushAsync(new ViewTextLinesPage("Protocol Report", report.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null));
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
