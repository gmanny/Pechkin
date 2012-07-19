using Pechkin;

namespace PechkinTests
{
    public class PechkinTests : PechkinAbstractTests<SimplePechkin>
    {
        protected override SimplePechkin ProduceTestObject(GlobalConfig cfg)
        {
            return new SimplePechkin(cfg);
        }

        protected override void TestEnd()
        {
            
        }
    }
}
