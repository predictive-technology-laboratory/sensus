using System;
using System.IO;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Sensus.Android.Tests.SetUp;
using Sensus.Tests.Classes;
using Xamarin.Android.NUnitLite;

namespace Sensus.Android.Tests
{
    [Activity(Label = "Sensus.Android.Tests", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : TestSuiteActivity
    {
        private LogSaver _logSaver = new LogSaver();

        protected override void OnCreate(Bundle bundle)
        {
            Console.SetOut(_logSaver);
            SetUpFixture.SetUp();

            AddTest(Assembly.GetExecutingAssembly());

            // to add tests in additional assemblies, uncomment/adapt the line below
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // there is currently an issue where the test runner blocks the UI thread.
            Intent.PutExtra("automated", true);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            base.OnCreate(bundle);
        }

        public override void Finish()
        {
            RunOnUiThread(() =>
            {
                TextView logView = new TextView(this)
                {
                    ContentDescription = "sensus-test-log",
                    Text = _logSaver.Log.ToString().Trim()
                };

                ScrollView logScroll = new ScrollView(this);
                logScroll.AddView(logView);

                SetContentView(logScroll);
            });
        }
    }
}
