using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class SensusNavigationPage : NavigationPage
    {
        public SensusNavigationPage()
            : base(new MainPage())
        {
            #region main page
            MainPage.ProtocolsTapped += async (o, e) =>
                {
                    await PushAsync(new ProtocolsPage());
                };

            MainPage.StatusTapped += async (o, e) =>
                {
                    await PushAsync(new StatusPage(SensusServiceHelper.Get()));
                };

            MainPage.StopSensusTapped += async (o, e) =>
                {
                    await SensusServiceHelper.Get().StopAsync();
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
