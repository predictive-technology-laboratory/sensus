using System.Linq;
using Xunit;
using Sensus.UI.Inputs;

namespace Sensus.Tests.UI.Inputs
{
    
    public class InputGroupTests
    {
        [Fact]
        public void EmptyConstructorDoesNotThrowTest()
        {
            Assert.DoesNotThrow(() => new InputGroup());
        }

        [Fact]
        public void BasicPropertyTest()
        {
            var group = new InputGroup
            {
                Geotag = true,
                Name = "Name"
            };

            Assert.True(group.Valid);
            Assert.True(group.Geotag);
            Assert.True(group.Inputs.Count == 0);
            Assert.Equal("Name", group.Name);
        }

        [Fact]
        public void BasicInputTest()
        {
            var input = new SliderInput();

            var inputId = input.Id;

            var group = new InputGroup
            {
                Geotag = true,
                Inputs = { input }
            };

            Assert.Equal(input.Id, group.Inputs.Single().Id);
            Assert.AreSame(group.Inputs.Single(), input);
            Assert.Equal(group.Id, group.Inputs.Single().GroupId);
        }

        [Fact]
        public void BasicPropertyCopyTest()
        {
            var group = new InputGroup
            {
                Geotag = true,
                Name = "Name"
            };

            var copy = group.Copy(false);

            Assert.Equal(group.Id, copy.Id);
            Assert.Equal(group.Valid, copy.Valid);
            Assert.Equal(group.Geotag, copy.Geotag);
            Assert.True(copy.Inputs.Count == 0);
            Assert.Equal(group.Name, copy.Name);
        }

        [Fact]
        public void BasicInputCopyTest()
        {
            var input = new SliderInput();
            var group = new InputGroup { Inputs = { input } };
            var copy = group.Copy(true);  // assign new ID as done when user taps Copy on the InputGroup.

            Assert.Equal(1, copy.Inputs.Count());
            Assert.AreNotEqual(input.Id, copy.Inputs.Single().Id);  // copied inputs should have different IDs
            Assert.AreNotSame(input, copy.Inputs.Single());
            Assert.AreNotEqual(group.Id, copy.Id);
            Assert.Equal(copy.Id, copy.Inputs.Single().GroupId);
        }

        [Fact]
        public void DisplayConditionInputCopyTest1()
        {
            var input = new SliderInput();

            input.DisplayConditions.Add(new InputDisplayCondition(input, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input } };
            var copy = group.Copy(false);

            Assert.AreSame(input, input.DisplayConditions.Single().Input);
            Assert.AreSame(input, group.Inputs.Single());
            Assert.AreNotSame(group.Inputs.Single(), copy.Inputs.Single());
            Assert.AreSame(copy.Inputs.Single(), copy.Inputs.Single().DisplayConditions.Single().Input);
        }

        [Fact]
        public void DisplayConditionInputCopyTest2()
        {
            var input1 = new SliderInput();
            var input2 = new SliderInput();

            input2.DisplayConditions.Add(new InputDisplayCondition(input2, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input1, input2 } };
            var copy = group.Copy(false);

            Assert.AreSame(input2, input2.DisplayConditions.Single().Input);
            Assert.AreSame(input1, group.Inputs.Skip(0).Take(1).Single());
            Assert.AreSame(input2, group.Inputs.Skip(1).Take(1).Single());

            Assert.AreNotSame(input1, copy.Inputs.Skip(0).Take(1).Single());
            Assert.AreNotSame(input2, copy.Inputs.Skip(1).Take(1).Single());

            Assert.AreSame(copy.Inputs.Skip(1).Take(1).Single(), copy.Inputs.Skip(1).Take(1).Single().DisplayConditions.Single().Input);
        }

        [Fact]
        public void DisplayConditionInputCopyTest3()
        {
            var input1 = new SliderInput();
            var input2 = new SliderInput();

            input2.DisplayConditions.Add(new InputDisplayCondition(input1, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input1, input2 } };
            var copy = group.Copy(false);

            Assert.AreSame(input1, input2.DisplayConditions.Single().Input);
            Assert.AreSame(input1, group.Inputs.Skip(0).Take(1).Single());
            Assert.AreSame(input2, group.Inputs.Skip(1).Take(1).Single());

            Assert.AreNotSame(input1, copy.Inputs.Skip(0).Take(1).Single());
            Assert.AreNotSame(input2, copy.Inputs.Skip(1).Take(1).Single());

            Assert.AreSame(copy.Inputs.Skip(0).Take(1).Single(), copy.Inputs.Skip(1).Take(1).Single().DisplayConditions.Single().Input);
        }

        [Fact]
        public void DisplayConditionInputCopyTest4()
        {
            var input1 = new SliderInput();
            var input2 = new SliderInput();

            input1.DisplayConditions.Add(new InputDisplayCondition(input2, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input1, input2 } };
            var copy = group.Copy(false);

            Assert.AreSame(input2, input1.DisplayConditions.Single().Input);
            Assert.AreSame(input1, group.Inputs.Skip(0).Take(1).Single());
            Assert.AreSame(input2, group.Inputs.Skip(1).Take(1).Single());

            Assert.AreNotSame(input1, copy.Inputs.Skip(0).Take(1).Single());
            Assert.AreNotSame(input2, copy.Inputs.Skip(1).Take(1).Single());

            Assert.AreSame(copy.Inputs.Skip(1).Take(1).Single(), copy.Inputs.Skip(0).Take(1).Single().DisplayConditions.Single().Input);
        }

        [Fact]
        public void DisplayConditionInputCopyTest5()
        {
            var input1 = new SliderInput();
            var input2 = new SliderInput();

            input1.DisplayConditions.Add(new InputDisplayCondition(input2, InputValueCondition.Equals, "a", false));

            var group = new InputGroup { Inputs = { input1 } };
            var copy = group.Copy(false);

            Assert.AreSame(input2, input1.DisplayConditions.Single().Input);
            Assert.AreSame(input1, group.Inputs.Single());

            Assert.AreNotSame(group.Inputs.Single(), copy.Inputs.Single());
            Assert.AreNotSame(input2, copy.Inputs.Single().DisplayConditions.Single().Input);

            Assert.Equal(input2.Id, copy.Inputs.Single().DisplayConditions.Single().Input.Id);
        }
    }
}
