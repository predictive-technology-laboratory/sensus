using NUnit.Framework;

namespace Sensus.Tools.Tests
{
    [TestFixture]
    public class LockConcurrentTests: IConcurrentTests
    {        
        [SetUp]
        public void BeforeEachTest()
        {
            Concurrent = new LockConcurrent();
        }
    }
}