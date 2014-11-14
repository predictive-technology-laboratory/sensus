using Sensus.Probes;
using Sensus.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace Sensus
{
    public class App
    {
        private static App _app;

        public static void Init(ProbeInitializer probeInitializer)
        {
            _app = new App(probeInitializer);
        }

        public static App Get()
        {
            return _app;
        }

        private ProbeInitializer _probeInitializer;
        private readonly string _logPath;
        private NavigationPage _navigationPage;

        public ProbeInitializer ProbeInitializer
        {
            get { return _probeInitializer; }
        }

        public NavigationPage NavigationPage
        {
            get { return _navigationPage; }
        }

        public App(ProbeInitializer probeInitializer)
        {
            _probeInitializer = probeInitializer;

            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
            Console.Error.WriteLine("Writing error output to \"" + _logPath + "\".");
            Console.SetError(new StandardOutWriter(_logPath, true, true, Console.Error));

            #region main page
            MainPage.ProtocolsTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProtocolsPage());
                };
            #endregion

            #region protocols page
            ProtocolsPage.ProtocolTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProtocolPage(e.Item as Protocol));
                };
            #endregion

            #region protocol page
            ProtocolPage.CreateLocalDataStorePressed += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new DataStoresPage(o as Protocol, true));
                };

            ProtocolPage.CreateRemoteDataStorePressed += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new DataStoresPage(o as Protocol, false));
                };

            ProtocolPage.ProbeTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProbePage(e.Item as Probe));
                };
            #endregion

            #region data stores page
            DataStoresPage.CreateDataStorePressed += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                    await _navigationPage.PushAsync(new DataStorePage(e.DataStore, e.Protocol, e.Local));
                };
            #endregion

            #region data store page
            DataStorePage.CancelPressed += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                };

            DataStorePage.OkPressed += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                };
            #endregion

            _navigationPage = new NavigationPage(new MainPage());
        }
    }
}
