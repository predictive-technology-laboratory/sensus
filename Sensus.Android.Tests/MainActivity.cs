using System;
using System.IO;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Sensus.Android.Tests.SetUp;
using Xamarin.Android.NUnitLite;

namespace Sensus.Android.Tests
{
    [Activity(Label = "Sensus.Android.Tests", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : TestSuiteActivity
    {
        private class LogBuilder : TextWriter
        {
            public override Encoding Encoding => Encoding.Unicode;

            public StringBuilder Log { get; } = new StringBuilder();

            public override void Write(string value)
            {
                base.Write(value);

                lock (Log)
                {
                    Log.Append(value);
                }
            }

            public override void WriteLine()
            {
                base.WriteLine();

                lock(Log)
                {
                    Log.AppendLine();
                }
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(value);

                lock (Log)
                {
                    Log.AppendLine(value);
                }
            }
        }

        private LogBuilder _logBuilder = new LogBuilder();

        protected override void OnCreate(Bundle bundle)
        {
            SetUpFixture.SetUp();

            AddTest(Assembly.GetExecutingAssembly());

            // to add tests in additional assemblies, uncomment/adapt the line below
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // there is currently an issue where the test runner blocks the UI thread.
            Intent.PutExtra("automated", true);

            // redirect the output to our writer
            Console.SetOut(_logBuilder);

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
                    Text = _logBuilder.Log.ToString().Trim()
                };

                ScrollView logScroll = new ScrollView(this);
                logScroll.AddView(logView);

                SetContentView(logScroll);
            });
        }
    }
}
