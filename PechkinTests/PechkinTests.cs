using Pechkin;

namespace PechkinTests
{
    public class PechkinTests : PechkinAbstractTests<IPechkin>
    {
        protected override IPechkin ProduceTestObject(GlobalConfig cfg)
        {
            Factory.UseSynchronization = false;

            return Factory.Create(cfg);
        }

        protected override void TestEnd()
        {
            
        }
    }
}
