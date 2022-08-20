using System.Data;
using cdb.Common.Extensions;
using NUnit.Framework;

namespace cdb.Common.UnitTests;

[TestFixture]
public class ConfigurationExtensionsTest
{
    [TestCase("Snapshot", IsolationLevel.Snapshot)]
    [TestCase("x-x-x", IsolationLevel.Snapshot)]
    //[TestCase("read commited", IsolationLevel.ReadCommitted)]
    [TestCase("Read Committed", IsolationLevel.ReadCommitted)]
    [TestCase("ReadCommitted", IsolationLevel.ReadCommitted)]
    public void ToEnum_Test(string strValue, IsolationLevel isolationLevel)
    {
        var temp = strValue.ToEnum(IsolationLevel.Snapshot);

        Assert.AreEqual(isolationLevel, temp);
    }
}