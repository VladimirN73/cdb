using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Polly;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable InconsistentNaming


namespace MsSqlCloneDb.Lib
{
    public interface ICloneProcessor
    {
        void Execute(CloneParameters config);
    }

    public interface ILogger
    {
        void AddLog(string str);
    }

    public class CloneProcessor : ICloneProcessor
    {
        public const string SqlClearDatabase = "SQL_ClearDatabase.sql";

        public const string Backup_Script_Fragment = "SET DATEFORMAT YMD";

        protected CloneParameters _config;

        protected ILogger _logger;
        private readonly string _executionId; // use unique Id to enable parallel execution: the 'restore'-scripts will be not overwritten

        public CloneProcessor(): this(null)
        {
        }

        public CloneProcessor(ILogger logger)
        {
            _logger = logger;

            //_executionId = Guid.NewGuid().ToString().Replace("-","");
            _executionId = DateTime.Now.Ticks.ToString();
        }

        public string FileNamePrefixForBackupMerge => $"{_executionId}_backup_merge_";
        public string FileNamePrefixForBackupRestore => $"{_executionId}_backup_restore_";

        private SqlConnectionStringBuilder TargetBuilder => BuilderByConnectionString(_config.dbTargetConnectionString);

        private SqlConnectionStringBuilder SourceBuilder => BuilderByConnectionString(_config.dbSourceConnectionString);

        private void AddLog(string log)
        {
            _logger?.AddLog(log);
            HelperX.AddLog(log);
        }

        private SqlConnectionStringBuilder BuilderByConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            return new SqlConnectionStringBuilder(connectionString);
        }

        public void Execute(CloneParameters config)
        {
            SetConfig(config);

            CheckConnections();

            // einmaliges Wiederholen des gesamten Ablaufs bei nicht bereits behandelten Exceptions
            var retryPolicy = Policy
                .Handle<Exception>()
                .Retry(1, onRetry: (exception, retryCount) =>
                {
                    AddLog("==========================================================");
                    AddLog("Exception in Execute: " + exception);
                    AddLog(String.Empty);
                    AddLog("Retry Execution");
                    AddLog("==========================================================");
                });

            retryPolicy.Execute(() =>
            {
                // STEP
                // Create Db Schema file only if the parameter 'dbSource' is provided. 
                DoCreateSchema();

                // STEP
                //CreateBackupForTablesToRestore(TargetBuilder);
                //CreateBackupForTablesToMerge(TargetBuilder);

                //// STEP
                var isSuccess = DoLoadSchema(TargetBuilder, _config.schemaFile);
                if (!isSuccess)
                {
                    throw new Exception($@"Error by {nameof(DoLoadSchema)}");
                }

                //// STEP
                DoTransferData(SourceBuilder, TargetBuilder);

                //// STEP
                //if (!ExecuteUpdateScripts(TargetBuilder))
                //{
                //    throw new Exception($@"Error by {nameof(ExecuteUpdateScripts)}");
                //}

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
                //if (!ExecuteFinalScripts(TargetBuilder))
                //{
                //    throw new Exception($@"Error by {nameof(ExecuteFinalScripts)}");
                //}
            });
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

        private void CheckConnections()
        {
                if (SourceBuilder != null)
                {
                    CheckConnection(SourceBuilder.ConnectionString);
                    AddLog(@"Checking Source-Connection: successful");
                }

                if (TargetBuilder != null)
                {
                    CheckConnection(TargetBuilder.ConnectionString);
                    AddLog(@"Checking Target-Connection: successful");
                }
        }

        private void CheckConnection(string connectionString)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                dbConnection.Open();
                dbConnection.Close();
            }
        }

        public void DoCreateSchema(string schemaFile, SqlConnectionStringBuilder source)
        {
            var strMethod = $@"{nameof(DoCreateSchema)}";

            using (new StackLogger(strMethod))
            {
                AddLog($@"source   :{source?.InitialCatalog}");
                AddLog($@"fileName :{schemaFile}");

                if (source == null)
                {
                    AddLog(
                        "Das Erstellen der Db-Schema wird übersprungen, da die Source-Db nicht vorhanden ist");
                    return;
                }

                var retryPolicy = Policy.Handle<Exception>().Retry();
                retryPolicy.Execute(() => GenerateSchema(schemaFile, source));
            }
        }

        private void GenerateSchema(string schemaFile, SqlConnectionStringBuilder source)
        {
            try
            {
                using (var scripter = new DbSchemaScripter(source.DataSource, source.UserID, source.Password, schemaFile))
                {
                    scripter.GenerateSchema(source.InitialCatalog);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Fehler in GenerateSchema {source.InitialCatalog}");
                AddLog("Stacktrace: " + ex.ToString());
                throw;
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

        public bool DoLoadSchema(SqlConnectionStringBuilder target, string schemaFile)
        {
            EnsureNonProdDb(target);

            var strMethod = $@"{nameof(DoLoadSchema)}";

            using (new StackLogger(strMethod))
            {
                var ret = true;

                // Load Schema only if source and target are provided. 
                var loadSchema = !string.IsNullOrEmpty(schemaFile) &&
                                 !string.IsNullOrEmpty(target?.ConnectionString);

                AddLog($@"target    :{target?.InitialCatalog}");
                AddLog($@"schemaFile:{schemaFile}");

                if (!loadSchema)
                {
                    // trace and return
                    AddLog(
                        "Das Laden der Db-Schema wird übersprungen, da das Schema-File oder die Ziel-Db nicht vorhanden sind");
                }
                else
                {
                    ret = ret && ExecuteScriptFromFile(SqlClearDatabase, target);
                    ret = ret && ExecuteScriptFromFile(schemaFile, target);
                }

                return ret;
            }
        }

        public bool DoTransferData(
            SqlConnectionStringBuilder source,
            SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);

            var strMethod = $@"{nameof(DoTransferData)}";

            using (new StackLogger(strMethod))
            {
                var ret = true;

                TraceConfigDatabases();

                if (_config.SkipAllTables)
                {
                    AddLog($"--- {strMethod} : SkipAllTables returns 'true', so no data-transfer is performed");
                }
                else if (string.IsNullOrEmpty(source?.ConnectionString))
                {
                    AddLog($"--- {strMethod} : dbsource is missing, so no data-transfer is performed");
                }
                else
                {
                    DisableAllCheckConstraintsOnTargetDb(target);
                    DisableAllTriggersOnTargetDb(target);
                    DisableAllIndexesOnTargetDb(target);

                    BulkCopy(source, target);

                    EnableAllCheckConstraintsOnTargetDb(target);
                    EnableAllTriggersOnTargetDb(target);
                    EnableAllIndexesOnTargetDb(target);
                }

                return ret;
            }
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

            // Exclude PROD-DB from the target DBs
            var isProd = connectionString.ToUpper().Contains("ICLX_P_DB");

            if (isProd)
            {
                throw new Exception("Prod-DB darf nicht als target-Db benutzt werden");
            }
        }

        public void TraceConfigDatabases()
        {
            AddLog("==========================================================");
            AddLog($"=== source : {SourceBuilder?.InitialCatalog}");
            AddLog($"=== target : {TargetBuilder?.InitialCatalog}");
            AddLog("==========================================================");
        }

        public static int ExecuteCommand(
            string commandText,
            SqlConnection connection,
            bool writelog = true)
        {
            var strMethod = $@"{nameof(ExecuteCommand)}";

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
                if (writelog) HelperX.AddLog($@"{strMethod}: Rows={affectedRowsStr} - {commandText}");
            }
        }

        protected List<string> CreateBackupForTablesToRestore(SqlConnectionStringBuilder target)
        {
            var strMethod = $@"{nameof(CreateBackupForTablesToRestore)}";

            using (new StackLogger(strMethod))
            {
                var ret = CreateBackupForTables(target, TablesToRestore, FileNamePrefixForBackupRestore);
                return ret;
            }
        }

        public bool RestoreBackupForTablesToRestore(SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);

            var strMethod = $@"{nameof(RestoreBackupForTablesToRestore)}";

            using (new StackLogger(strMethod))
            {
                foreach (var tableName in TablesToRestore)
                {
                    AddLog($@" {tableName}");

                    var fileName = GetFileNameForBackUp(tableName, FileNamePrefixForBackupRestore);

                    if (File.Exists(fileName))
                    {
                        // before RESTORE ensure that the Table is empty
                        var str = "DELETE FROM " + tableName;
                        if (!ExecuteScriptFromString(str, target, false, false))
                        {
                            return false;
                        }

                        if (!ExecuteScriptFromFile(fileName, target, writeScriptOutputLog: true))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        AddLog($@"WARNING: File '{fileName}' not found. Table {tableName} is not restored. Probably it was not available before clone.");
                    }
                }
            }

            return true;
        }

        public List<string> CreateBackupForTablesToMerge(SqlConnectionStringBuilder target, string fileSuffix = null)
        {
            var strMethod = $@"{nameof(CreateBackupForTablesToMerge)}";

            using (new StackLogger(strMethod))
            {
                var ret = CreateBackupForTables(target, TablesToMerge, FileNamePrefixForBackupMerge, fileSuffix);
                return ret;
            }
        }

        private List<string> CreateBackupForTables(
            SqlConnectionStringBuilder target,
            IEnumerable<string> tables,
            string filePrefix,
            string fileSuffix = null)
        {
            EnsureNonProdDb(target);

            var retCreatedFiles = new List<string>();

            foreach (var tableName in tables)
            {
                var strFile = CreateBackupForSingleTable(target, tableName, filePrefix, fileSuffix);
                retCreatedFiles.Add(strFile);
            }

            return retCreatedFiles;
        }

        protected string CreateBackupForSingleTable(
            SqlConnectionStringBuilder target,
            string tableName,
            string filePrefix,
            string fileSuffix = null)
        {
            EnsureNonProdDb(target);

            var strMethod = $@"{nameof(CreateBackupForSingleTable)}({tableName})";
            using (new StackLogger(strMethod))
            {
                var fileName = GetFileNameForBackUp(tableName, filePrefix, fileSuffix);
                if (File.Exists(fileName))
                {
                    var strOldFile = fileName + ".bak";
                    File.Delete(strOldFile);
                    File.Copy(fileName, strOldFile);
                }

                var fileInfo = new FileInfo(fileName);

                using (var scripter = new DbSchemaScripter(target.DataSource, target.UserID, target.Password, fileName))
                {
                    scripter.GenerateBackup(target.InitialCatalog, tableName);
                }

                // adapt the resulting script (if it was created). 
                // Add 'Backup_Script_Fragment' as first line, 
                // to avoid error 'Bei der Konvertierung eines nvarchar-Datentyps in einen datetime-Datentyp liegt der Wert außerhalb des gültigen Bereichs'
                if (File.Exists(fileName))
                {
                    var currentContent = File.ReadAllText(fileName);
                    File.WriteAllText(fileName, Backup_Script_Fragment + Environment.NewLine + currentContent);
                }

                return fileInfo.FullName;
            }
        }

        public bool RestoreBackupForTablesToMerge(SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);

            var strMethod = $@"{nameof(RestoreBackupForTablesToMerge)}";

            using (new StackLogger(strMethod))
            {
                return RestoreBackupForTablesToMergeInternal(target);
            }
        }

        private bool RestoreBackupForTablesToMergeInternal(SqlConnectionStringBuilder target)
        {
            if (TablesToMerge.Count < 1)
            {
                return true;
            }

            // Backup the entries copied from Target to Source
            CreateBackupForTablesToMerge(target, "_source");

            var tableNames = TablesToMerge;

            using (var dbConnection = new SqlConnection(target.ConnectionString))
            {
                dbConnection.Open();

                if (!ExecuteMergeSchemaScripts(dbConnection))
                {
                    return false;
                }

                using (var scripter = new DbSchemaScripter(target.DataSource, target.UserID, target.Password, null))
                {
                    // in first cycle the data will be moved into temp-tables
                    foreach (var tableName in tableNames)
                    {
                        var strTuple = scripter.GetSchemaAndTable(tableName, target.InitialCatalog);
                        var strSchemaAndTable = $"{strTuple.Item1}.{strTuple.Item2}";

                        var tempTable = "#" + strTuple.Item2;

                        AddLog($"Restore for merge '{strSchemaAndTable}'. Move merge-data into '{tempTable}'");

                        // copy data from real-table to temp-table
                        var strScriptCopy =
                            $"IF OBJECT_ID('tempdb.dbo.{tempTable}', 'U') IS NOT NULL DROP TABLE {tempTable}; \n" +
                            $"select * into {tempTable} from {strSchemaAndTable};";

                        if (!ExecuteScriptFromString(strScriptCopy, dbConnection, writeLog: true,
                            continueOnError: false))
                        {
                            return false;
                        }
                    }

                    // in second cycle the original data will be restored
                    foreach (var tableName in tableNames)
                    {
                        var strTuple = scripter.GetSchemaAndTable(tableName, target.InitialCatalog);
                        var strSchemaAndTable = $"{strTuple.Item1}.{strTuple.Item2}";

                        // make real-table free
                        var strScriptDelete = $"delete from {strSchemaAndTable}";
                        if (!ExecuteScriptFromString(strScriptDelete, dbConnection, writeLog: true,
                            continueOnError: false))
                        {
                            return false;
                        }

                        // restore real-table from backup
                        var fileName = GetFileNameForBackUp(tableName, FileNamePrefixForBackupMerge);
                        if (!ExecuteScriptFromFile(fileName, dbConnection, writeLog: true))
                        {
                            return false;
                        }
                    }
                }

                if (!ExecuteMergeScripts(dbConnection))
                {
                    return false;
                }

                dbConnection.Close();
            }

            return true;
        }

        private static string GetFileNameForBackUp(string tablename, string filePrefix, string fileSuffix = null)
        {
            var fileNameSuffix = fileSuffix ?? "";
            var ret = filePrefix + tablename + fileNameSuffix + ".sql";
            return ret;
        }

        public List<string> TablesToRestore => _config.restoreTables;

        public List<string> TablesToMerge => _config.mergeTables;

        public List<string> TablesToSkip => _config.skipTables;

        public List<PartialTableTranfer> PartialTransfer => _config.PartialTransfer;

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
                    var indexList = table.Indexes.Cast<Index>()
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
                        var indexList = v.Indexes.Cast<Index>().ToList();

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
                        Trace.TraceInformation($"{strMethod}. Error by View {v.Name}");
                        Trace.TraceError(ex.Message);
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
            HelperX.AddLog("Enabling all check constraints on target database.");
            ModifyChecksOnTargetDb("ALTER TABLE [{0}].[{1}] CHECK CONSTRAINT {2}", target);
        }

        private static void DisableAllCheckConstraintsOnTargetDb(SqlConnectionStringBuilder target)
        {
            HelperX.AddLog("Disabling all check constraints on target database.");
            ModifyChecksOnTargetDb("ALTER TABLE [{0}].[{1}] NOCHECK CONSTRAINT {2}", target);
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

            using (var copier = new DbBulkCopy(
                scsb.DataSource, scsb.InitialCatalog, scsb.UserID, scsb.Password, scsb.NetworkLibrary,
                tcsb.DataSource, tcsb.InitialCatalog, tcsb.UserID, tcsb.Password, tcsb.NetworkLibrary))
            {
                AddLog("Daten werden uebertragen");
                copier.Copy(TablesToSkip, PartialTransfer);
            }
        }

        protected bool ExecuteUpdateScripts(SqlConnectionStringBuilder target)
        {
            var strMethod = $@"{nameof(ExecuteUpdateScripts)}";

            using (new StackLogger(strMethod))
            {
                return ExecuteScriptsByList(target, _config.updateScripts, false, false);
            }
        }

        protected bool ExecuteFinalScripts(SqlConnectionStringBuilder target)
        {
            var strMethod = $@"{nameof(ExecuteFinalScripts)}";

            using (new StackLogger(strMethod))
            {
                return ExecuteScriptsByList(target, _config.finalScripts, false, false);
            }
        }

        protected bool ExecuteMergeSchemaScripts(SqlConnection dbConnection)
        {
            var strMethod = $@"{nameof(ExecuteMergeSchemaScripts)}";

            using (new StackLogger(strMethod))
            {
                return ExecuteScriptsByList(dbConnection, _config.mergeSchemaScripts, false, false);
            }
        }

        protected bool ExecuteMergeScripts(SqlConnection dbConnection)
        {
            var strMethod = $@"{nameof(ExecuteMergeScripts)}";

            using (new StackLogger(strMethod))
            {
                return ExecuteScriptsByList(dbConnection, _config.mergeScripts, false, false);
            }
        }

        protected bool ExecuteScriptsByList(SqlConnectionStringBuilder target, List<ScriptInfo> scripts, bool writeLog, bool continueOnError)
        {
            bool ret;
            using (var dbConnection = new SqlConnection(target.ConnectionString))
            {
                dbConnection.Open();

                ret = ExecuteScriptsByList(dbConnection, scripts, writeLog, continueOnError, writeScriptOutputLog: true);

                dbConnection.Close();
            }

            return ret;
        }

        protected bool ExecuteScriptsByList(SqlConnection dbConnection, List<ScriptInfo> scripts, bool writeLog, bool continueOnError, bool writeScriptOutputLog = false)
        {
            var retSuccess = true;
            foreach (var script in scripts)
            {
                AddLog($@" {script.ScriptName}");
                var flagSuccess = ExecuteScriptFromString(script.ScriptText, dbConnection, writeLog, continueOnError,
                    writeScriptOutputLog);
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
            bool continueOnError = true, bool writeScriptOutputLog = false)
        {
            EnsureNonProdDb(target);
            bool ret;
            using (var dbConnection = new SqlConnection(target.ConnectionString))
            {
                dbConnection.Open();

                ret = ExecuteScriptFromFile(fileName, dbConnection, continueOnError: continueOnError);

                dbConnection.Close();
            }

            return ret;
        }

        private bool ExecuteScriptFromFile(string fileName, SqlConnection dbConnection, bool writeLog = false,
            bool continueOnError = true, bool writeScriptOutputLog = false)
        {
            EnsureNonProdDb(dbConnection.ConnectionString);

            var str = GetScriptFromFile(fileName);

            str = str.NormaliseEndOfLine();

            return ExecuteScriptFromString(str, dbConnection, writeLog, continueOnError, writeScriptOutputLog);
        }

        private string GetScriptFromFile(string fileName)
        {
            AddLog($"Script '{fileName}' wird eingelesen");
            if (!File.Exists(fileName))
            {
                AddLog($"WARNING: File '{fileName}' not found");
                return null;
            }

            var ret = HelperX.ReadStringFromFile(fileName);

            return ret;
        }

        public string ExecuteSelect(
            string strSelect,
            SqlConnection dbConnection,
            bool writeLog = false)
        {
            EnsureNonProdDb(dbConnection.ConnectionString);

            var strMethod = $@"{nameof(ExecuteSelect)}";

            if (writeLog) AddLog($@"--> {strMethod}");

            string ret;

            try
            {
                if (writeLog) AddLog(strSelect);

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

                var r = Serialize(reader);
                ret = JsonConvert.SerializeObject(r, Formatting.Indented);

            }
            catch (Exception ex)
            {
                AddLog(
                    string.Format("Fehler: Das Query konnte nicht ausgeführt werden:{2}{0}{2}Meldung:{2}{1}",
                        strSelect, ex.Message, Environment.NewLine));

                throw;
            }
            finally
            {
                if (writeLog) AddLog($@"<-- {strMethod}");
            }

            return ret;
        }

        private static IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            return results;
        }

        private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
        {
            var ret = cols.ToDictionary(col => col, col => reader[col]);
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
            bool continueOnError,
            bool writeScriptOutputLog = false)
        {
            EnsureNonProdDb(dbConnection.ConnectionString);

            var strMethod = $@"{nameof(ExecuteScriptFromString)}";
            if (writeLog) AddLog($@"--> {strMethod}");

            var retSuccess = true; // no error yet

            var scriptNormalized = scriptString.NormaliseEndOfLine();

            var listBlocks = HelperX.SplitSqlScript(scriptNormalized);

            if (writeScriptOutputLog)
            {
                CommandOutputCollector.AddOutputCollectorHandler(dbConnection);
            }
            else
            {
                CommandOutputCollector.RemoveOutputCollectorHandler(dbConnection);
            }

            foreach (var sqlBlock in listBlocks)
            {
                try
                {
                    ExecuteCommand(sqlBlock, dbConnection, writeLog);
                    var output = CommandOutputCollector.instance.GetOutput();
                    if (output.Any())
                    {
                        AddLog("Script Output");
                        foreach (var line in output)
                        {
                            AddLog("\t" + line);
                        }
                    }
                    CommandOutputCollector.instance.Reset();
                }
                catch (Exception ex)
                {
                    AddLog(
                        string.Format("Fehler: Das Query konnte nicht ausgeführt werden:{2}{0}{2}Meldung:{2}{1}",
                            sqlBlock, ex.Message, Environment.NewLine));

                    retSuccess = false;

                    if (!continueOnError) break;
                }
            }

            if (writeLog) AddLog($@"<-- {strMethod}");

            return retSuccess;
        }

        private static Server TargerServer(SqlConnectionStringBuilder consb)
        {
            var server = new Server(consb.DataSource);
            server.ConnectionContext.LoginSecure = false;
            server.ConnectionContext.Login = consb.UserID;
            server.ConnectionContext.Password = consb.Password;

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

    internal class StackLogger : IDisposable
    {
        private readonly string _strMethod;
        private readonly bool _writeLog;

        public StackLogger(string strMethod, bool writeLog = true)
        {
            _strMethod = strMethod;
            _writeLog = writeLog;

            if (_writeLog) AddLog($@"--> {_strMethod}");
        }

        public void Dispose()
        {
            if (_writeLog) AddLog($@"<-- {_strMethod}");
        }

        private void AddLog(string log)
        {
            Console.Out.WriteLine(log);
            Trace.TraceInformation(log);
        }
    }

    public class PartialTableTranfer
    {
        public string TableName;
        public string WhereCondition;
    }

}
