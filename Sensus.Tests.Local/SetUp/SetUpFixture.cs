using NUnit.Framework;
using Sensus.Context;
using Sensus.Test.Classes;

namespace Sensus.Tests //this namespace has to align with the tests we want this fixture for
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void SetUp()
        {
            SensusContext.Current = new TestSensusContext();
        }
    }
}