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
        private const string PROTOCOL_START_NEXT_BUTTON = "NextButton";
        private const string PROTOCOL_START_CONSENT_MESSAGE = "ConsentMessage";
        private const string PROTOCOL_CONSENT_CODE = "ConsentCode";
        private const string UNIT_TESTING_PROTOCOL_NAME = "Unit Testing Protocol";
        private const string PROTOCOL_STATUS = "Status";
        private const string PROTOCOL_STOP = "Stop";

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
            // wait for consent screen to come up
            _app.WaitForElement(PROTOCOL_START_CONSENT_MESSAGE);
            _app.Screenshot("Waiting for user consent.");

            // enter the consent code
            string consentMessage = _app.Query(PROTOCOL_START_CONSENT_MESSAGE).First().Text;
            int consentCode = int.Parse(consentMessage.Substring(consentMessage.LastIndexOf(" ") + 1));
            _app.WaitForElementThenEnterText(PROTOCOL_CONSENT_CODE, consentCode.ToString());
            _app.Screenshot("Consent code entered.");

            // start protocol and wait a bit
            _app.WaitForElementThenTap(PROTOCOL_START_NEXT_BUTTON);
            _app.WaitFor(new TimeSpan(0, 0, 10));

            // ensure that protocol has started -- indicated by the presence of the stop button
            _app.WaitForElementThenTap(UNIT_TESTING_PROTOCOL_NAME);
            _app.Screenshot("Protocol menu.");
            Assert.IsTrue(_app.Query(PROTOCOL_STOP).Any());
            _app.Back();

            CheckProtocolStatus();

            // wait for local and data stores to complete one run
            
        }

        private void CheckProtocolStatus()
        {
            _app.WaitForElementThenTap(UNIT_TESTING_PROTOCOL_NAME);
            _app.WaitForElementThenTap(PROTOCOL_STATUS);
            Func<AppQuery, AppQuery> statusLinesQuery = c => c.Class("TextCellRenderer_TextCellView").Class("TextView");
            _app.WaitForElement(statusLinesQuery);
            _app.SetOrientationLandscape();
            _app.Screenshot("Protocol status.");
            string[] errorsWarningsMisc = _app.Query(statusLinesQuery).Select(c => c.Text).ToArray();
            Assert.Equals(errorsWarningsMisc.Length, 3);
            foreach (string line in errorsWarningsMisc)
                Assert.IsEmpty(line);

            _app.Back();
            _app.SetOrientationPortrait();
        }
    }
}