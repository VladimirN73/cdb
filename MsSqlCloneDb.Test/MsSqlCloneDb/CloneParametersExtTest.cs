using System;
using System.Drawing;
using System.IO;
using NUnit.Framework;

namespace MsSqlCloneDb.Test.MsSqlCloneDb
{
    [TestFixture]
    public class CloneParametersExtTest
    {

        [TestCase("update.sql", "update.sql")]
        [TestCase("", "")]
        public void ParameterUpdateScripts(string paramValue, string strExpected)
        {
            var args = new[]
            {
                $"-updateScripts={paramValue}",
                $"-finalScripts=final.sql"
            };

            var parameters = CloneParametersExt.GetParameters(args);

            Assert.AreEqual(strExpected, parameters.strUpdateScripts);
        }

        [TestCase("Data Source=xx;Initial Catalog=DB_SOURCE", @".\Scripts\SQL_001.txt", "SourceDB=DB_SOURCE")]
        [TestCase("Data Source=xx;Initial Catalog=DB_SOURCE", @".\Scripts\SQL_001.txt", "VariableA=#{VariableA}#")]
        [TestCase("", @".\Scripts\SQL_001.txt", "SourceDB=#{SourceDB}#")]
        [TestCase("", @".\Scripts\SQL_001.txt", "VariableA=#{VariableA}#")]
        [TestCase("dbSourceSimple", @".\Scripts\SQL_001.txt", "SourceDB=ICLx_Clone_Source")]
        [TestCase("dbSourceSimple", @".\Scripts\SQL_001.txt", "VariableA=#{VariableA}#")]
        public void ReplaceVariablesInFinalScripts(string dbSource, string script, string expectedFragment)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Script content
            //SourceDB=#{SourceDB}#
            //VariableA =#{VariableA}#
 
            var config = new CloneParametersExt
            {
                dbSource = dbSource,
                strFinalScripts = script
            };

            var parameters = CloneParametersExt.AdaptParameters(config);
            parameters = CloneParametersExt.ReplaceVariablesInFinalScripts(parameters, new LoggerInternal() );

            var str = parameters.finalScripts[0].ScriptText;
            
            Assert.True(str.Contains(expectedFragment));

        }

        internal class LoggerInternal : ILogSink
        {
            public void AddLogEntry(string log)
            {
                Console.WriteLine(log);
            }

            public void AddLogEntry(string log, Color color)
            {
                Console.WriteLine(log);
            }

            public void AddBoldLogEntry(string log)
            {
                Console.WriteLine(log);
            }

            public void AddBoldLogEntry(string log, Color color)
            {
                Console.WriteLine(log);
            }
        }
    }

    
}
