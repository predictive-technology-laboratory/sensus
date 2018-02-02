using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using Xamarin.UITest.Queries;

namespace Sensus.iOS.Tests.AppCenter
{
    [TestFixture]
    public class Tests
    {
        private IApp _app;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _app = ConfigureApp.iOS.StartApp();
        }

        [Test]
        public void TestApp()
        {
            // run tests and wait for results
            _app.Tap(c => c.Text("Run Tests"));
            _app.WaitForElement(c => c.Text("Overall result:"), timeout: TimeSpan.FromSeconds(90));

            // get and parse label content
            List<AppResult> labels = _app.Query(q => q.Class("UILabel")).ToList();
            string overallResult = labels[labels.FindIndex(label => label.Text.Trim() == "Overall result:") + 1].Text;
            int testsRun = int.Parse(labels[labels.FindIndex(label => label.Text.Trim() == "Tests run:") + 1].Text);
            int testsPassed = int.Parse(labels[labels.FindIndex(label => label.Text.Trim() == "Passed:") + 1].Text);

            // check results
            if (overallResult != "Passed" || testsPassed != testsRun)
            {
                _app.Tap(c => c.Text("Failed Results"));
                _app.Screenshot("Failures");
                throw new Exception($"{testsPassed} of {testsRun} passed.");
            }
        }
    }
}
