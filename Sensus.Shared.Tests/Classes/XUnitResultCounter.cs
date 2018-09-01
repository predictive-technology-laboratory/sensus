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
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Runners;
using System.Linq;

namespace Sensus.Tests.Classes
{
    public class XUnitResultCounter : IResultChannel
    {
        public event EventHandler<string> ResultsDelivered;

        private Dictionary<TestState, int> _testStateCount;

        public async Task CloseChannel()
        {
            // there's an issue where CloseChannel is called before all of the results come though RecordResult. wait a bit for everything to come through.
            await Task.Delay(5000);

            ResultsDelivered?.Invoke(this, string.Concat(_testStateCount.OrderBy(stateCount => stateCount.Key).Select(stateCount => stateCount.Key + ":" + stateCount.Value + Environment.NewLine)).Trim());
        }

        public Task<bool> OpenChannel(string message = null)
        {
            _testStateCount = new Dictionary<TestState, int>();

            return Task.FromResult(true);
        }

        public void RecordResult(TestResultViewModel result)
        {
            TestState testState = result.TestCase.Result;

            if (_testStateCount.ContainsKey(testState))
            {
                _testStateCount[testState]++;
            }
            else
            {
                _testStateCount.Add(testState, 1);
            }
        }
    }
}
