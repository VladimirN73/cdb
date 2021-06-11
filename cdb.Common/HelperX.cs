using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace cdb.Common
{
    public static class HelperX
    {
        public const string SqlSeparator = "GO";
        public const string EOL = "\n";

        public static void AddLog(string log)
        {
            Console.Out.WriteLine(log);
            Trace.TraceInformation(log);
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

        // check if the string 'tableSchemaB.tableNameB' is equal to pattern 'value'
        // example 'dbo.Rolle' is equal to 'dbo.*'
        // example 'dbo.Rolle' is equal to 'rolle'
        public static bool IsEqualToPattern(this string strPattern, string tableSchemaB, string tableNameB)
        {
            if (strPattern == "*" ||
                strPattern.IsEqualToTable(tableSchemaB, tableNameB))
            {
                return true;
            }

            var tableSchemaA = "";
            var tableNameA = strPattern;
            var nameParts = strPattern.Split('.');
            if (nameParts.Length > 1)
            {
                tableSchemaA = nameParts[0];
                tableNameA = nameParts[1];
            }

            if (!tableSchemaA.IsNullOrEmpty() && !tableSchemaA.EqualToPattern(tableSchemaB))
            {
                return false;
            }


            if (!tableNameA.IsNullOrEmpty() && !tableNameA.EqualToPattern(tableNameB))
            {
                return false;
            }

            return true;
        }

        //consider to use regex?
        public static bool EqualToPattern(this string strPattern, string str)
        {
            if (strPattern == "*" ||
               string.Equals(strPattern, str, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }



        public static IEnumerable<string> SplitSqlScript(string sqlScript)
        {
            if (sqlScript == null) sqlScript = "";

            // first normalize GO: replace '  GO  ' by 'GO'
            var lines = sqlScript.Split(new[] { EOL }, StringSplitOptions.None);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var str = line.Trim().ToUpper();
                sb.AppendLine(str == SqlSeparator ? SqlSeparator : line);
            }

            var scriptNormalized = sb.ToString().NormaliseEndOfLine();

            return scriptNormalized.Split(new[] { SqlSeparator + EOL }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string NormaliseEndOfLine(this string value)
        {
            var ret = value
                .Replace("\r\n", "\n")
                .Replace("\n\r", "\n")
                .Replace("\r", "\n")
                .Replace("\n", EOL);
            return ret;
        }

        /// <summary>
        /// Einlesen eines Strings aus einem File unter Berücksichtigung des passenden Encodings (wg. Sonderzeichen in File-Inhalt, z.B. SQL-Inserts mit Umlauten)
        /// Dabei werden mehrere Encodings versucht und bei Vorhandensein von Nicht-ANSI Zeichen (ANSI: Code kleiner als 256) das nächste Encoding versucht
        /// </summary>
        /// <param name="fileName">Dateiname der Textdatei</param>
        /// <returns>Ausgelesener String</returns>
        public static string ReadStringFromFile(string fileName)
        {
            var encodings = new[] { Encoding.UTF8, Encoding.GetEncoding("Windows-1252") };
            string ret = null;
            foreach (var encoding in encodings)
            {
                ret = ReadStringFromFile(fileName, encoding);
                if (ContainsNonAnsiCharacters(ret) && encoding != encodings.Last())
                {
                    continue;
                }
            }

            return ret;
        }

        private static bool ContainsNonAnsiCharacters(string str)
        {
            return str.Any(x => !(x < 256));
        }

        private static string ReadStringFromFile(string fileName, Encoding encoding)
        {
            var streamReader = new StreamReader(fileName, encoding);
            var ret = streamReader.ReadToEnd();
            streamReader.Close();
            return ret;
        }

        public static IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            return results;
        }

        public static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
        {
            var ret = cols.ToDictionary(col => col, col => reader[col]);
            return ret;
        }

    }
}
