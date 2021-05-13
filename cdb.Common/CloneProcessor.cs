﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace cdb.Common
{
    public interface ICloneProcessor
    {
        bool Execute(CloneParameters config);
    }

    public class CloneProcessor : ICloneProcessor
    {
        public const string SqlClearDatabase = "SQL_ClearDatabase.sql";

        private readonly IAppLogger _logger;

        private readonly string
            _executionId; // use unique Id to enable parallel execution: the 'restore'-scripts will be not overwritten

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
            var isSuccess = DoLoadSchema(TargetBuilder, _config.schemaFile);
            if (!isSuccess)
            {
                throw new Exception($@"Error by {nameof(DoLoadSchema)}");
            }

            //// STEP
            //DoTransferData(SourceBuilder, TargetBuilder);

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

            // Exclude PROD-DB from the target DBs
            var isProd = !connectionString.ToLower().Contains("localhost"); // TODO magic string

            if (isProd)
            {
                throw new Exception("Prod-DB cannot used as a target DB ");
            }
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
            var strMethod = $@"{nameof(DoCreateSchema)}";

            using (new StackLogger(strMethod))
            {
                Log($@"source   :{source?.InitialCatalog}");
                Log($@"fileName :{schemaFile}");

                if (source == null)
                {
                    Log("Das Erstellen der Db-Schema wird übersprungen, da die Source-Db nicht vorhanden ist");
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

                Log($@"target    :{target?.InitialCatalog}");
                Log($@"schemaFile:{schemaFile}");

                if (!loadSchema)
                {
                    // TODO translate
                    // trace and return
                    Log(
                        "Das Laden der Db-Schema wird übersprungen, da das Schema-File oder die Ziel-Db nicht vorhanden sind");
                }
                else
                {
                    ClearTargetDatabase(target);
                    ret = ExecuteScriptFromFile(schemaFile, target);
                }

                return ret;
            }
        }

        protected void ClearTargetDatabase(SqlConnectionStringBuilder target)
        {
            EnsureNonProdDb(target);
            var ret = ExecuteScriptFromFile(SqlClearDatabase, target, false);
            if (!ret)
            {
                throw new Exception($"Error by {nameof(ClearTargetDatabase)} ");
            }
        }

        //TODO make it internal (or protected)
        public bool ExecuteUpdateScripts(SqlConnectionStringBuilder target)
        {
            var strMethod = $@"{nameof(ExecuteUpdateScripts)}";

            using (new StackLogger(strMethod))
            {
                return ExecuteScriptsByList(target, _config.updateScripts, false, false);
            }
        }

        //TODO make it internal (or protected)
        public bool ExecuteFinalScripts(SqlConnectionStringBuilder target)
        {
            var strMethod = $@"{nameof(ExecuteFinalScripts)}";

            using (new StackLogger(strMethod))
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
            Log($"Script '{fileName}' wird eingelesen");
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

            var strMethod = $@"{nameof(ExecuteScriptFromString)}";
            if (writeLog) Log($@"--> {strMethod}");

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

            if (writeLog) Log($@"<-- {strMethod}");

            return retSuccess;
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
                    HelperX.AddLog($"--- {strMethod} : SkipAllTables returns 'true', so no data-transfer is performed");
                }
                else if (string.IsNullOrEmpty(source?.ConnectionString))
                {
                    HelperX.AddLog($"--- {strMethod} : dbsource is missing, so no data-transfer is performed");
                }
                else
                {
                    //DisableAllCheckConstraintsOnTargetDb(target);
                    //DisableAllTriggersOnTargetDb(target);
                    //DisableAllIndexesOnTargetDb(target);

                    //BulkCopy(source, target);

                    //EnableAllCheckConstraintsOnTargetDb(target);
                    //EnableAllTriggersOnTargetDb(target);
                    //EnableAllIndexesOnTargetDb(target);
                }

                return ret;
            }
        }

    }
}
