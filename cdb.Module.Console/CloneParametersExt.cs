using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using cdb.Common;
using cdb.Common.Extensions;
using Microsoft.Extensions.Configuration;

// ReSharper disable InconsistentNaming

namespace cdb.Module.Console;

public class CloneParametersExt : CloneParameters
{
    private const string param_dbSource = "dbSource";
    private const string param_dbTarget = "dbTarget";
    private const string param_skipTables = "skipTables";
    private const string param_onlyTables = "onlyTables";
    private const string param_restoreTables = "restoreTables";
    private const string param_finalScripts = "finalScripts";
    private const string param_updateScripts = "updateScripts";

    private const string param_IsolationLevel = "IsolationLevel";

    public string strSkipTables;
    public string strOnlyTables;
    public string strRestoreTables;
    public string strUpdateScripts;
    public string strFinalScripts;

    public static CloneParametersExt GetParameters(string[] parameterArray, ICmdParameterParser parser)
    {
        var ret = new CloneParametersExt();

        parser.TryGetParameterValue(parameterArray, param_dbSource, out ret.dbSource);
        parser.TryGetParameterValue(parameterArray, param_dbTarget, out ret.dbTarget);
        parser.TryGetParameterValue(parameterArray, param_skipTables, out ret.strSkipTables);
        parser.TryGetParameterValue(parameterArray, param_onlyTables, out ret.strOnlyTables);
        parser.TryGetParameterValue(parameterArray, param_restoreTables, out ret.strRestoreTables);
        parser.TryGetParameterValue(parameterArray, param_finalScripts, out ret.strFinalScripts);
        parser.TryGetParameterValue(parameterArray, param_updateScripts, out ret.strUpdateScripts);

        parser.TryGetParameterValue(parameterArray, param_IsolationLevel, out ret.IsolationLevel);

        return ret;
    }

    private const int shift = 30;
    public static void PrintParameters(CloneParametersExt parameterObject, IAppLogger logger)
    {
        var par = parameterObject; // for shortness
        var str = new StringBuilder();

        str.AppendLine($@"{param_dbSource,shift}: {par.dbSource}");
        str.AppendLine($@"{param_dbTarget,shift}: {par.dbTarget}");
        str.AppendLine($@"{param_skipTables,shift}: {par.skipTables.Join()}");
        str.AppendLine($@"{param_onlyTables,shift}: {par.onlyTables.Join()}");
        str.AppendLine($@"{param_restoreTables,shift}: {par.restoreTables.Join()}");
        str.AppendLine($@"{param_finalScripts,shift}: {par.finalScripts.Join()}");
        str.AppendLine($@"{param_updateScripts,shift}: {par.updateScripts.Join()}");
        str.AppendLine($@"{param_IsolationLevel,shift}: {par.IsolationLevel}");
        logger.Log(str.ToString());
    }

    // if some Command-Line-Parameters are mising
    // then try to load it from the config-file
    //
    // Connection-Strings check/adapt
    //
    public static CloneParametersExt AdaptParameters(CloneParametersExt cloneParams, IConfiguration config)
    {

        // =================================================
        // db Source
        //
        var connSource = config.GetConnectionStringByKey(cloneParams.dbSource);
        cloneParams.dbSourceConnectionString = connSource ?? cloneParams.dbSource;

        // =================================================
        // db Target
        //
        var connTarget = config.GetConnectionStringByKey(cloneParams.dbTarget);
        cloneParams.dbTargetConnectionString = connTarget ?? cloneParams.dbTarget;

        // ==================================================

        cloneParams.skipTables = HelperConsole.GetListFromString(cloneParams.strSkipTables);
        cloneParams.onlyTables = HelperConsole.GetListFromString(cloneParams.strOnlyTables);

        cloneParams.restoreTables = HelperConsole.GetListFromString(cloneParams.strRestoreTables);

        cloneParams.updateScripts = GetScriptsByPatternString(cloneParams.strUpdateScripts);
        cloneParams.finalScripts = GetScriptsByPatternString(cloneParams.strFinalScripts);

        return cloneParams;
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

                ret.Add(new ScriptInfo { ScriptName = item, ScriptText = str });
            }
        }

        return ret;
    }

    private static string GetScriptFromFile(string fileName)
    {
        AddLog($"Load the script '{fileName}'");
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

    public static List<string> GetFilesByPatternString(string strPatterns)
    {
        var patterns = HelperConsole.GetListFromString(strPatterns);
        return GetFilesByPatternList(patterns);
    }

    protected static List<string> GetFilesByPatternList(List<string> patterns)
    {
        var ret = new List<string>();

        const string fileNameSeparator = "/";

        foreach (var item in patterns)
        {
            AddLog($"process file-pattern '{item}'");

            var pattern = item
                .Replace(@"\", fileNameSeparator)
                .Replace(@"/", fileNameSeparator);

            if (pattern.Contains("*"))
            {
                var iLastSlash = pattern.LastIndexOf(fileNameSeparator, StringComparison.Ordinal);
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

    private static void AddLog(string log)
    {
        System.Console.Out.WriteLine(log);
    }
}