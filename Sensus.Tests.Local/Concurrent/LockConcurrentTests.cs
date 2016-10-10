using NUnit.Framework;
using Sensus.Tools;

namespace Sensus.Local.Tests
{
    [TestFixture]
    public class LockConcurrentTests: IConcurrentTests
    {
        public LockConcurrentTests() : base(new LockConcurrent())
        {
            
        }
    }
}