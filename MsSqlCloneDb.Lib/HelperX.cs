using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MsSqlCloneDb.Lib
{
    internal static class HelperX
    {
        public const string SqlSeparator = "GO";


        public static void AddLog(string log)
        {
            Console.Out.WriteLine(log);
            Trace.TraceInformation(log);
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
               string.Equals(strPattern,str, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }



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

        /// <summary>
        /// Einlesen eines Strings aus einem File unter Berücksichtigung des passenden Encodings (wg. Sonderzeichen in File-Inhalt, z.B. SQL-Inserts mit Umlauten)
        /// Dabei werden mehrere Encodings versucht und bei Vorhandensein von Nicht-ANSI Zeichen (ANSI: Code < 256) das nächste Encoding versucht
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

    }
}
