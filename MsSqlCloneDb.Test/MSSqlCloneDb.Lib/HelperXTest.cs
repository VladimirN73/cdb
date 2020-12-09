using System;
using System.Linq;
using MsSqlCloneDb.Lib;
using NUnit.Framework;

namespace MsSqlCloneDb.Test.MSSqlCloneDb.Lib
{
    [TestFixture]
    public class HelperXTest
    {

        [TestCase("GO")]
        [TestCase("GO ")]
        [TestCase("GO  ")]
        [TestCase(" GO ")]
        public void CheckScriptSplit(string strValue)
        {
            var strScript = $@"
Update
{strValue}
Select";
            var str = HelperX.SplitSqlScript(strScript).ToList();

            str.ForEach(Console.WriteLine);

            Assert.AreEqual(2, str.Count);

            
        }

        [TestCase("GO")]
        [TestCase("GO ")]
        [TestCase("GO  ")]
        [TestCase(" GO ")]
        public void CheckScriptSplit_Goto(string strValue)
        {
            var strScript = $@"
Update GOTO TOGO
{strValue}
Select";
            var str = HelperX.SplitSqlScript(strScript).ToList();

            str.ForEach(Console.WriteLine);

            Assert.AreEqual(2, str.Count);
        }

        [TestCase("GO")]
        [TestCase("GO ")]
        [TestCase("GO  ")]
        [TestCase(" GO ")]
        public void CheckScriptSplit_New_Line(string strValue)
        {
            var strScript = $"Update GOTO TOGO\n{strValue}\nSelect";

            strScript = strScript.NormaliseEndOfLine();

            var str = Helper.SplitSqlScript(strScript).ToList();

            str.ForEach(Console.WriteLine);

            Assert.AreEqual(2, str.Count);
        }

        [TestCase("EventLog",        "global", "EventLog", true)]
        [TestCase("eventlog",        "global", "EventLog", true)]
        [TestCase("global.eventLog", "global", "EventLog", true)]
        [TestCase("GLOBAL.eventLog", "global", "EventLog", true)]
        public void IsEqulaToTable(string strNameA, string tableSchemaB, string tableNameB, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, strNameA.IsEqualToTable(tableSchemaB, tableNameB));
        }

        [TestCase("*", "global", "EventLog", true)]
        [TestCase("*.eventLOG", "global", "EventLog", true)]
        [TestCase("Global.*", "global", "EventLog", true)]
        [TestCase("GLOBAL.eventLog", "global", "EventLog", true)]
        [TestCase("Rolle", "Benutzer", "Rolle", true)]
        [TestCase("Rolle", "Benutzer", "BenutzerRolle", false)]
        public void IsEqualToPattern(string strNameA, string tableSchemaB, string tableNameB, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, strNameA.IsEqualToPattern(tableSchemaB, tableNameB));
        }

    }
}
