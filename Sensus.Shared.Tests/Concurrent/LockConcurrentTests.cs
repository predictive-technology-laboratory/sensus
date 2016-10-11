using NUnit.Framework;
using Sensus.Local.Tests;
using Sensus.Tools;

namespace Sensus.Tests.Local.Concurrent
{
    [TestFixture]
    public class LockConcurrentTests: IConcurrentTests
    {
        public LockConcurrentTests() : base(new LockConcurrent())
        {
            
        }
    }
}