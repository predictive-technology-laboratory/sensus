using NUnit.Framework;
using Sensus.Tools;

namespace Sensus.Shared.Tests.Concurrent
{
    [TestFixture]
    public class LockConcurrentTests: IConcurrentTests
    {
        public LockConcurrentTests() : base(new LockConcurrent())
        {
            
        }
    }
}