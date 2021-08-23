using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using cdb.Common.Extensions;
using Microsoft.SqlServer.Management.Smo;

namespace cdb.Common
{
    public interface ICloneProcessor
    {
        bool Execute(CloneParameters config);
    }

    public class CloneProcessor : ICloneProcessor
    {
        private readonly string _strClass = $"{nameof(CloneProcessor)}";

        public const string SqlClearDatabase = "./Scripts/SQL_ClearDatabase.sql";

        private readonly IAppLogger _logger;

        private readonly string _executionId; // use unique Id to enable parallel execution

        private CloneParameters _config;

        public CloneProcessor(IAppLogger logger)
        {
            _logger = logger;
            _executionId = DateTime.Now.Ticks.ToString();
        }

        public CloneParameters Config => _config;

        private SqlConnectionStringBuilder TargetBuilder => BuilderByConnectionString(_config.dbTargetConnectionString);

        private SqlConnectionStringBuilder SourceBuilder => BuilderByConnectionString(_config.dbSourceConnectionString);

        private SqlConnectionStringBuilder BuilderByConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            return new SqlConnectionStringBuilder(connectionString);
        }

        public bool Execute(CloneParameters config)
        {
            SetConfig(config);

            CheckConnections();

            // STEP
            // Create Db Schema file only if the parameter 'dbSource' is provided. 
            DoCreateSchema();

            // STEP
            //CreateBackupForTablesToRestore(TargetBuilder);
            //CreateBackupForTablesToMerge(TargetBuilder);

            //// STEP
            DoLoadSchema(TargetBuilder, _config.schemaFile);
            
            //// STEP
            DoTransferData(SourceBuilder, TargetBuilder);

            //// STEP
            if (!ExecuteUpdateScripts(TargetBuilder))
            {
                throw new Exception($@"Error by {nameof(ExecuteUpdateScripts)}");
            }

            //// STEP
            //if (!RestoreBackupForTablesToRestore(TargetBuilder))
            //{
            //    throw new Exception($@"Error by {nameof(RestoreBackupForTablesToRestore)}");
            //}

            //if (!RestoreBackupForTablesToMerge(TargetBuilder))
            //{
            //    throw new Exception($@"Error by {nameof(RestoreBackupForTablesToRestore)}");
            //}

            //// STEP
            if (!ExecuteFinalScripts(TargetBuilder))
            {
                throw new Exception($@"Error by {nameof(ExecuteFinalScripts)}");
            }

            return true;
        }

        public void SetConfig(CloneParameters config)
        {
            _config = config;

            TraceConfigDatabases();

            EnsureNonProdDb();
        }

        private void DoCreateSchema()
        {
            if (SourceBuilder != null)
            {
                _config.schemaFile = GenerateFileName();

                DoCreateSchema(_config.schemaFile, SourceBuilder);
            }
        }

        protected string GenerateFileName()
        {
            // define file name to store DB-Target-Schema
            var schemaFile = SourceBuilder.InitialCatalog.Replace(".", "_");
            if (string.IsNullOrEmpty(schemaFile)) schemaFile = "DB_Source_Schema";
            schemaFile = schemaFile.Replace(" ", "_");

            schemaFile = $"{_executionId}_{schemaFile}.sql";

            return schemaFile;
        }

        public void EnsureNonProdDb()
        {
            EnsureNonProdDb(_config.dbTargetConnectionString);
        }

        public void EnsureNonProdDb(SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target.ConnectionString);
        }

        public void EnsureNonProdDb(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return;

            // TODO magic strings
            // Exclude PROD-DB from the target DBs
            var str = connectionString.ToLower().Trim();
            var isNonProd =
                str.Contains("localhost") ||
                str.Contains("sql2019") ||
                str.Contains("cdb_") ||
                str.EndsWith("-dev") ||
                str.EndsWith("_dev")
                ;

            if (isNonProd)
            {
                return;
            }

            throw new Exception("Prod-DB cannot used as a target DB ");
        }

        public void TraceConfigDatabases()
        {
            var str = new StringBuilder();
            str.AppendLine("==========================================================");
            str.AppendLine($"=== source : {SourceBuilder?.InitialCatalog}");
            str.AppendLine($"=== target : {TargetBuilder?.InitialCatalog}");
            str.AppendLine("==========================================================");
            Log(str.ToString());
        }

        private void Log(string str)
        {
            _logger?.Log(str);
        }

        private void CheckConnections()
        {
            if (SourceBuilder != null)
            {
                CheckConnection(SourceBuilder.ConnectionString);
                Log(@"Checking Source-Connection: successful");
            }

            if (TargetBuilder != null)
            {
                CheckConnection(TargetBuilder.ConnectionString);
                Log(@"Checking Target-Connection: successful");
            }
        }

        private void CheckConnection(string connectionString)
        {
            using var dbConnection = new SqlConnection(connectionString);
            dbConnection.Open();
            dbConnection.Close();
        }

        public void DoCreateSchema(string schemaFile, SqlConnectionStringBuilder source)
        {
            var strFunc = $@"{_strClass}.{nameof(DoCreateSchema)}";

            using (new StackLogger(strFunc))
            {
                Log($@"source   :{source?.InitialCatalog}");
                Log($@"fileName :{schemaFile}");

                if (source == null)
                {
                    Log("The step 'create Db schema' is skipped, due to the target-db is not presented");
                    return;
                }

                GenerateSchema(schemaFile, source);
            }
        }

        private void GenerateSchema(string schemaFile, SqlConnectionStringBuilder source)
        {
            try
            {
                using var scripter = new DbSchemaScripter(source.DataSource, source.UserID, source.Password, schemaFile);
                scripter.GenerateSchema(source.InitialCatalog);
            }
            catch (Exception ex)
            {
                Log($"Error in GenerateSchema {source.InitialCatalog}");
                Log("Stacktrace: " + ex);
                throw;
            }
        }

        public void DoLoadSchema(SqlConnectionStringBuilder target, string schemaFile)
        {
            EnsureNonProdDb(target);

            var strFunc = $@"{_strClass}.{nameof(DoLoadSchema)}";

            using var stacklogger = new StackLogger(strFunc);
            Log($@"target    :{target?.InitialCatalog}");
            Log($@"schemaFile:{schemaFile}");

            if (string.IsNullOrEmpty(target?.ConnectionString))
            {
                Log("The load-schema is skipped, due to the target-db is empty");
                return;
            }

            if (schemaFile.IsNullOrEmpty())
            {
                Log("The load-schema is skipped, due to the schema-file is empty");
                return;
            }
            
            ClearTargetDatabase(target);
            
            var ret = ExecuteScriptFromFile(schemaFile, target, false);

            if (!ret)
            {
                throw  new Exception($"Error by {strFunc}");
            }
        }

        protected void ClearTargetDatabase(SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);

            var strFunc = $@"{_strClass}.{nameof(ClearTargetDatabase)}";

            using var stacklogger = new StackLogger(strFunc);

            var ret = ExecuteScriptFromFile(SqlClearDatabase, target, false);
            if (!ret)
            {
                throw new Exception($"Error by {nameof(ClearTargetDatabase)} ");
            }
        }

        //TODO make it internal (or protected)
        public bool ExecuteUpdateScripts(SqlConnectionStringBuilder target)
        {
            var strFunc = $@"{_strClass}.{nameof(ExecuteUpdateScripts)}";

            using (new StackLogger(strFunc))
            {
                return ExecuteScriptsByList(target, _config.updateScripts, false, false);
            }
        }

        //TODO make it internal (or protected)
        public bool ExecuteFinalScripts(SqlConnectionStringBuilder target)
        {
            var strFunc = $@"{_strClass}.{nameof(ExecuteFinalScripts)}";

            using (new StackLogger(strFunc))
            {
                return ExecuteScriptsByList(target, _config.finalScripts, false, false);
            }
        }

        protected bool ExecuteScriptsByList(SqlConnectionStringBuilder target, List<ScriptInfo> scripts, bool writeLog,
            bool continueOnError)
        {
            bool ret;
            using (var dbConnection = new SqlConnection(target.ConnectionString))
            {
                dbConnection.Open();

                ret = ExecuteScriptsByList(dbConnection, scripts, writeLog, continueOnError);

                dbConnection.Close();
            }

            return ret;
        }

        protected bool ExecuteScriptsByList(SqlConnection dbConnection, List<ScriptInfo> scripts, bool writeLog,
            bool continueOnError)
        {
            var retSuccess = true;
            foreach (var script in scripts)
            {
                Log($@" {script.ScriptName}");
                var flagSuccess = ExecuteScriptFromString(script.ScriptText, dbConnection, writeLog, continueOnError);
                if (!flagSuccess)
                {
                    retSuccess = false;
                }

                if (!flagSuccess && !continueOnError)
                {
                    break;
                }
            }

            return retSuccess;
        }

        private bool ExecuteScriptFromFile(string fileName, SqlConnectionStringBuilder target,
            bool continueOnError = true)
        {
            EnsureNonProdDb(target);

            var strFunc = $@"{_strClass}.{nameof(ExecuteScriptFromFile)}({fileName},target, {continueOnError})";

            using var stacklogger = new StackLogger(strFunc);

            if (!continueOnError && !File.Exists(fileName))
            {
                throw new Exception($"File {fileName} not found");
            }

            using var dbConnection = new SqlConnection(target.ConnectionString);
            dbConnection.Open();

            var ret = ExecuteScriptFromFile(fileName, dbConnection, continueOnError: continueOnError);

            dbConnection.Close();

            return ret;
        }

        private bool ExecuteScriptFromFile(string fileName, SqlConnection dbConnection, bool writeLog = false,
            bool continueOnError = true)
        {
            EnsureNonProdDb(dbConnection.ConnectionString);

            var str = GetScriptFromFile(fileName);

            str = str.NormaliseEndOfLine();

            return ExecuteScriptFromString(str, dbConnection, writeLog, continueOnError);
        }

        private string GetScriptFromFile(string fileName)
        {
            Log($"Script '{fileName}' - try to load");
            if (!File.Exists(fileName))
            {
                Log($"WARNING: File '{fileName}' not found");
                return null;
            }

            var ret = HelperX.ReadStringFromFile(fileName);

            return ret;
        }

        public bool ExecuteScriptFromString(
            string scriptString,
            SqlConnectionStringBuilder target,
            bool writeLog,
            bool continueOnError)
        {
            EnsureNonProdDb(target);

            bool retSuccess;

            using (var dbConnection = new SqlConnection(target.ConnectionString))
            {
                dbConnection.Open();

                retSuccess = ExecuteScriptFromString(scriptString, dbConnection, writeLog, continueOnError);

                dbConnection.Close();
            }

            return retSuccess;
        }

        private bool ExecuteScriptFromString(
            string scriptString,
            SqlConnection dbConnection,
            bool writeLog,
            bool continueOnError)
        {
            EnsureNonProdDb(dbConnection.ConnectionString);

            var strFunc = $@"{_strClass}.{nameof(ExecuteScriptFromString)}";
            if (writeLog) Log($@"--> {strFunc}");

            var retSuccess = true; // no error yet

            var scriptNormalized = scriptString.NormaliseEndOfLine();

            var listBlocks = HelperX.SplitSqlScript(scriptNormalized);

            foreach (var sqlBlock in listBlocks)
            {
                try
                {
                    ExecuteCommand(sqlBlock, dbConnection, writeLog);
                }
                catch (Exception ex)
                {
                    Log(
                        string.Format("Error: The query cannot be executed:{2}{0}{2} Message:{2}{1}",
                            sqlBlock, ex.Message, Environment.NewLine));

                    retSuccess = false;

                    if (!continueOnError) break;
                }
            }

            if (writeLog) Log($@"<-- {strFunc}");

            return retSuccess;
        }

        public static int ExecuteCommand(
            string commandText,
            SqlConnection connection,
            bool writelog = true)
        {
            var strFunc = $@"{nameof(ExecuteCommand)}";

            var affectedRows = -1;
            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                var command = new SqlCommand(commandText, connection)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 600
                };

                affectedRows = command.ExecuteNonQuery();
                return affectedRows;
            }
            finally
            {
                var affectedRowsStr = (affectedRows > -1) ? affectedRows.ToString() : "-";
                if (writelog) HelperX.AddLog($@"{strFunc}: Rows={affectedRowsStr} - {commandText}");
            }
        }

        public void DoTransferData(
            SqlConnectionStringBuilder source,
            SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);

            var strFunc = $@"{_strClass}.{nameof(DoTransferData)}";

            using var stackLogger = new StackLogger(strFunc);

            TraceConfigDatabases();

            if (_config.SkipAllTables)
            {
                HelperX.AddLog($"--- {strFunc} : SkipAllTables returns 'true', so no data-transfer is performed");
                return;
            }

            if (string.IsNullOrEmpty(source?.ConnectionString))
            {
                HelperX.AddLog($"--- {strFunc} : dbsource is missing, so no data-transfer is performed");
                return;
            }

            DisableAllCheckConstraintsOnTargetDb(target);
            DisableAllTriggersOnTargetDb(target);
            DisableAllIndexesOnTargetDb(target);

            BulkCopy(source, target);

            EnableAllCheckConstraintsOnTargetDb(target);
            EnableAllTriggersOnTargetDb(target);
            EnableAllIndexesOnTargetDb(target);
        }

        private static void EnableAllIndexesOnTargetDb(SqlConnectionStringBuilder target)
        {
            HelperX.AddLog("Enabling all indexes on target database.");
            ModifyTableIndexesOnTargetDb(true, target);
            ModifyViewIndexesOnTargetDb(true, target);
        }

        private static void DisableAllIndexesOnTargetDb(SqlConnectionStringBuilder target)
        {
            HelperX.AddLog("Disabling all indexes on target database.");
            ModifyTableIndexesOnTargetDb(false, target);
            ModifyViewIndexesOnTargetDb(false, target);
        }

        private static void ModifyTableIndexesOnTargetDb(bool isEnable, SqlConnectionStringBuilder target)
        {
            var strMethod = $"{nameof(ModifyTableIndexesOnTargetDb)}({isEnable})";

            var strFormat = "ALTER INDEX [{0}] ON [{1}].[{2}] REBUILD";

            if (!isEnable)
            {
                strFormat = "ALTER INDEX [{0}] ON [{1}].[{2}] DISABLE";
            }

            var consb = target.ConnectionString;

            using (var destConn = new SqlConnection(consb))
            {
                destConn.Open();

                var db = TargetDatabase(target);

                foreach (Table table in db.Tables)
                {
                    var indexList = table.Indexes.Cast<Microsoft.SqlServer.Management.Smo.Index>()
                        .Where(w =>
                            !w.IndexKeyType.Equals(IndexKeyType.DriPrimaryKey) &&
                            !w.IndexKeyType.Equals(IndexKeyType.DriUniqueKey))
                        .ToList();

                    foreach (var index in indexList)
                    {
                        var strCommand = string.Format(strFormat, index.Name, table.Schema, table.Name);
                        try
                        {
                            ExecuteCommand(strCommand, destConn);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation($"{strMethod}. Error by table '{table.Name}'");
                            Trace.TraceError(ex.Message);
                        }
                    }
                }
            }
        }

        private static void ModifyViewIndexesOnTargetDb(bool isEnable, SqlConnectionStringBuilder target)
        {
            var strMethod = $"{nameof(ModifyViewIndexesOnTargetDb)}({isEnable})";

            var strFormat = "ALTER INDEX ALL ON [{0}].[{1}] REBUILD";

            if (!isEnable)
            {
                strFormat = "ALTER INDEX ALL ON [{0}].[{1}] DISABLE";
            }

            var consb = target.ConnectionString;

            using (var destConn = new SqlConnection(consb))
            {
                destConn.Open();

                var db = TargetDatabase(target);

                var viewList = db.Views.Cast<View>()
                    .Where(w => w.HasIndex)
                    .ToList();

                foreach (var v in viewList)
                {
                    if (isEnable)
                    {
                        var indexList = v.Indexes.Cast<Microsoft.SqlServer.Management.Smo.Index>().ToList();

                        // if more than one index, then activate the clustered index first
                        if (indexList.Count > 1)
                        {
                            // now move clustered index on to of the list
                            var indexListSorted = indexList.Where(x => x.IsClustered).ToList();
                            indexListSorted.AddRange(indexList.Where(x => !x.IsClustered));

                            foreach (var index in indexListSorted)
                            {
                                var strCommandIndex = $"ALTER INDEX {index.Name} ON [{v.Schema}].[{v.Name}] REBUILD;";
                                ExecuteCommand(strCommandIndex, destConn);
                            }
                        }
                    }

                    var strCommand = string.Format(strFormat, v.Schema, v.Name);
                    try
                    {
                        ExecuteCommand(strCommand, destConn);
                    }
                    catch (Exception ex)
                    {
                        HelperX.AddLog($"{strMethod}. Error by View {v.Name}");
                        HelperX.AddLog(ex.Message);
                    }
                }
            }
        }

        private static void ModifyChecksOnTargetDb(string strFormat, SqlConnectionStringBuilder target)
        {
            using (var destConn = new SqlConnection(target.ConnectionString))
            {
                destConn.Open();

                var db = TargetDatabase(target);

                foreach (Table table in db.Tables)
                {
                    var checks = table.Checks.Cast<Check>();

                    foreach (var check in checks)
                    {
                        var strCommand = string.Format(strFormat, table.Schema, table.Name, check.Name);
                        ExecuteCommand(strCommand, destConn);
                    }
                }
            }
        }

        private static void EnableAllCheckConstraintsOnTargetDb(SqlConnectionStringBuilder target)
        {
            var strFunc = $"{nameof(EnableAllCheckConstraintsOnTargetDb)}";
            using( new StackLogger(strFunc))
            {
                ModifyChecksOnTargetDb("ALTER TABLE [{0}].[{1}] CHECK CONSTRAINT {2}", target);
            }
        }

        private static void DisableAllCheckConstraintsOnTargetDb(SqlConnectionStringBuilder target)
        {
            var strFunc = $"{nameof(DisableAllCheckConstraintsOnTargetDb)}";
            using (new StackLogger(strFunc))
            {
                ModifyChecksOnTargetDb("ALTER TABLE [{0}].[{1}] NOCHECK CONSTRAINT {2}", target);
            }
        }

        private static void ModifyTriggersOnTargetDb(string strFormat, SqlConnectionStringBuilder tcsb)
        {
            using (var destConn = new SqlConnection(tcsb.ConnectionString))
            {
                destConn.Open();

                var db = TargetDatabase(tcsb);

                var tables = db.Tables.Cast<Table>();

                foreach (var t in tables)
                {
                    var strCommand = string.Format(strFormat, t.Schema, t.Name);
                    ExecuteCommand(strCommand, destConn);
                }
            }
        }

        private static void EnableAllTriggersOnTargetDb(SqlConnectionStringBuilder tcsb)
        {
            HelperX.AddLog("Enabling all triggers on target database.");
            ModifyTriggersOnTargetDb("ALTER TABLE [{0}].[{1}] ENABLE TRIGGER ALL", tcsb);
        }

        private static void DisableAllTriggersOnTargetDb(SqlConnectionStringBuilder tcsb)
        {
            HelperX.AddLog("Disabling all triggers on target database.");
            ModifyTriggersOnTargetDb("ALTER TABLE [{0}].[{1}] DISABLE TRIGGER ALL", tcsb);
        }

        private void BulkCopy(SqlConnectionStringBuilder scsb, SqlConnectionStringBuilder tcsb)
        {
            EnsureNonProdDb(tcsb);

            using var copier = new DbBulkCopy(scsb, tcsb, Config.IsolationLevel.ToEnum(IsolationLevel.Snapshot));
            HelperX.AddLog("Starting data-transfer");
            copier.Copy(null, null);
        }

        //TODO  similar code (copy-paste) as in DbSchemaScripter.ctor
        private static Server TargerServer(SqlConnectionStringBuilder consb)
        {
            var server = new Server(consb.DataSource);
            server.ConnectionContext.LoginSecure = true;

            if (!string.IsNullOrEmpty(consb.UserID))
            {
                server.ConnectionContext.LoginSecure = false;
                server.ConnectionContext.Login = consb.UserID;
                server.ConnectionContext.Password = consb.Password;
            }

            if (!server.ConnectionContext.IsOpen)
                server.ConnectionContext.Connect();

            return server;

        }

        private static Database TargetDatabase(SqlConnectionStringBuilder consb)
        {
            var db = TargerServer(consb).Databases[consb.InitialCatalog];

            return db;

        }
    }
}
