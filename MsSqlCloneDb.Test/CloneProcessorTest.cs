using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using MsSqlCloneDb.Lib;
using Newtonsoft.Json;
using NUnit.Framework;

namespace MsSqlCloneDb.Test
{
    [TestFixture]
    public class CloneProcessorTest
    {
        private CloneProcessorMock _sut;

        private Stopwatch _stopWatch;
        private TestLogger _logger;

        [OneTimeSetUp]
        public void OneSetUp()
        {
        }

        [SetUp]
        public void SetUp()
        {
            WriteInfo($@"Start execution at {DateTime.Now.ToLocalTime()}");
            _stopWatch = Stopwatch.StartNew();

            _logger = new TestLogger();
            _sut = new CloneProcessorMock(_logger);

            SetWorkingDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            WriteElapsedTime();
        }

        [TestCase("dbTargetSimple", "[]")]
        [TestCase("dbTargetICLx", "[]")]
        public void ClearTargetDatabase(string dbTargetAppConfig, string strExpected = "[]")
        {
            var config = new CloneParametersExt
            {
                dbTarget = dbTargetAppConfig
            };

            _sut.SetConfigMock(config);

            // Select available tables
            var sql = "SELECT sobjects.name FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";
            // ReSharper disable once RedundantAssignment
            var str = _sut.ExecuteSelectMock(sql); // for debug

            ClearDatabase(dbTargetAppConfig);

            str = _sut.ExecuteSelectMock(sql);
            Assert.AreEqual(strExpected, str);
        }

        [TestCase("dbSourceICLx", "dbTargetICLx")]
        [TestCase("dbSourceSimple", "dbTargetSimple")]
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
                strSkipTables = "*"
            };
            config.AdaptParameters();

            _sut.Execute(config);

            CheckSchemas(config.dbSourceConnectionString, config.dbTargetConnectionString);
        }

        [TestCase("dbSourceICLx", "dbTargetICLx")]
        [TestCase("dbSourceSimple", "dbTargetSimple")]
        public void Execute_01_Full_Clone(string dbSource, string dbTarget)
        {
            var config = new CloneParametersExt
            {
                dbSource = dbSource,
                dbTarget = dbTarget,
                strSkipTables = "*",
                strRestoreTables = "global.globalConfiguration",
                strMergeTables = "",
                strMergeScripts = "",
                strUpdateScripts = "",
                strFinalScripts = ""
            };
            config.AdaptParameters();

            _sut.Execute(config);

            CheckSchemas(config.dbSourceConnectionString, config.dbTargetConnectionString);
        }

        [TestCase("dbTargetSimple", @"Scripts\SQL_Update_01.txt", true)]  // success expected
        [TestCase("dbTargetSimple", @"Scripts\SQL_Update_02.txt", false)] // Error expected
        [TestCase("dbTargetSimple", @"Scripts\SQL_Update_02.txt,Scripts\SQL_Update_01.txt", false)]
        [TestCase("dbTargetSimple", @"Scripts\SQL_Update_01.txt,Scripts\SQL_Update_02.txt", false)]
        [TestCase("dbTargetSimple", @"Scripts\SQL_Update_*.txt", false)]
        public void Execute_UpdateScripts(string dbTarget, string scripts, bool expectedSuccess)
        {
            var config = new CloneParametersExt
            {
                dbSource = "",
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strMergeTables = "",
                strMergeScripts = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters();

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

        [TestCase("dbTargetICLx")]
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
                strMergeTables = "",
                strMergeScripts = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters();

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

        [TestCase("dbTargetICLx", "-- first\r\nSELECT 1 FROM NOTEXISTING\r\nGO\r\n-- second\r\nUPDATE NOTEXISTINGTABLE\r\nGO")]
        [TestCase("dbTargetICLx", "-- first\n\rSELECT 1 FROM NOTEXISTING\n\rGO\n\r-- second\n\rUPDATE NOTEXISTINGTABLE\n\rGO")]
        [TestCase("dbTargetICLx", "-- first\rSELECT 1 FROM NOTEXISTING\rGO\r-- second\rUPDATE NOTEXISTINGTABLE\rGO")]
        [TestCase("dbTargetICLx", "-- first\nSELECT 1 FROM NOTEXISTING\nGO\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        [TestCase("dbTargetICLx", "-- first\rSELECT 1 FROM NOTEXISTING\rgo\r-- second\rUPDATE NOTEXISTINGTABLE\rGO")]
        [TestCase("dbTargetICLx", "-- first\nSELECT 1 FROM NOTEXISTING\nGo\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        [TestCase("dbTargetICLx", "-- first\nSELECT 1 FROM NOTEXISTING\ngO\n-- second\nUPDATE NOTEXISTINGTABLE\nGO")]
        public void ExecuteScriptFromString_Line_Ending_And_Case_Insensitive(string dbTarget, string strScript)
        {
            var scripts = CreateScriptFile(strScript);

            var config = new CloneParametersExt
            {
                dbSource = "",
                dbTarget = dbTarget,
                strSkipTables = "",
                strRestoreTables = "",
                strMergeTables = "",
                strMergeScripts = "",
                strUpdateScripts = scripts,
                strFinalScripts = ""
            };
            config.AdaptParameters();

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
            var list = _sut.GetFilesByPatternsMock(strPattern);

            PrintList(list);

            Assert.AreEqual(expectedCount, list.Count);
        }
        
        [TestCase("globalConfigurationStructure", "", 1)]
        [TestCase("MandantConfigurationStructure", "", 1)]
        [TestCase("globalConfigurationStructure", "SQL_Merge_GlobalConfigurationStructure.sql", 1)]
        [TestCase("MandantConfigurationStructure", "SQL_Merge_MandantConfigurationStructure.sql", 1)]
        [TestCase("globalConfigurationStructure,MandantConfigurationStructure", "SQL_Merge_GlobalConfigurationStructure.sql,SQL_Merge_MandantConfigurationStructure.sql", 2)]
        public void TestMerge(string strMergeTables, string strMergeScripts, int expectedAmountOfFiles)
        {
            SetWorkingDirectory();

            var commandLineParameters = new[]
            {
                        "-dbTarget=dbTargetICLx", 
                        $"-mergeTables={strMergeTables}",
                        $"-mergeScripts={strMergeScripts}"
                    };

            var config = CloneParametersExt.GetParameters(commandLineParameters).AdaptParameters();

            config.PrintParameters(_logger);

            _sut.SetConfigMock(config);

            var targetStringBuilder = new SqlConnectionStringBuilder(config.dbTargetConnectionString);

            var createdFiles = _sut.CreateBackupForTablesToMerge(targetStringBuilder);

            Assert.AreEqual(expectedAmountOfFiles, createdFiles.Count, $@"amount of expected created backup files is {expectedAmountOfFiles}");

            var i = 0;
            foreach (var createdFile in createdFiles)
            {
                WriteInfo($@"------- {i++}. created backup  for {createdFile} ------------");
                var backupText = File.ReadAllText(createdFiles.First());
                WriteInfo(backupText);
            }

            _sut.RestoreBackupForTablesToMerge(targetStringBuilder);
        }
        
        [TestCase("SQL_AutoCreate_GlobalConfiguration.sql")]
        [TestCase("SQL_AutoCreate_MandantConfiguration.sql")]
        [TestCase("SQL_AutoCreate_GlobalConfiguration.sql,SQL_AutoCreate_MandantConfiguration.sql")]
        public void TestAutoCreateAndOverwrite(string strFinalScripts)
        {
            SetWorkingDirectory();

            var commandLineParameters = new[]
            {
                "-dbTarget=dbTargetICLx",
                $"-strFinalScripts={strFinalScripts}"
            };

            var toolParameters = CloneParametersExt.GetParameters(commandLineParameters).AdaptParameters();

            toolParameters.PrintParameters(_logger);

            _sut.SetConfig(toolParameters);

            var targetStringBuilder = new SqlConnectionStringBuilder(toolParameters.dbTargetConnectionString);

            var resSuccess = _sut.ExecuteFinalScriptsMock(targetStringBuilder);
            Assert.IsTrue(resSuccess, "Fehler bei ExecuteFinalScripts");
        }

        [TestCase("SQL_Final_001_FixDatabase.sql")]
        public void TestCheckconstraints(string strFinalScripts)
        {
            SetWorkingDirectory();

            var commandLineParameters = new[]
            {
                "-dbTarget=dbTargetICLx",
                $"-finalScripts={strFinalScripts}"
            };

            var toolParameters = CloneParametersExt.GetParameters(commandLineParameters).AdaptParameters();

            toolParameters.PrintParameters(_logger);

            _sut.SetConfig(toolParameters);

            var targetStringBuilder = new SqlConnectionStringBuilder(toolParameters.dbTargetConnectionString);

            #region Create 'wrong' data in the DB

            var script = @"
        -- Deactivate Constraint on GlobalConfigurationStructureLang
        ALTER TABLE [global].[GlobalConfigurationStructureLang] NOCHECK CONSTRAINT FK_GlobalConfigurationStructureLang_Key
        DELETE FROM [global].[GlobalConfigurationStructureLang] WHERE [KEY] in ('KEY_1', 'KEY_2')

        INSERT INTO [global].[GlobalConfigurationStructureLang] ([Key],[Name],[Description],[Lang],[_CreatedBy],[_CreateDate],[_ModifiedBy],[_ModifyDate])
             VALUES('KEY_1', 'NAME_KEY_1', 'DESCRIPTION_KEY_1_DE', 'DE', 'INITIAL', getdate(), 'INITIAL', getdate())

        INSERT INTO [global].[GlobalConfigurationStructureLang] ([Key],[Name],[Description],[Lang],[_CreatedBy],[_CreateDate],[_ModifiedBy],[_ModifyDate])
             VALUES('KEY_2', 'NAME_KEY_2', 'DESCRIPTION_KEY_2_DE', 'DE', 'INITIAL', getdate(), 'INITIAL', getdate())

        -- Activate constraint, but do not check this constraint for already available records
        ALTER TABLE [global].[GlobalConfigurationStructureLang] CHECK CONSTRAINT FK_GlobalConfigurationStructureLang_Key

        ";

            var resSuccess = _sut.ExecuteScriptFromString(script, targetStringBuilder, true,true);
            Assert.IsTrue(resSuccess, "Fehler bei ExecuteScriptFromString");

            CheckGlobalConfigurationStructureLang(targetStringBuilder, 2);

            #endregion

            // Execute Script
            _sut.ExecuteFinalScriptsMock(targetStringBuilder);


            // Check Result
            CheckGlobalConfigurationStructureLang(targetStringBuilder, 0);

        }

        [TestCase("dbSourceICLx", "global.GlobalConfiguration")]
        public void CreateBackupForSingleTable(string dbTarget, string strTable)
        {
            var connectionString = Helper.GetConnectionString(dbTarget);
            var targetBuilder = new SqlConnectionStringBuilder(connectionString);

            var fileName = _sut.CreateBackupForSingleTableMock(targetBuilder, strTable, "", "");


            string firstLine = File.ReadLines(fileName).First();

            Assert.AreEqual(CloneProcessor.Backup_Script_Fragment, firstLine);
        }

        private void CheckGlobalConfigurationStructureLang(SqlConnectionStringBuilder targetStringBuilder, int expectedCount)
        {
            const string strSelect = "SELECT [KEY]  FROM [global].[GlobalConfigurationStructureLang] WHERE [KEY] in ('KEY_1', 'KEY_2')";
            var strJson = _sut.ExecuteSelectMock(strSelect, targetStringBuilder.ConnectionString);
            var temp = JsonConvert.DeserializeObject<List<KeyValueClass>>(strJson);
            Assert.AreEqual(expectedCount, temp.Count);
        }
        
        // todo move to base class or create an extension
        private void PrintList(List<string> list)
        {
            foreach (var item in list)
            {
                WriteInfo(item);
            }
        }

        private void CheckSchemas(string sourceConnectionString, string targetConnectionString)
        {
            // Select available tables
            var sql = "SELECT sobjects.name as Value FROM sysobjects sobjects WHERE sobjects.xtype = 'U'";
            var strSource = _sut.ExecuteSelectMock(sql, sourceConnectionString);
            var strTarget = _sut.ExecuteSelectMock(sql, targetConnectionString);

            var sourceList = GetSelectedValues(strSource);
            var targetList = GetSelectedValues(strTarget);

            Assert.AreEqual(sourceList, targetList);
        }

        private List<string> GetSelectedValues(string strJson)
        {

            var ret = JsonConvert.DeserializeObject<List<SelectValueSingle>>(strJson)
                .OrderBy(x => x.Value)
                .Select(x => x.Value)
                .ToList();

            return ret;
        }

        private void ClearDatabase(string databaseAppConfig)
        {
            var strFinalScripts = "SQL_ClearDatabase.sql"; // todo magic-string. use const

            var commandLineParameters = new[]
            {
                $"-dbTarget={databaseAppConfig}",
                $"-finalScripts={strFinalScripts}"
            };

            var toolParameters = CloneParametersExt.GetParameters(commandLineParameters);
            toolParameters = CloneParametersExt.AdaptParameters(toolParameters);

            //todo trace parameters

            _sut.Execute(toolParameters);
        }

        private void WriteInfo(string str)
        {
            Console.WriteLine(str);
        }

        private void WriteElapsedTime()
        {
            WriteInfo($@"Elapsed Time : {_stopWatch.ElapsedMilliseconds / 1000} second(s)"); 
        }

        [SuppressMessage("ReSharper", "UnusedVariable")]
        private void SetWorkingDirectory()
        {
            var str1 = Directory.GetCurrentDirectory();
            var str2 = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var currentDirectory = Directory.GetCurrentDirectory();

            WriteInfo($@"Current directory='{currentDirectory}'");
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

    public class CloneProcessorMock : CloneProcessor
    {
        public CloneProcessorMock(ILogger logger) : base(logger)
        {
        }

        public string ExecuteSelectMock(string sql, string dbConnectionstring = null)
        {
            string ret;

            var connectionString = dbConnectionstring ?? _config.dbTargetConnectionString;

            using (var dbConnection = new SqlConnection(connectionString))
            {
                dbConnection.Open();

                ret = ExecuteSelect(sql, dbConnection);
                dbConnection.Close();
            }

            return ret;
        }

        public void SetConfigMock(CloneParametersExt config)
        {
            config.AdaptParameters();
            SetConfig(config);
        }

        public string CreateBackupForSingleTableMock(
            SqlConnectionStringBuilder target,
            string tableName,
            string filePrefix,
            string fileSuffix)
        {
            return CreateBackupForSingleTable(target, tableName, filePrefix, fileSuffix);
        }

        // TODO move it in CloneParametersExtTest 
        public List<string> GetFilesByPatternsMock(string strPatterns)
        {
            return CloneParametersExt.GetFilesByPatternString(strPatterns);
        }

        public bool ExecuteFinalScriptsMock(SqlConnectionStringBuilder target)
        {
            return ExecuteFinalScripts(target);
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

    public class TestLogger : ILogger, ILogSink
    {
        private readonly StringBuilder sb = new StringBuilder();

        public void AddLog(string str)
        {
            WriteInfo(str);
        }

        public void AddBoldLogEntry(string log)
        {
            WriteInfo(log);
        }

        public void AddBoldLogEntry(string log, System.Drawing.Color color)
        {
            WriteInfo(log);
        }

        public void AddLogEntry(string log)
        {
            WriteInfo(log);
        }

        public void AddLogEntry(string log, System.Drawing.Color color)
        {
            WriteInfo(log);
        }

        private void WriteInfo(string str)
        {
            sb.AppendLine(str);
            Console.WriteLine(str);
        }

        public string LogText => sb.ToString();

        public static TestLogger Instance => new TestLogger();
    }
}
