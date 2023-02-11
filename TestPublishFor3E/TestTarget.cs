using NUnit.Framework;
using PublishFor3E;

namespace TestPublishFor3E
    {
    [TestFixture]
    public class TestTarget
        {
        [Test]
        public void CannotSpecifyInvalidUrl()
            {
            Assert.IsFalse(Target.TryParse(null!, out _, out _));
            Assert.IsFalse(Target.TryParse(string.Empty, out _, out _));
            Assert.IsFalse(Target.TryParse("    ", out _, out _));

            Assert.IsFalse(Target.TryParse("absolute rubbish", out _, out _));
            Assert.IsFalse(Target.TryParse("morerubbish", out _, out _));

            Assert.IsFalse(Target.TryParse("/TE_3E_TEST/web/dashboard", out _, out _));

            Assert.IsFalse(Target.TryParse("ftp://3ewapi1/TE_3E_DEMO/", out _, out _));
            Assert.IsFalse(Target.TryParse("htpps://3ewapi1/TE_3E_DEMO/", out _, out _));
            Assert.IsFalse(Target.TryParse("httpp://3ewapi1/TE_3E_DEMO/", out _, out _));
            Assert.IsFalse(Target.TryParse("httpp://3ewapi1/", out _, out _));
            Assert.IsFalse(Target.TryParse("//3ewapi1/TE_3E_DEMO/", out _, out _));

            Assert.IsFalse(Target.TryParse("http://TE_3E_DEMO/", out _, out _));

            Assert.IsFalse(Target.TryParse("http://3ewapi1/3EDEMO/", out _, out _));
            }

        [Test]
        public void CanSpecifyValidUrl1()
            {
            Assert.IsTrue(Target.TryParse("http://wapi1/TE_3E_ENV/", out Target t, out string reason));
            Assert.AreEqual("http://wapi1/TE_3E_ENV/", t!.BaseUri.ToString());
            Assert.AreEqual("TE_3E_ENV", t.Environment);
            Assert.IsNull(reason);
            }

        [Test]
        public void CanSpecifyValidUrl2()
            {
            Assert.IsTrue(Target.TryParse("https://wapi.company.com/te_3e_staging_server/web/dashboard/NxPageHome", out Target t, out string reason));
            Assert.AreEqual("https://wapi.company.com/TE_3E_STAGING_SERVER/", t!.BaseUri.ToString());
            Assert.AreEqual("TE_3E_STAGING_SERVER", t.Environment);
            Assert.IsNull(reason);
            }

        [Test]
        public void CanCompare()
            {
            Target.TryParse("https://wapi.company.com/TE_3E_STAGING_SERVER", out Target t1, out _);
            Target.TryParse("https://wapi.company.com/TE_3E_STAGING_SERVER", out Target t2, out _);
            Target.TryParse("https://wapi.company.com/te_3e_staging_server", out Target t3, out _);
            Target.TryParse("http://wapi.company.com/TE_3E_STAGING_SERVER", out Target t4, out _);

            Assert.IsTrue(t1!.Equals(t2));
            Assert.IsTrue(t1.Equals(t3));
            Assert.IsFalse(t1.Equals(t4));

            Assert.IsTrue(t1 == t2);
            Assert.IsTrue(t1 == t3);
            Assert.IsFalse(t1 == t4);

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1, t3);
            Assert.AreNotEqual(t1, t4);

            Assert.AreNotEqual(t1, null);
            Assert.AreNotEqual(null, t2);
            }
        }
    }
