using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace cdb.Common
{
    public class CloneParameters
    {
        public string dbSource;
        public string dbTarget;
        public string dbSourceConnectionString;
        public string dbTargetConnectionString;

        public string IsolationLevel = System.Data.IsolationLevel.Snapshot.ToString();

        public List<string> skipTables = new List<string>();
        public List<string> onlyTables = new List<string>();
        public List<string> restoreTables = new List<string>();
        public List<ScriptInfo> updateScripts = new List<ScriptInfo>();
        public List<ScriptInfo> finalScripts = new List<ScriptInfo>();

        public List<PartialTableTranfer> PartialTransfer = new List<PartialTableTranfer>();

        public string schemaFile;

        public bool SkipAllTables =>
            skipTables.Any(x => x.Equals("*", StringComparison.InvariantCultureIgnoreCase)) &&
            PartialTransfer.Count < 1;
    }

    public class ScriptInfo
    {
        public string ScriptName;
        public string ScriptText;
    }

    public class PartialTableTranfer
    {
        public string TableName;
        public string WhereCondition;
    }
}
