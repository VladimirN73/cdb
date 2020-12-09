using NUnit.Framework;

namespace MsSqlCloneDb.Test.MsSqlCloneDb
{
    [TestFixture]
    public class HelperTest
    {
        [TestCase("Data Source=host;Initial Catalog=db_name;User ID=user;Password=pwd", "Data Source=host;Initial Catalog=db_name;User ID=user;Password=pwd")]
        [TestCase("", "")]
        [TestCase("Data Source=host;Initial Catalog=db_name;User ID=user;Password=", "Data Source=host;Initial Catalog=db_name;User ID=user")]
        public void GetDecryptedConnectionString(string connString, string strExpected)
        {
            var str = Helper.GetDecryptedConnectionString(connString);

            Assert.AreEqual(strExpected, str);
        }
    }
}
