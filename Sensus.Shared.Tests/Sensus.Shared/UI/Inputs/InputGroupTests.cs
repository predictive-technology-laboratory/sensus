using System.Linq;
using NUnit.Framework;
using Sensus.UI.Inputs;

namespace Sensus.Tests.UI.Inputs
{
    [TestFixture]
    public class InputGroupTests
    {
        [Test]
        public void EmptyConstructorDoesNotThrowTest()
        {
            Assert.DoesNotThrow(() => new InputGroup());
        }

        [Test]
        public void BasicPropertyTest()
        {
            var group = new InputGroup
            {
                Geotag = true,
                Name = "Name"
            };

            Assert.IsTrue(group.Valid);
            Assert.IsTrue(group.Geotag);
            Assert.IsEmpty(group.Inputs);
            Assert.AreEqual("Name", group.Name);
        }

        [Test]
        public void BasicInputTest()
        {
            var input = new SliderInput();

            var inputId = input.Id;

            var group = new InputGroup
            {
                Geotag = true,
                Inputs = { input }
            };

            Assert.AreEqual(input.Id, group.Inputs.Single().Id);
            Assert.AreSame(group.Inputs.Single(), input);
            Assert.AreEqual(group.Id, group.Inputs.Single().GroupId);            
        }

        [Test]
        public void BasicPropertyCopyTest()
        {
            var group = new InputGroup
            {
                Geotag = true,
                Name = "Name"
            };

            var copy = new InputGroup(group);

            Assert.AreEqual(group.Valid, copy.Valid);
            Assert.AreEqual(group.Geotag, copy.Geotag);
            Assert.IsEmpty(copy.Inputs);
            Assert.AreEqual(group.Name, copy.Name);
        }

        [Test]
        public void BasicInputCopyTest()
        {
            var input = new SliderInput();

            var inputId = input.Id;

            var group = new InputGroup { Inputs = { input } };
            var copy  = new InputGroup(group);

            Assert.AreEqual(1, copy.Inputs.Count());
            Assert.AreEqual(input.Id, copy.Inputs.Single().Id);
            Assert.AreNotSame(input, copy.Inputs.Single());
            Assert.AreEqual(copy.Id, copy.Inputs.Single().GroupId);
        }

        [Test]
        public void DisplayConditionInputCopyTest1()
        {
            var input = new SliderInput();

            input.DisplayConditions.Add(new InputDisplayCondition(input, InputValueCondition.Equals, "a", false));
            
            var group = new InputGroup { Inputs = { input } };
            var copy = new InputGroup(group);

            Assert.AreSame(input, input.DisplayConditions.Single().Input);
            Assert.AreSame(input, group.Inputs.Single());
            Assert.AreNotSame(group.Inputs.Single(), copy.Inputs.Single());
            Assert.AreSame(copy.Inputs.Single(), copy.Inputs.Single().DisplayConditions.Single().Input);
        }

        [Test, Explicit("This is a known fail case. Currently we handle this by manually updating all input references when we copy.")]
        public void DisplayConditionInputCopyTest2()
        {
            var input1 = new SliderInput();
            var input2 = new SliderInput();

            input1.DisplayConditions.Add(new InputDisplayCondition(input2, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input1 } };
            var copy = new InputGroup(group);

            Assert.AreSame(input2, input1.DisplayConditions.Single().Input);
            Assert.AreSame(input1, group.Inputs.Single());
            Assert.AreNotSame(group.Inputs.Single(), copy.Inputs.Single());
            Assert.AreSame(input2, copy.Inputs.Single().DisplayConditions.Single().Input);
        }
    }
}
