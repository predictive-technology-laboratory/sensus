using Sensus.Probes;
using Sensus.Probes.Location;
using Sensus.UI;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Geolocation;

namespace Sensus
{
    public abstract class App
    {
        private static App _singleton;

        protected static void Set(App app)
        {
            if (_singleton == null)
                _singleton = app;
            else
                throw new InvalidOperationException("App singleton has already been set.");
        }

        public static App Get()
        {
            if (_singleton == null)
                throw new InvalidOperationException("App singleton has not been set.");

            return _singleton;
        }

        private NavigationPage _navigationPage;
        private readonly string _logPath;

        public NavigationPage NavigationPage
        {
            get { return _navigationPage; }
        }

        protected App(Geolocator locator)
        {
            GpsReceiver.Get().Initialize(locator);

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
                    await _navigationPage.PushAsync(new CreateDataStorePage(o as Protocol, true));
                };

            ProtocolPage.CreateRemoteDataStorePressed += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new CreateDataStorePage(o as Protocol, false));
                };

            ProtocolPage.ProbeTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProbePage(e.Item as Probe));
                };
            #endregion

            #region data stores page
            CreateDataStorePage.CreateDataStorePressed += async (o, e) =>
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

        public void OnStop()
        {
            // TODO:  Stop protocols

            GpsReceiver.Get().ClearListeners();
        }
    }
}
