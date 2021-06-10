using cdb.Common.Extensions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace cdb.Common.IntegrationTests
{
    [TestFixture]
    public class ConfigurationExtensionsTest : TestBase
    {
        private IConfiguration _sut;

        private TestContainer _di;

        #region test setup
        
        [OneTimeSetUp]
        public void OneSetUp()
        {
            _di = new TestContainer();

            _sut = _di.Resolve<IConfiguration>();
        }

        [SetUp]
        public void SetUp()
        {
            StartTest();
        }

        [TearDown]
        public void TearDown()
        {
            WriteElapsedTime();
        }

        #endregion


        [TestCase("dbSourceDB_", "Data Source=localhost;Initial Catalog=cdb_local_1")]
        [TestCase("dbTargetDB_", "Data Source=localhost;Initial Catalog=cdb_local_2")]
        [TestCase("local_unknown", null)]
        public void GetConnectionString(string str, string strExpected)
        {
            var ret = _sut.GetConnectionStringByKey(str);

            WriteInfo(ret);

            if (strExpected == null)
            {
                Assert.IsNull(ret);
            }
            else
            {
                Assert.IsTrue(ret.Contains(strExpected));
            }
        }
    }
}
