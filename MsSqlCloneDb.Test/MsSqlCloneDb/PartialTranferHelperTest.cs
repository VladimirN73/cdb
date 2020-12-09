using System.Collections.Generic;
using MsSqlCloneDb.Lib;
using NUnit.Framework;

namespace MsSqlCloneDb.Test.MsSqlCloneDb
{
    [TestFixture]
    public class PartialTranferHelperTest
    {
        [Test]
        public void NameValueCollection()
        {
            var collection = PartialTranferHelper.NameValueCollection;

            foreach (var item in collection)
            {
                var name = item.ToString();
                var value = collection.Get(name);
            }

            Assert.AreEqual(2, collection.Count);
        }
    }
}
