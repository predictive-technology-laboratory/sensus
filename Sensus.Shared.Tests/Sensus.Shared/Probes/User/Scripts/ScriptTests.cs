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
using Xunit;
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System.Linq;

namespace Sensus.Tests.Sensus.Shared.Probes.User.Scripts
{
    
    public class ScriptTests
    {
        [Fact]
        public void ScriptCopySameIdTest()
        {
            ScriptProbe probe = new ScriptProbe();
            ScriptRunner runner = new ScriptRunner("test", probe);
            Script script = new Script(runner);
            InputGroup group = new InputGroup();
            Input input1 = new SliderInput();
            Input input2 = new SliderInput();
            group.Inputs.Add(input1);
            group.Inputs.Add(input2);
            script.InputGroups.Add(group);

            Script copy = script.Copy(false);

            Assert.AreSame(script.Runner, copy.Runner);
            Assert.AreEqual(script.Id, copy.Id);
            Assert.AreEqual(script.InputGroups.Single().Id, copy.InputGroups.Single().Id);
            Assert.AreEqual(script.InputGroups.Single().Inputs.First().Id, copy.InputGroups.Single().Inputs.First().Id);
            Assert.AreEqual(script.InputGroups.Count, copy.InputGroups.Count);
            Assert.AreEqual(script.InputGroups.Single().Inputs.Count, copy.InputGroups.Single().Inputs.Count);
        }

        [Fact]
        public void ScriptCopyNewIdTest()
        {
            ScriptProbe probe = new ScriptProbe();
            ScriptRunner runner = new ScriptRunner("test", probe);
            Script script = new Script(runner);
            InputGroup group = new InputGroup();
            Input input1 = new SliderInput();
            Input input2 = new SliderInput();
            group.Inputs.Add(input1);
            group.Inputs.Add(input2);
            script.InputGroups.Add(group);

            Script copy = script.Copy(true);

            Assert.AreSame(script.Runner, copy.Runner);
            Assert.AreNotEqual(script.Id, copy.Id);
            Assert.AreEqual(script.InputGroups.Single().Id, copy.InputGroups.Single().Id);
            Assert.AreEqual(script.InputGroups.Single().Inputs.First().Id, copy.InputGroups.Single().Inputs.First().Id);    
            Assert.AreEqual(script.InputGroups.Count, copy.InputGroups.Count);
            Assert.AreEqual(script.InputGroups.Single().Inputs.Count, copy.InputGroups.Single().Inputs.Count);
        }
    }
}
