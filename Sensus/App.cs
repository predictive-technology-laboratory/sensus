using Sensus.Probes;
using Sensus.UI;
using System;
using System.IO;
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

#if DEBUG
            Logger.Init(_logPath, true, true, LoggingLevel.Debug, Console.Error);
#else
            Logger.Init(_logPath, true, true, LoggingLevel.Normal, Console.Error);
#endif
            
            if (Logger.Level >= LoggingLevel.Normal)
                Logger.Log("Writing error output to \"" + _logPath + "\".");

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
                    _navigationPage.CurrentPage.ForceLayout();
                };

            DataStorePage.OkPressed += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                    _navigationPage.CurrentPage.ForceLayout();
                };
            #endregion

            _navigationPage = new NavigationPage(new MainPage());
        }
    }
}
