using System;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;

namespace Sensus.Android.Tests.AppCenter
{
    [TestFixture]
    public class Tests
    {
        AndroidApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = ConfigureApp.Android.StartApp();
        }

        [Test]
        public void UnitTests()
        {
            string log = app.WaitForElement(c => c.All().Marked("sensus-test-log"), timeout: TimeSpan.FromMinutes(2)).FirstOrDefault()?.Text;
            Console.Out.WriteLine(log);
        }
    }
}
