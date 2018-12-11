//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System.Linq;

namespace Sensus.Tests.Probes.User.Scripts
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

            Assert.Same(script.Runner, copy.Runner);
            Assert.Equal(script.Id, copy.Id);
            Assert.Equal(script.InputGroups.Single().Id, copy.InputGroups.Single().Id);
            Assert.Equal(script.InputGroups.Single().Inputs.First().Id, copy.InputGroups.Single().Inputs.First().Id);
            Assert.Equal(script.InputGroups.Count, copy.InputGroups.Count);
            Assert.Equal(script.InputGroups.Single().Inputs.Count, copy.InputGroups.Single().Inputs.Count);
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

            Assert.Same(script.Runner, copy.Runner);
            Assert.NotEqual(script.Id, copy.Id);
            Assert.Equal(script.InputGroups.Single().Id, copy.InputGroups.Single().Id);
            Assert.Equal(script.InputGroups.Single().Inputs.First().Id, copy.InputGroups.Single().Inputs.First().Id);    
            Assert.Equal(script.InputGroups.Count, copy.InputGroups.Count);
            Assert.Equal(script.InputGroups.Single().Inputs.Count, copy.InputGroups.Single().Inputs.Count);
        }
    }
}
