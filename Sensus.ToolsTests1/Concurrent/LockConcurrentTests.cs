using NUnit.Framework;

namespace Sensus.Tools.Tests
{
    [TestFixture]
    public class LockConcurrentTests: IConcurrentTests
    {
        public LockConcurrentTests() : base(new LockConcurrent())
        {
            
        }
    }
}