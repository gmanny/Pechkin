using System;
using System.Collections.Specialized;
using System.Threading;
using Pechkin;
using PechkinTests;
using Xunit;

namespace PechkinSynchronizedTests
{
    public class PechkinSynchronizedTestsInitClass : IDisposable
    {
        public void Dispose() // deinit library
        {

        }
    }

    public class PechkinSynchronizedTests : PechkinAbstractTests<IPechkin>, IUseFixture<PechkinSynchronizedTestsInitClass>
    {
        protected override IPechkin ProduceTestObject(GlobalConfig cfg)
        {
            Factory.UseSynchronization = true;
            Factory.UseDynamicLoading = true;

            return Factory.Create(cfg);
        }

        protected override void TestEnd()
        {
        }

        public void SetFixture(PechkinSynchronizedTestsInitClass data)
        {
            
        }

        private static int _threadId;

        [Fact]
        public void ThreadedRequestsGetProcessed()
        {
            // oh well, this test doesn't fail if it does, because there's no assertions or exceptions (or anything) in threads
            // but log and debug should show anomalies (and todo I should assert on them)

            NameValueCollection properties = new NameValueCollection();
            properties["showDateTime"] = "true";

            ThreadStart ts = () =>
                                 {
                                     lock (this)
                                     {
                                         Thread.CurrentThread.Name = "test thread " + (_threadId++).ToString();
                                     }

                                     string html = GetResourceString("PechkinTests.Resources.page.html");

                                     IPechkin c = ProduceTestObject(new GlobalConfig());

                                     byte[] ret = c.Convert(html);

                                     Assert.NotNull(ret);

                                     c.Dispose();
                                 };

            Thread t = new Thread(ts);
            Thread t2 = new Thread(ts);

            t.Start();
            Thread.Sleep(1000);
            t2.Start();

            GC.Collect();

            Thread.Sleep(6000);

            GC.Collect();

            Thread.Sleep(10000);

            TestEnd();
        }
    }
}
