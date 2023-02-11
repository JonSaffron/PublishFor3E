using System.IO;
using NUnit.Framework;
using PublishFor3E;

namespace TestPublishFor3E
    {
    [TestFixture]
    internal class TestSaveSettings
        {
        private PublishParameters _paramsForUnitTest;
        private PublishParameters _paramsForDev;
        private PublishParameters _paramsForDev2;

        [Test]
        public void TestInitialSave()
            {
            InitialSetup();

            var actual = StoredSettings.LoadPublishParameters("TE_3E_UNITTEST");
            Assert.IsNotNull(actual);

            Assert.AreEqual(this._paramsForUnitTest, actual);

            actual = StoredSettings.LoadPublishParameters("unit");
            Assert.IsNotNull(actual);

            Assert.AreEqual(this._paramsForUnitTest, actual);

            actual = StoredSettings.LoadPublishParameters("test");
            Assert.IsNotNull(actual);

            Assert.AreEqual(this._paramsForUnitTest, actual);

            actual = StoredSettings.LoadPublishParameters("invalid");
            Assert.IsNull(actual);
            }

        [Test]
        public void TestUpdate()
            {
            InitialSetup();

            Target target = Target.Parse("http://otherserver/TE_3E_UNITTEST");
            var pp = new PublishParameters(target!);
            pp.AddWapis(new [] {"monday", "tuesday"});

            StoredSettings.SavePublishParameters(pp);

            var actual = StoredSettings.LoadPublishParameters("TE_3E_UNITTEST");
            Assert.IsNotNull(actual);

            Assert.AreEqual(pp, actual);
            }

        [Test]
        public void TestExactMatch()
            {
            InitialSetup();
            
            var actual = StoredSettings.LoadPublishParameters("dev");
            Assert.IsNull(actual);

            actual = StoredSettings.LoadPublishParameters("TE_3E_DEV");
            Assert.AreEqual(this._paramsForDev, actual);
            }

        private void InitialSetup()
            {
            File.Delete(StoredSettings.PathToSettingsFile());
            Target target;

            target = Target.Parse("http://mywapiserver/TE_3E_UNITTEST");
            this._paramsForUnitTest = new PublishParameters(target!);
            this._paramsForUnitTest.AddWapis(new [] {"one", "two", "three"});
            StoredSettings.SavePublishParameters(this._paramsForUnitTest);

            target = Target.Parse("http://mywapiserver/TE_3E_DEV");
            this._paramsForDev = new PublishParameters(target!);
            this._paramsForDev.AddWapis(new [] {"one", "two", "three"});
            StoredSettings.SavePublishParameters(this._paramsForDev);

            target = Target.Parse("http://mywapiserver/TE_3E_DEV2");
            this._paramsForDev2 = new PublishParameters(target!);
            this._paramsForDev2.AddWapis(new [] {"one", "two", "three"});
            StoredSettings.SavePublishParameters(this._paramsForDev2);
            }
        }
    }
