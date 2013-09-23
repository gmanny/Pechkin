using System;
using System.IO;
using System.Reflection;
using System.Text;
using Pechkin;
using Xunit;

namespace PechkinTests
{
    public abstract class PechkinAbstractTests<TConvType> where TConvType : IPechkin
    {
        protected abstract TConvType ProduceTestObject(GlobalConfig cfg);

        protected abstract void TestEnd();

        public static string GetResourceString(string name)
        {
            if (name == null)
                return null;

            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (s == null)
                return null;

            return new StreamReader(s).ReadToEnd();
        }

        [Fact]
        public void ReturnsResultFromString()
        {
            string html = GetResourceString("PechkinTests.Resources.page.html");

            using (IPechkin c = ProduceTestObject(new GlobalConfig()))
            {
                byte[] ret = c.Convert(html);

                Assert.NotNull(ret);
            }

            TestEnd();
        }

        [Fact]
        public void ResultIsPdf()
        {
            string html = GetResourceString("PechkinTests.Resources.page.html");

            using (IPechkin c = ProduceTestObject(new GlobalConfig()))
            {
                byte[] ret = c.Convert(html);

                Assert.NotNull(ret);

                byte[] right = Encoding.UTF8.GetBytes("%PDF");

                Assert.True(right.Length <= ret.Length);

                byte[] test = new byte[right.Length];
                Array.Copy(ret, 0, test, 0, right.Length);

                for (int i = 0; i < right.Length; i++)
                {
                    Assert.Equal(right[i], test[i]);
                }
            }

            TestEnd();
        }

        [Fact]
        public void ReturnsResultFromFile()
        {
            string html = GetResourceString("PechkinTests.Resources.page.html");

            string fn = Path.GetTempFileName() + ".html";
            FileStream fs = new FileStream(fn, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            
            sw.Write(html);

            sw.Close();

            using (IPechkin c = ProduceTestObject(new GlobalConfig()))
            {
                byte[] ret = c.Convert(new ObjectConfig().SetPageUri(fn));

                Assert.NotNull(ret);
            }

            File.Delete(fn);

            TestEnd();
        }

        [Fact]
        public void OneObjectPerformsTwoConversionSequentially()
        {
            string html = GetResourceString("PechkinTests.Resources.page.html");

            using (IPechkin c = ProduceTestObject(new GlobalConfig()))
            {
                byte[] ret = c.Convert(html);

                Assert.NotNull(ret);

                ret = c.Convert(html);

                Assert.NotNull(ret);
            }

            TestEnd();
        }

        [Fact]
        public void ObjectIsHappilyGarbageCollected()
        {
            string html = GetResourceString("PechkinTests.Resources.page.html");

            IPechkin c = ProduceTestObject(new GlobalConfig());

            byte[] ret = c.Convert(html);

            Assert.NotNull(ret);

            c.Dispose();

            c = ProduceTestObject(new GlobalConfig());
            ret = c.Convert(html);

            Assert.NotNull(ret);
            
            GC.Collect();

            c.Dispose();

            TestEnd();
        }
    }
}