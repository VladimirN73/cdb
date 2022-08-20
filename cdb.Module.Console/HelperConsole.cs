using System.Collections.Generic;
using System.Linq;
using cdb.Common;
using Microsoft.Extensions.Configuration;

namespace cdb.Module.Console;

public static class HelperConsole
{

    public static List<string> GetListFromString(string str)
    {
        var ret = new List<string>();
        if (!string.IsNullOrEmpty(str))
        {
            var list = str.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            ret.AddRange(list);
        }

        return ret;
    }

    public static string Join(this IEnumerable<string> list)
    {
        return string.Join(",", list);
    }

    public static string Join(this IEnumerable<ScriptInfo> list)
    {
        return list.Select(x => x.ScriptName).Join();
    }

    public static CloneParametersExt AdaptParameters(this CloneParametersExt cloneParams, IConfiguration config)
    {
        return CloneParametersExt.AdaptParameters(cloneParams, config);
    }

    public static void PrintParameters(this CloneParametersExt config, IAppLogger logger)
    {
        CloneParametersExt.PrintParameters(config, logger);
    }

}