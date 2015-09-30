// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xamarin.UITest;
using NUnit.Framework;
using System.Linq;
using Xamarin.UITest.Queries;

namespace Sensus.UiTest
{
    [TestFixture]
    public abstract class Tests
    {
        private const string UNIT_TESTING_PROTOCOL_NAME = "Unit Testing Protocol";
        private const string PROTOCOL_START = "Start";
        private const string PROTOCOL_EDIT = "Edit";
        private const string PROTOCOL_STATUS = "Status";
        private const string PROTOCOL_STOP = "Stop";
        private const string PROTOCOL_STOP_CONFIRM = "Yes";
        private const string PROTOCOL_CONSENT_SUBMIT_BUTTON = "NextButton";
        private const string PROTOCOL_CONSENT_MESSAGE = "ConsentMessage";
        private const string PROTOCOL_CONSENT_CODE = "ConsentCode";
        private const string LOCAL_DATA_STORE_EDIT = "Local Data Store";
        private const string REMOTE_DATA_STORE_EDIT = "Remote Data Store";
        private const string DATA_STORE_COMMIT_DELAY = "Commit Delay (MS): View";
        private const string DATA_STORE_OK = "OK";

        private IApp _app;

        protected IApp App
        {
            get { return _app; }
            set { _app = value; }
        }

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void ReadEvaluatePrintLoop()
        {
            _app.Repl();
        }

        [Test]
        public void RunUnitTestingProtocol()
        {
            ConsentToProtocolStart();
            AssertProtocolRunning(new TimeSpan(0, 0, 5));
            AssertProtocolStatusEmpty("Protocol status after startup.");

            // set data store delays such that they will be testable within a reasonable time period
            StopProtocol();
            TapProtocol();
            _app.WaitForElementThenTap(PROTOCOL_EDIT);

            TimeSpan localDataStoreDelay = new TimeSpan(0, 0, 5);
            _app.WaitForElementThenTap(LOCAL_DATA_STORE_EDIT);
            _app.WaitForElementThenEnterText(DATA_STORE_COMMIT_DELAY, localDataStoreDelay.TotalMilliseconds.ToString());
            _app.WaitForElementThenTap(DATA_STORE_OK);

            TimeSpan remoteDataStoreDelay = new TimeSpan(0, 0, 15);
            _app.WaitForElementThenTap(REMOTE_DATA_STORE_EDIT);
            _app.WaitForElementThenEnterText(DATA_STORE_COMMIT_DELAY, remoteDataStoreDelay.TotalMilliseconds.ToString());
            _app.WaitForElementThenTap(DATA_STORE_OK);
            _app.Back();  // to protocols page

            // start protocol, wait for remote data store to commit data, and then check status
            StartProtocol();
            _app.WaitFor(remoteDataStoreDelay.Add(new TimeSpan(0, 0, 25)));
            AssertProtocolStatusEmpty("Protocol status after remote data store.");
        }

        private void TapProtocol()
        {
            _app.WaitForElementThenTap(UNIT_TESTING_PROTOCOL_NAME);
            _app.WaitForElement(PROTOCOL_EDIT);  // wait for action sheet to come up
        }

        private void StartProtocol()
        {
            TapProtocol();
            _app.WaitForElementThenTap(PROTOCOL_START);
            ConsentToProtocolStart();
        }

        private void ConsentToProtocolStart()
        {
            // wait for consent screen to come up
            _app.WaitForElement(PROTOCOL_CONSENT_MESSAGE);

            // enter the consent code
            string consentMessage = _app.Query(PROTOCOL_CONSENT_MESSAGE).First().Text;
            int consentCode = int.Parse(consentMessage.Substring(consentMessage.LastIndexOf(" ") + 1));
            _app.WaitForElementThenEnterText(PROTOCOL_CONSENT_CODE, consentCode.ToString());
            _app.WaitForElementThenTap(PROTOCOL_CONSENT_SUBMIT_BUTTON);
        }

        private void AssertProtocolRunning(TimeSpan delay)
        {
            _app.WaitFor(delay);
            TapProtocol();
            Assert.IsTrue(_app.Query(PROTOCOL_STOP).Any());
            _app.Back();  // to protocols page
        }

        private void AssertProtocolStatusEmpty(string screenshotTitle)
        {
            TapProtocol();
            _app.WaitForElementThenTap(PROTOCOL_STATUS);
            Func<AppQuery, AppQuery> statusLinesQuery = c => c.Class("TextCellRenderer_TextCellView").Class("TextView");
            _app.WaitForElement(statusLinesQuery);
            _app.SetOrientationLandscape();
            _app.Screenshot(screenshotTitle);
            string[] errorWarningMisc = _app.Query(statusLinesQuery).Select(c => c.Text).ToArray();
            Assert.AreEqual(errorWarningMisc.Length, 3);
            foreach (string line in errorWarningMisc)
                Assert.IsEmpty(line.Substring(line.IndexOf(":") + 1).Trim());

            _app.SetOrientationPortrait();
            _app.Back();  // to protocols page
        }

        private void StopProtocol()
        {
            TapProtocol();
            _app.WaitForElementThenTap(PROTOCOL_STOP);
            _app.WaitForElementThenTap(PROTOCOL_STOP_CONFIRM);
        }
    }
}