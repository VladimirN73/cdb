using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cdb.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;

namespace cdb.Module.Console
{
    public static class HelperConsole
    {

        public static List<string> GetListFromString(string str)
        {
            var ret = new List<string>();
            if (!string.IsNullOrEmpty(str))
            {
                var list = str.Split(',').Select(x => x.Trim().ToLower()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                ret.AddRange(list);
            }

            return ret;
        }

        public const string SqlSeparator = "GO";

        public static IEnumerable<string> SplitSqlScript(string sqlScript)
        {
            if (sqlScript == null) sqlScript = "";

            // first normalize GO: replace '  GO  ' by 'GO'
            var lines = sqlScript.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var str = line.Trim().ToUpper();
                sb.AppendLine(str == SqlSeparator ? SqlSeparator : line);
            }

            var scriptNormalized = sb.ToString();

            return scriptNormalized.Split(new[] { SqlSeparator + "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string NormaliseEndOfLine(this string value)
        {
            var ret = value
                    .Replace("\r\n", "\n")
                    .Replace("\n\r", "\n")
                    .Replace("\r", "\n")
                    .Replace("\n", "\r\n");
            return ret;
        }


        public static bool IsEqualToTable(this string value, string tableSchemaB, string tableNameB)
        {
            return TableNamesAreEqual(value, tableSchemaB, tableNameB);
        }


        public static bool TableNamesAreEqual(string tableNameA, string tableSchemaB, string tableNameB)
        {
            var tableSchemaA = "";
            var nameParts = tableNameA.Split('.');
            if (nameParts.Length > 1)
            {
                tableSchemaA = nameParts[0];
                tableNameA = nameParts[1];
            }

            if (!string.IsNullOrEmpty(tableSchemaA))
            {
                if (!string.Equals(tableSchemaA, tableSchemaB, StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }
            }

            var ret = string.Equals(tableNameA, tableNameB, StringComparison.CurrentCultureIgnoreCase);

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
}
