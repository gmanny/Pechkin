using System;
using Pechkin;
using Pechkin.Synchronized;
using PechkinTests;
using Xunit;

namespace PechkinSynchronizedTests
{
    public class PechkinSynchronizedTestsInitClass : IDisposable
    {
        public void Dispose() // deinit library
        {
            SynchronizedPechkin.ClearBeforeExit();
        }
    }

    public class PechkinSynchronizedTests : PechkinAbstractTests<SynchronizedPechkin>, IUseFixture<PechkinSynchronizedTestsInitClass>
    {
        protected override SynchronizedPechkin ProduceTestObject(GlobalConfig cfg)
        {
            return new SynchronizedPechkin(cfg);
        }

        protected override void TestEnd()
        {
        }

        public void SetFixture(PechkinSynchronizedTestsInitClass data)
        {
            
        }
    }
}
