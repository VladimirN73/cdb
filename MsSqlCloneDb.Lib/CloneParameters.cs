using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MsSqlCloneDb.Lib
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CloneParameters
    {
        public string dbSource;
        public string dbTarget;
        public string dbSourceConnectionString;
        public string dbTargetConnectionString;

        public List<string> skipTables = new List<string>();
        public List<string> restoreTables = new List<string>();
        public List<string> mergeTables = new List<string>();
        public List<ScriptInfo> mergeSchemaScripts = new List<ScriptInfo>();
        public List<ScriptInfo> mergeScripts = new List<ScriptInfo>();
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
}
