using NUnit.Framework;
using PublishFor3E;

namespace TestPublishFor3E
    {
    [TestFixture]
    internal class TestSaveSettings
        {
        [Test]
        public void TestInitialSave()
            {
            Target.TryParse("http://mywapiserver/TE_3E_UNITTEST", out Target? target, out _);
            var pp = new PublishParameters(target!);
            pp.AddWapis(new [] {"one", "two", "three"});

            File.Delete(Program.SettingsFile());
            Program.SavePublishParameters(pp);

            var actual = Program.LoadPublishParameters("TE_3E_UNITTEST");
            Assert.IsNotNull(actual);

            Assert.AreEqual(pp, actual);

            actual = Program.LoadPublishParameters("unit");
            Assert.IsNotNull(actual);

            Assert.AreEqual(pp, actual);

            actual = Program.LoadPublishParameters("test");
            Assert.IsNotNull(actual);

            Assert.AreEqual(pp, actual);

            actual = Program.LoadPublishParameters("invalid");
            Assert.IsNull(actual);
            }

        [Test]
        public void TestUpdate()
            {
            TestInitialSave();

            Target.TryParse("http://otherserver/TE_3E_UNITTEST", out Target? target, out _);
            var pp = new PublishParameters(target!);
            pp.AddWapis(new [] {"monday", "tuesday"});

            Program.SavePublishParameters(pp);

            var actual = Program.LoadPublishParameters("TE_3E_UNITTEST");
            Assert.IsNotNull(actual);

            Assert.AreEqual(pp, actual);
            }
        }
    }
