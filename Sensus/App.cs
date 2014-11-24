using Sensus.Exceptions;
using Sensus.Probes;
using Sensus.Probes.Location;
using Sensus.UI;
using System;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Geolocation;

namespace Sensus
{
    public abstract class App
    {
        private static App _singleton;
        private static readonly object _staticLockObject = new object();
        private static LoggingLevel _loggingLevel;

        /// <summary>
        /// This is a shortcut accessor for the logging level of the current App. It gets set when the the App is connected to its SensusService.
        /// </summary>
        public static LoggingLevel LoggingLevel
        {
            get { return _loggingLevel; }
        }

        protected static void Set(App app)
        {
            lock (_staticLockObject)
                _singleton = app;
        }

        public static App Get()
        {
            lock (_staticLockObject)
            {
                if (_singleton == null)
                    throw new SensusException("App singleton has not been set.");

                return _singleton;
            }
        }

        public event EventHandler StopSensusTapped;

        private NavigationPage _navigationPage;
        private ISensusService _sensusService;

        public NavigationPage NavigationPage
        {
            get { return _navigationPage; }
        }

        public ISensusService SensusService
        {
            get
            {
                // service might be null for a brief period when restarting the app
                int triesLeft = 5;
                while (_sensusService == null && triesLeft-- > 0)
                    Thread.Sleep(1000);

                if (_sensusService == null)
                    throw new SensusException("Failed to connect to SensusService.");

                return _sensusService;
            }
            set
            {
                _sensusService = value;

                // retrieve the logging level for quick access later, minimizing the computational impact of checking the logging level.
                _loggingLevel = _sensusService.LoggingLevel;
            }
        }

        protected App(Geolocator locator)
        {
            GpsReceiver.Get().Initialize(locator);            

            #region main page
            MainPage.ProtocolsTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProtocolsPage());
                };

            MainPage.SettingsTapped += (o, e) =>
                {
                };

            MainPage.StopSensusTapped += async (o, e) =>
                {
                    await App.Get().SensusService.StopAsync();

                    // also let application know that it should stop itself
                    if (StopSensusTapped != null)
                        StopSensusTapped(o, e);
                };
            #endregion

            #region protocols page
            ProtocolsPage.EditProtocol += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProtocolPage(o as Protocol));
                };
            #endregion

            #region protocol page
            ProtocolPage.CreateLocalDataStoreTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new CreateDataStorePage(o as Protocol, true));
                };

            ProtocolPage.CreateRemoteDataStoreTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new CreateDataStorePage(o as Protocol, false));
                };

            ProtocolPage.ProbeTapped += async (o, e) =>
                {
                    await _navigationPage.PushAsync(new ProbePage(e.Item as Probe));
                };
            #endregion

            #region data stores page
            CreateDataStorePage.CreateDataStoreTapped += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                    await _navigationPage.PushAsync(new DataStorePage(e.DataStore, e.Protocol, e.Local));
                };
            #endregion

            #region data store page
            DataStorePage.CancelTapped += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                    _navigationPage.CurrentPage.ForceLayout();
                };

            DataStorePage.OkTapped += async (o, e) =>
                {
                    await _navigationPage.PopAsync();
                    _navigationPage.CurrentPage.ForceLayout();
                };
            #endregion

            _navigationPage = new NavigationPage(new MainPage());
        }
    }
}
