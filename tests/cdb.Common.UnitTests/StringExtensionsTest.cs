using NUnit.Framework;

namespace cdb.Common.UnitTests;

[TestFixture]
public class StringExtensionsTest
{
    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase(" ", false)]
    [TestCase("a", false)]
    [TestCase(" a ", false)]
    public void IsNullOrEmpty(string strValue, bool expectedValue)
    {
        var real = strValue.IsNullOrEmpty(); 

        Assert.AreEqual(expectedValue, real);
    }
}