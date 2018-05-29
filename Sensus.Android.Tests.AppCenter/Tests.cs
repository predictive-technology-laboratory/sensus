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
            Assert.NotNull(log);

            string[] logLines = log.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string[] resultParts = logLines.Last().Split(',');
            Assert.AreEqual(resultParts.Length, 5);

            int testsRun = int.Parse(resultParts[0].Split(':')[1]);
            int testsPassed = int.Parse(resultParts[1].Split(':')[1]);
            int testsFailed = int.Parse(resultParts[2].Split(':')[1]);
            int testsSkipped = int.Parse(resultParts[3].Split(':')[1]);
            int testsInconclusive = int.Parse(resultParts[4].Split(':')[1]);

            Assert.GreaterOrEqual(testsRun, 145);  // will need to update this as we develop. ensures that tests are actually run.
            Assert.AreEqual(testsRun, testsPassed);
            Assert.AreEqual(testsFailed, 0);
            Assert.AreEqual(testsSkipped, 0);
            Assert.AreEqual(testsInconclusive, 0);
        }
    }
}
