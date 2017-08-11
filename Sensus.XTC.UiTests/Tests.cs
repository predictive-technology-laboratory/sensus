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
using System.Threading;

namespace Sensus.XTC.UiTests
{
    [TestFixture]
    public abstract class Tests
    {
        private const string ENABLE_TEST_CLOUD_PROTOCOL_RUNNING_NAME = "UI Testing Protocol (Running)";
        private const string ENABLE_TEST_CLOUD_PROTOCOL_STOPPED_NAME = "UI Testing Protocol (Stopped)";

        private const string PROTOCOL_ACTION_SHEET_START = "Start";
        private const string PROTOCOL_ACTION_SHEET_EDIT = "Edit";
        private const string PROTOCOL_ACTION_SHEET_STATUS = "Status";
        private const string PROTOCOL_ACTION_SHEET_STOP = "Stop";
        private const string PROTOCOL_ACTION_SHEET_CANCEL = "Cancel";
        private const string PROTOCOL_STOP_CONFIRM = "Yes";

        private const string PROTOCOL_CONSENT_CODE_LABEL = "ConsentCode Label";
        private const string PROTOCOL_CONSENT_CODE = "ConsentCode";

        private const string LOCAL_DATA_STORE_EDIT = "Local Data Store";
        private const string REMOTE_DATA_STORE_EDIT = "Remote Data Store";
        private const string DATA_STORE_COMMIT_DELAY = "Commit Delay (MS): View";
        private const string DATA_STORE_OK = "OK";

        private const string PROBES_EDIT = "Probes";
        private const string ACCELEROMETER_PROBE = "Acceleration (Listening)";
        private const string ACCELEROMETER_ENABLED = "Enabled: View";
        private const string ACCELEROMETER_TRIGGERED_SCRIPT_INPUT = "Accelerometer Test Input";

        private const string PROMPT_FOR_INPUTS_SUBMIT = "NextButton";
        private const string PROMPT_FOR_INPUTS_SUBMIT_CONFIRM = "Yes";

        private IApp _app;

        [SetUp]
        public void SetUp()
        {
            _app = GetApp();
        }

        protected abstract IApp GetApp();

        [Test]
        public void ReadEvaluatePrintLoop()
        {
            _app.Repl();
        }

        [Test]
        public void RunProtocol()
        {
            // wait a bit for app to start up -- ios sometimes displays permissions dialogs that need time to be dismissed (UiTest appears to take care of these on its own)
            Thread.Sleep(5000);
                
            // protocol has been started and is waiting for user consent
            ConsentToProtocolStart(new TimeSpan(0, 0, 5));

            // stop and edit protocol
            StopProtocol();
            TapProtocol(false);
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_EDIT, false);

            // set data store delays such that they will be testable within a reasonable time period
            TimeSpan localDataStoreDelay = new TimeSpan(0, 0, 5);
            _app.WaitForElementThenTap(LOCAL_DATA_STORE_EDIT, true);
            _app.WaitForElementThenEnterText(DATA_STORE_COMMIT_DELAY, true, localDataStoreDelay.TotalMilliseconds.ToString());
            _app.WaitForElementThenTap(DATA_STORE_OK, true);

            TimeSpan remoteDataStoreDelay = new TimeSpan(0, 0, 15);
            _app.WaitForElementThenTap(REMOTE_DATA_STORE_EDIT, true);
            _app.WaitForElementThenEnterText(DATA_STORE_COMMIT_DELAY, true, remoteDataStoreDelay.TotalMilliseconds.ToString());
            _app.WaitForElementThenTap(DATA_STORE_OK, true);  // to protocol page
            _app.Back();  // to protocols page

            // restart protocol, wait for remote data store to commit data, and then check status
            StartProtocol(new TimeSpan(0, 0, 5));
            Thread.Sleep(remoteDataStoreDelay.Add(new TimeSpan(0, 0, 5)));
            AssertProtocolStatusEmpty("Protocol status after remote data store.");

            // enable accelerometer to check script triggering
            TapProtocol(true);
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_EDIT, false);
            _app.WaitForElementThenTap(PROBES_EDIT, true);
            _app.WaitForElementThenTap(ACCELEROMETER_PROBE, true);
            _app.WaitForElementThenTap(ACCELEROMETER_ENABLED, true);
            Thread.Sleep(10000);  // 5-second accelerometer stabilization + a few seconds for the prompt page to show up before trying to scroll to it.
            _app.WaitForElementThenEnterText(ACCELEROMETER_TRIGGERED_SCRIPT_INPUT, true, "12345");
            _app.WaitForElementThenTap(PROMPT_FOR_INPUTS_SUBMIT, true);
            _app.WaitForElementThenTap(PROMPT_FOR_INPUTS_SUBMIT_CONFIRM, false);
            _app.Back();  // to probes page
            _app.Back();  // to protocol page
            _app.Back();  // to protocols page

            StopProtocol();
        }

        private void TapProtocol(bool running)
        {
            _app.WaitForElementThenTap(running ? ENABLE_TEST_CLOUD_PROTOCOL_RUNNING_NAME : ENABLE_TEST_CLOUD_PROTOCOL_STOPPED_NAME, true);
            _app.WaitForElement(PROTOCOL_ACTION_SHEET_EDIT);  // wait for action sheet to come up
        }

        private void StartProtocol(TimeSpan startupCheckDelay)
        {
            TapProtocol(false);
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_START, false);
            ConsentToProtocolStart(startupCheckDelay);
        }

        private void ConsentToProtocolStart(TimeSpan startupCheckDelay)
        {
            // wait for consent screen to come up
            _app.WaitForElement(PROTOCOL_CONSENT_CODE_LABEL);

            // enter the consent code
            string consentMessage = _app.Query(PROTOCOL_CONSENT_CODE_LABEL).First().Text;
            int consentCode = int.Parse(consentMessage.Substring(consentMessage.LastIndexOf(" ") + 1));
            _app.WaitForElementThenEnterText(PROTOCOL_CONSENT_CODE, true, consentCode.ToString());
            _app.WaitForElementThenTap(PROMPT_FOR_INPUTS_SUBMIT, true);

            // wait for the protocol to start
            Thread.Sleep(startupCheckDelay);

            // confirm that the protocol has started
            TapProtocol(true);
            Assert.IsTrue(_app.Query(PROTOCOL_ACTION_SHEET_STOP).Any());
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_CANCEL, false);  // to protocols page
        }

        private void AssertProtocolStatusEmpty(string statusScreenshotTitle)
        {
            TapProtocol(true);
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_STATUS, false);
            _app.WaitForElement(GetStatusLinesQuery());
            _app.SetOrientationLandscape();
            _app.Screenshot(statusScreenshotTitle);
            string[] errorWarningMisc = _app.Query(GetStatusLinesQuery()).Select(c => c.Text).ToArray();
            Assert.AreEqual(errorWarningMisc.Length, 3);
            foreach (string line in errorWarningMisc)
                Assert.IsEmpty(line.Substring(line.IndexOf(":") + 1).Trim());

            _app.SetOrientationPortrait();
            _app.Back();  // to protocols page
        }

        protected abstract Func<AppQuery, AppQuery> GetStatusLinesQuery();

        private void StopProtocol()
        {
            TapProtocol(true);
            _app.WaitForElementThenTap(PROTOCOL_ACTION_SHEET_STOP, false);
            _app.WaitForElementThenTap(PROTOCOL_STOP_CONFIRM, false);
        }
    }
}