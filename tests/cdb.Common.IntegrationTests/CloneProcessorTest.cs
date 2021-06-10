using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using cdb.Module.Console;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace cdb.Common.IntegrationTests
{
    [TestFixture]
    public class CloneProcessorTest :TestBase
    {
        private CloneProcessor _sut;

        private TestContainer _di;
        private AppLoggerTest _logger;
        private ICmdParameterParser _cmdParser;
        private IConfiguration _config;

        #region test setup

        [OneTimeSetUp]
        public void OneSetUp()
        {
            _di = new TestContainer();

            _sut = _di.Resolve<ICloneProcessor>() as CloneProcessor;

            _logger = _di.Resolve<IAppLogger>() as AppLoggerTest;
            _cmdParser = _di.Resolve<ICmdParameterParser>();
            _config = _di.Resolve<IConfiguration>();
        }

        [SetUp]
        public void SetUp()
        {
            StartTest();
        }

        [TearDown]
        public void TearDown()
        {
            WriteElapsedTime();
        }

        #endregion

        [TestCase("local_target")]
        public void ClearTargetDatabase(string dbTarget)
        {
            var config = new CloneParametersExt
            {
                dbTarget = dbTarget
            };

            config.AdaptParameters(_config);

            _sut.SetConfig(config);

            // Select available tables
            var sql = "SELECT sobjects.name FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";

            // ReSharper disable once RedundantAssignment
            var str = _sut.ExecuteSelect(sql); // for debug

            ClearDatabase(dbTarget);

            str = _sut.ExecuteSelect(sql);
            var strExpected = "[]";
            Assert.AreEqual(strExpected, str);
        }

        [TestCase("local_source", "local_target")]
        public void LoadSchemaTest(string dbSource, string dbTarget)
        {
            // =======================================================
            // Preparation. Ensure that target DB has no Tables
            ClearTargetDatabase(dbTarget);

            WriteElapsedTime();

            var config = new CloneParametersExt
            {
                dbSource = dbSource,
                dbTarget = dbTarget,
                skipTables = new List<string>{"*"}  // skip all
            };

            config.AdaptParameters(_config);

            _sut.Execute(config);

            CompareDbSchemas(config.dbSourceConnectionString, config.dbTargetConnectionString);
        }

        [TestCase("dbSourceDB", "dbTargetDB")]
        public void Execute_01_Full_Clone(string dbSource, string dbTarget)
        {
            var config = new CloneParametersExt
            {
                dbSource = dbSource,
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strUpdateScripts = "",
                strFinalScripts = ""
            };
            config.AdaptParameters(_config);

            _sut.Execute(config);

            Assert.IsFalse(DbIsEmpty(dbTarget));

            CompareDbSchemas(config.dbSourceConnectionString, config.dbTargetConnectionString);

            CompareDbContent(config.dbSourceConnectionString, config.dbTargetConnectionString);
        }

        [TestCase("dbTargetDB", @"Scripts\SQL_Update_01.txt", true)]  // success expected
        [TestCase("dbTargetDB", @"Scripts\SQL_Update_02.txt", false)] // Error expected
        [TestCase("dbTargetDB", @"Scripts\SQL_Update_02.txt,Scripts\SQL_Update_01.txt", false)]
        [TestCase("dbTargetDB", @"Scripts\SQL_Update_01.txt,Scripts\SQL_Update_02.txt", false)]
        [TestCase("dbTargetDB", @"Scripts\SQL_Update_*.txt", false)]
        public void Execute_UpdateScripts(string dbTarget, string scripts, bool expectedSuccess)
        {
            var config = new CloneParametersExt
            {
                dbSource = "",
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters(_config);

            var isSuccess = true;
            try
            {
                _sut.Execute(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isSuccess = false;
            }

            Assert.AreEqual(expectedSuccess, isSuccess);

        }

        [TestCase("local_target")]
        public void ExecuteScriptFromString_Abort_by_first_failed_fragment(string dbTarget)
        {
            var strScript = @"
-- first failed
SELECT 1 FROM NOTEXISTINGTABLE
GO
-- second failed
UPDATE NOTEXISTINGTABLE
GO
";
            var scripts = CreateScriptFile(strScript);

            var config = new CloneParametersExt
            {
                dbSource = "",
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters(_config);

            var isSuccess = true;
            try
            {
                _sut.Execute(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isSuccess = false;
            }

            Assert.AreEqual(false, isSuccess);

            Assert.IsTrue(_logger.LogText.Contains("-- first"));
            Assert.IsFalse(_logger.LogText.Contains("-- second"));

        }

        [TestCase("dbTargetDB", "-- first\r\nSELECT 1 FROM NOTEXISTING\r\nGO\r\n-- second\r\nUPDATE NOTEXISTINGTABLE\r\nGO")]
        [TestCase("dbTargetDB", "-- first\n\rSELECT 1 FROM NOTEXISTING\n\rGO\n\r-- second\n\rUPDATE NOTEXISTINGTABLE\n\rGO")]
        [TestCase("dbTargetDB", "-- first\rSELECT 1 FROM NOTEXISTING\rGO\r-- second\rUPDATE NOTEXISTINGTABLE\rGO")]
        [TestCase("dbTargetDB", "-- first\nSELECT 1 FROM NOTEXISTING\nGO\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        [TestCase("dbTargetDB", "-- first\rSELECT 1 FROM NOTEXISTING\rgo\r-- second\rUPDATE NOTEXISTINGTABLE\rGO")]
        [TestCase("dbTargetDB", "-- first\nSELECT 1 FROM NOTEXISTING\nGo\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        [TestCase("dbTargetDB", "-- first\nSELECT 1 FROM NOTEXISTING\ngO\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        public void ExecuteScriptFromString_Line_Ending_And_Case_Insensitive(string dbTarget, string strScript)
        {
            var scripts = CreateScriptFile(strScript);

            var config = new CloneParametersExt
            {
                dbSource = "",
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters(_config);

            var isSuccess = true;
            try
            {
                _sut.Execute(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isSuccess = false;
            }

            Assert.AreEqual(false, isSuccess);

            Assert.IsTrue(_logger.LogText.Contains("-- first"));
            Assert.IsFalse(_logger.LogText.Contains("-- second"));

        }

        [TestCase(@".\Scripts\SQL_Final_0*.sql", 2)]
        [TestCase(@".\Scripts\SQL_Final_*.sql", 3)]
        [TestCase(@".\Scripts\SQL_Final_x*.sql,.\Scripts\SQL_Final_0*.sql", 3)]
        [TestCase(@".\ScriptsWRONG\SQL_Final_x*.sql", 0)]
        public void GetFilesByPatternsTest(string strPattern, int expectedCount)
        {
            var list = CloneParametersExt.GetFilesByPatternString(strPattern);

            PrintList(list);

            Assert.AreEqual(expectedCount, list.Count);
        }
        
        private void CompareDbSchemas(string sourceConnectionString, string targetConnectionString)
        {
            // Select available tables
            var sql = "SELECT sobjects.name as Value FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";
            var strSource = _sut.ExecuteSelect(sql, sourceConnectionString);
            var strTarget = _sut.ExecuteSelect(sql, targetConnectionString);

            var sourceList = GetSelectedValues(strSource);
            var targetList = GetSelectedValues(strTarget);

            Assert.AreEqual(sourceList, targetList);
        }

        private void CompareDbContent(string sourceConnectionString, string targetConnectionString)
        {
            // Select available tables
            var sql = "SELECT sobjects.name as Value FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";
            var strSource = _sut.ExecuteSelect(sql, sourceConnectionString);

            var res = JArray.Parse(strSource);

            var tableList = res.Children().Select(x => x["Value"]).ToList();

            foreach (var strTable in tableList)
            {
                sql = $"select * from {strTable}";
                var strA = _sut.ExecuteSelect(sql, sourceConnectionString);
                var strB = _sut.ExecuteSelect(sql, targetConnectionString);

                Assert.AreEqual(strA, strB);
            }

            int i = 1;
        }

        private List<string> GetSelectedValues(string strJson)
        {

            var ret = JsonConvert.DeserializeObject<List<SelectValueSingle>>(strJson)
                .OrderBy(x => x.Value)
                .Select(x => x.Value)
                .ToList();

            return ret;
        }

        private void ClearDatabase(string dbTarget)
        {
            var strFinalScripts = CloneProcessor.SqlClearDatabase;

            var commandLineParameters = new[]
            {
                $"-dbTarget={dbTarget}",
                $"-finalScripts={strFinalScripts}"
            };

            var toolParameters = CloneParametersExt.GetParameters(commandLineParameters, _cmdParser);
            toolParameters = toolParameters.AdaptParameters(_config);

            _sut.Execute(toolParameters);
        }

        private bool DbIsEmpty(string dbTarget)
        {
            var config = new CloneParametersExt
            {
                dbTarget = dbTarget
            };

            config.AdaptParameters(_config);

            _sut.SetConfig(config);

            // Select available tables
            var sql = "SELECT sobjects.name FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";

            var str = _sut.ExecuteSelect(sql);
            var strExpected = "[]";

            var ret = str == strExpected;
            return ret;
        }

        private string CreateScriptFile(string strScript)
        {
            return CreateScriptFile(strScript, @"sql_temp.sql");
        }

        private string CreateScriptFile(string strScript, string strFileName)
        {

            var file = File.Create(strFileName);
            file.Close();

            File.AppendAllText(strFileName, strScript);

            var ret = Path.GetFullPath(strFileName);

            return ret;
        }
    }


    public class SelectValueSingle
    {
        public string Value;
    }

    public class KeyValueClass
    {
        public string Key;
    }


    public static class CloneProcessorTestExtensions
    {
        public static string ExecuteSelect(
            this CloneProcessor value,
            string strSelect,
            string connectionString = null,
            IAppLogger logger = null)
        {
            var strMethod = $@"{nameof(ExecuteSelect)}";

            logger?.Log($@"--> {strMethod}");

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = value.Config.dbTargetConnectionString;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException($"{nameof(connectionString)}");
            }
            
            value.EnsureNonProdDb(connectionString);
            
            var dbConnection = new SqlConnection(connectionString);

            string ret;

            try
            {
                logger?.Log(strSelect);

                if (dbConnection.State == ConnectionState.Closed)
                {
                    dbConnection.Open();
                }

                var command = new SqlCommand(strSelect, dbConnection)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 600
                };

                var reader = command.ExecuteReader();

                var r = HelperX.Serialize(reader);
                ret = JsonConvert.SerializeObject(r, Formatting.Indented);

            }
            catch (Exception ex)
            {
                logger?.Log(
                    string.Format("Error: The query cannot be executed:{2}{0}{2} Message:{2}{1}",
                        strSelect, ex.Message, Environment.NewLine));

                throw;
            }
            finally
            {
                logger?.Log($@"<-- {strMethod}");
            }

            return ret;
        }
}

}
