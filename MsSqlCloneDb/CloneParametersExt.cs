using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using MsSqlCloneDb.Lib;
using MsSqlCloneDb.TextProcessor;

namespace MsSqlCloneDb
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CloneParametersExt : CloneParameters
    {
        private const string param_dbSource = "dbSource";
        private const string param_dbTarget = "dbTarget";
        private const string param_skipTables = "skipTables";
        private const string param_restoreTables = "restoreTables";
        private const string param_mergeTables = "mergeTables";
        private const string param_mergeSchemaScripts = "mergeSchemaScripts";
        private const string param_mergeScripts = "mergeScripts";
        private const string param_finalScripts = "finalScripts";
        private const string param_updateScripts = "updateScripts";

        public string strSkipTables;
        public string strRestoreTables;
        public string strMergeTables;
        public string strMergeSchemaScripts;
        public string strMergeScripts;
        public string strUpdateScripts;
        public string strFinalScripts;

        public static CloneParametersExt GetParameters(string[] parameterArray)
        {
            var ret = new CloneParametersExt();

            CmdParameterParser.TryGetParameterValue(parameterArray, param_dbSource, out ret.dbSource);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_dbTarget, out ret.dbTarget);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_skipTables, out ret.strSkipTables);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_restoreTables, out ret.strRestoreTables);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_mergeTables, out ret.strMergeTables);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_mergeSchemaScripts, out ret.strMergeSchemaScripts);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_mergeScripts, out ret.strMergeScripts);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_finalScripts, out ret.strFinalScripts);
            CmdParameterParser.TryGetParameterValue(parameterArray, param_updateScripts, out ret.strUpdateScripts);

            return ret;
        }

        private const int shift = 30;
        public static void PrintParameters(CloneParametersExt parameterObject, ILogSink logger)
        {
            var par = parameterObject; // for shortness

            logger.AddLogEntry($@"{param_dbSource,shift}: {par.dbSource}");
            logger.AddLogEntry($@"{param_dbTarget,shift}: {par.dbTarget}");
            logger.AddLogEntry($@"{param_skipTables,shift}: {par.skipTables.Join()}");
            logger.AddLogEntry($@"{param_restoreTables,shift}: {par.restoreTables.Join()}");
            logger.AddLogEntry($@"{param_mergeTables,shift}: {par.mergeTables.Join()}");
            logger.AddLogEntry($@"{param_mergeSchemaScripts,shift}: {par.mergeSchemaScripts.Join()}");
            logger.AddLogEntry($@"{param_mergeScripts,shift}: {par.mergeScripts.Join()}");
            logger.AddLogEntry($@"{param_finalScripts,shift}: {par.finalScripts.Join()}");
            logger.AddLogEntry($@"{param_updateScripts,shift}: {par.updateScripts.Join()}");
        }

        // Wenn einige Command-Line-Parameters fehlen
        // dann diese aus App.Config nachladen
        //
        // Connection-Strings prüfen bzw. anpassen
        //
        public static CloneParametersExt AdaptParameters(CloneParametersExt config)
        {
            var ret = config;

            // =================================================
            // db Source
            //
            var connSource = Helper.GetConnectionString(ret.dbSource);
            ret.dbSourceConnectionString = connSource ?? ret.dbSource;

            // =================================================
            // db Target
            //
            var connTarget = Helper.GetConnectionString(ret.dbTarget);
            ret.dbTargetConnectionString = connTarget ?? ret.dbTarget;

            // =================================================
            //
            if (ret.strSkipTables == null)
            {
                ret.strSkipTables = GetFromSettings(param_skipTables);
            }

            // =================================================
            //
            if (ret.strRestoreTables == null)
            {
                ret.strRestoreTables = GetFromSettings(param_restoreTables);
            }

            // =================================================
            //
            if (ret.strMergeTables == null)
            {
                ret.strMergeTables = GetFromSettings(param_mergeTables);
            }

            // =================================================
            //
            if (ret.strUpdateScripts == null)
            {
                ret.strUpdateScripts = GetFromSettings(param_updateScripts);
            }

            // =================================================
            //
            if (ret.strMergeSchemaScripts == null)
            {
                ret.strMergeSchemaScripts= GetFromSettings(param_mergeSchemaScripts);
            }

            // =================================================
            //
            if (ret.strMergeScripts == null)
            {
                ret.strMergeScripts = GetFromSettings(param_mergeScripts);
            }

            // =================================================
            //
            if (ret.strFinalScripts == null)
            {
                ret.strFinalScripts = GetFromSettings(param_finalScripts);
            }
            

            // ==================================================

            config.skipTables    = Helper.GetListFromString(config.strSkipTables);
            config.restoreTables = Helper.GetListFromString(config.strRestoreTables);
            config.mergeTables   = Helper.GetListFromString(config.strMergeTables);

            config.updateScripts = GetScriptsByPatternString(config.strUpdateScripts);
            config.mergeSchemaScripts = GetScriptsByPatternString(config.strMergeSchemaScripts);
            config.mergeScripts  = GetScriptsByPatternString(config.strMergeScripts);
            config.finalScripts  = GetScriptsByPatternString(config.strFinalScripts);

          // ==================================================
            var collection = PartialTranferHelper.NameValueCollection;
            foreach (var item in collection)
            {
                var key = item.ToString();
                config.PartialTransfer.Add(new PartialTableTranfer{TableName = key , WhereCondition = collection.Get(key)});
            }

            return ret;
        }


        public static CloneParametersExt ReplaceVariablesInFinalScripts(CloneParametersExt config, ILogSink logger)
        {
            var variablesProvider = new VariableProvider();

            SetVariables(variablesProvider, config);

            var textProcessor = new SimpleTextProcessor();
            textProcessor.Init(variablesProvider);
            foreach (var script in config.finalScripts)
            {
                script.ScriptText = textProcessor.GetText(script.ScriptText);
            }

            variablesProvider.PrintVariables(logger);

            return config;
        }

        public static void SetVariables(VariableProvider variableProvider, CloneParametersExt config)
        {
            var connString = config.dbSourceConnectionString;
            if (!connString.IsNullOrEmpty())
            {
                var temp = new SqlConnectionStringBuilder(connString);
                variableProvider.Add(VariableProvider.VAR_SOURCE_DB, temp.InitialCatalog);
            }

            connString = config.dbTargetConnectionString;
            if (!connString.IsNullOrEmpty())
            {
                var temp = new SqlConnectionStringBuilder(connString);
                variableProvider.Add(VariableProvider.VAR_TARGET_DB, temp.InitialCatalog);
            }
        }


        private static List<ScriptInfo> GetScriptsByPatternString(string strPatterns)
        {
            var listFiles = GetFilesByPatternString(strPatterns);
            return GetScriptsByFileList(listFiles);
        }

        private static List<ScriptInfo> GetScriptsByFileList(List<string> scriptFiles)
        {
            var ret = new List<ScriptInfo>();
            foreach (var item in scriptFiles)
            {
                var str = GetScriptFromFile(item);

                if (!str.IsNullOrEmpty())
                {

                    ret.Add(new ScriptInfo{ScriptName = item, ScriptText = str});
                }                
            }

            return ret;
        }

        private static string GetScriptFromFile(string fileName)
        {
            AddLog($"Script '{fileName}' wird eingelesen");
            if (!File.Exists(fileName))
            {
                AddLog($"WARNING: File '{fileName}' not found");
                return null;
            }

            var streamReader = new StreamReader(fileName);
            var ret = streamReader.ReadToEnd();
            streamReader.Close();

            return ret;
        }


        // TODO move to protected as long the corresponding tests are changed
        public static List<string> GetFilesByPatternString(string strPatterns)
        {
            var patterns = Helper.GetListFromString(strPatterns);
            return GetFilesByPatternList(patterns);
        }

        protected static List<string> GetFilesByPatternList(List<string> patterns)
        {
            var ret = new List<string>();

            foreach (var pattern in patterns)
            {
                AddLog($"process file-pattern '{pattern}'");
                if (pattern.Contains("*"))
                {
                    var iLastSlash = pattern.LastIndexOf(@"\", StringComparison.Ordinal);
                    var folder = pattern.Substring(0, iLastSlash + 1);
                    var filePattern = pattern.Substring(iLastSlash + 1, pattern.Length - iLastSlash - 1);

                    if (Directory.Exists(folder))
                    {
                        var subList = Directory.GetFiles(folder, filePattern)
                            .OrderBy(x => x)
                            .ToList();
                        ret.AddRange(subList);
                    }
                    else
                    {
                        AddLog($"WARNING: Folder '{folder}' not found");
                    }
                }
                else
                {
                    ret.Add(pattern);
                }
            }

            return ret;
        }

        public static void DecryptParameters(CloneParameters parameterObject, ILogSink logger)
        {
            var strMethod = $@"{nameof(DecryptParameters)}";
            logger.AddLogEntry($"--> {strMethod}");

            var par = parameterObject; // for shortness

            par.dbSourceConnectionString = Helper.GetDecryptedConnectionString(par.dbSourceConnectionString);
            par.dbTargetConnectionString = Helper.GetDecryptedConnectionString(par.dbTargetConnectionString);

            logger.AddLogEntry($"<-- {strMethod}");
        }

        private static string GetFromSettings(string strSetting)
        {
            string ret = null;

            if (ConfigurationManager.AppSettings[strSetting] != null)
                ret = ConfigurationManager.AppSettings[strSetting];

            return ret;
        }

        private static void AddLog(string log)
        {
            Console.Out.WriteLine(log);
            Trace.TraceInformation(log);
        }
    }


    public class PartialTranferHelper
    {
        public static  NameValueCollection NameValueCollection =>
            _nameValueCollection
            ?? (_nameValueCollection = ConfigurationManager.GetSection("PartialTableTransfer") as NameValueCollection)
            ?? new NameValueCollection();

        private static NameValueCollection _nameValueCollection;
    }
}
