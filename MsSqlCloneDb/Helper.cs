using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MsSqlCloneDb.Lib;
using MsSqlCloneDb.Lib.Common;

namespace MsSqlCloneDb
{
    public static class Helper
    {
        private static List<ConnectionStringSettings> _listConnections;
        public static List<ConnectionStringSettings> ListConnections
        {
            get
            {
                if (_listConnections == null)
                {
                    _listConnections = new List<ConnectionStringSettings>();
                    foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                    {
                        _listConnections.Add(connectionString);
                    }
                }
                return _listConnections;
            }
        }

        public static string GetConnectionString(string connectionName)
        {
            string ret = null;

            var connectionStringSetting = ListConnections.FirstOrDefault(x => String.Equals(x.Name, connectionName, StringComparison.CurrentCultureIgnoreCase));

            if (connectionStringSetting != null)
            {
                ret = connectionStringSetting.ConnectionString;
            }
            return ret;
        }

        public static string GetDecryptedConnectionString(string str)
        {
            if (str.IsNullOrEmpty()) return str;

            var connString = GetDecrypted(str);

            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connString);

            if (sqlConnectionStringBuilder.Password.IsNullOrEmpty())
            {
                return sqlConnectionStringBuilder.ConnectionString;
            }

            sqlConnectionStringBuilder.Password = GetDecrypted(sqlConnectionStringBuilder.Password);

            return sqlConnectionStringBuilder.ConnectionString;
        }

        public static string GetDecrypted(string str)
        {
            var ret = str;
            try
            {
                if (str != null)
                {
                    ret = EncryptionServiceFactory.Create().Decrypt(str);
                }
            }
#pragma warning disable 168
            catch (Exception ex)
#pragma warning restore 168
            {
                // ignore. if the string could not be decrypted it maybe was just not encrypted in the first place.
            }
            return ret;
        }

        public static List<string> GetListFromString(string str)
        {
            var ret = new List<string>();
            if (!String.IsNullOrEmpty(str))
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
            var lines = sqlScript.Split(new[] {"\r\n"}, StringSplitOptions.None);
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

        [DebuggerStepThrough]
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
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
    }
}
