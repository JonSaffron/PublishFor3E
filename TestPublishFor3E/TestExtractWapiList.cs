using PublishFor3E;
using NUnit.Framework;

namespace TestPublishFor3E
    {
    [TestFixture]
    internal class TestExtractWapiList
        {
        [Test]
        public void TestWorksWithPipes()
            {
            var output = Program.ExtractWapiList("one|two|three|four");

            Assert.AreEqual(new[] {"one", "two", "three", "four"}, output);
            }

        [Test]
        public void TestWorksWithCommas()
            {
            var output = Program.ExtractWapiList("red,blue,green");

            Assert.AreEqual(new[] {"red", "blue", "green"}, output);
            }

        [Test]
        public void TestWorksWithSemiCommas()
            {
            var output = Program.ExtractWapiList("monday;tuesday;wednesday");

            Assert.AreEqual(new[] {"monday", "tuesday", "wednesday"}, output);
            }

        [Test]
        public void TestNoObviousSeparator()
            {
            var output = Program.ExtractWapiList("once.upon.a.time");

            Assert.AreEqual(new[] {"once.upon.a.time"}, output);
            }
        }
    }
