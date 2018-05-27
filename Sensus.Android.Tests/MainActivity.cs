using System.Reflection;

using Android.App;
using Android.OS;
using Sensus.Android.Tests.SetUp;
using Xamarin.Android.NUnitLite;

namespace Sensus.Android.Tests
{
    [Activity(Label = "Sensus.Android.Tests", MainLauncher = true)]
    public class MainActivity : TestSuiteActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            SetUpFixture.SetUp();

            AddTest(Assembly.GetExecutingAssembly());

            // AddTest (typeof (Your.Library.TestClass).Assembly);

            Intent.PutExtra("automated", true);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            base.OnCreate(bundle);
        }
    }
}
