using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace cdb.Common
{
    public class DbSchemaScripter : IDisposable
    {
        private Server _myServer;
        private readonly string _fileName;


        public DbSchemaScripter(string serverName, string userName, string password, string fileName)
        {
            _myServer = new Server(serverName);

            //Using windows authentication
            _myServer.ConnectionContext.LoginSecure = false;
            _myServer.ConnectionContext.Login = userName;
            _myServer.ConnectionContext.Password = password;

            _fileName = fileName;
        }

        public void Dispose()
        {
            if (_myServer.ConnectionContext.IsOpen)
                _myServer.ConnectionContext.Disconnect();
            _myServer = null;
        }


        #region public method -------------------------------------------------

        public void GenerateSchema(string database)
        {
            OpenConnectionContext(database);

            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }

            var db = _myServer.Databases[database];

            //GenerateRoles(db);
            GenerateDatabaseSchemas(db);
            GenerateTables(db);
            GenerateTableVariables(db);
            //GenerateStoreProcedures(db);
            //GenerateUserDefinedFunctions(db);
            GenerateTableRestrictions(db);
            GenerateViews(db);
            GenerateDatabaseSchemaPermissions(db);
        }

        public void GenerateBackup(string database, string tableName)
        {
            GenerateBackup(database, new[] { tableName }.ToList());
        }

        public void GenerateBackup(string database, List<string> tableNames)
        {
            HelperX.AddLog($"Generate the backup of '{database}'");

            OpenConnectionContext(database);

            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }

            var db = _myServer.Databases[database];

            GenerateTableBackup(db, tableNames);
        }
        public void ResetConnectionContext(string database)
        {
            CloseConnectionContext(database);
            OpenConnectionContext(database);
        }

        #endregion

        #region main generate methods -----------------------------------------

        private void GenerateRoles(Database db)
        {
            HelperX.AddLog("Process user roles ");

            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true
            };

            var roles = db.Roles.Cast<DatabaseRole>()
                .Where(r => !r.IsFixedRole && r.Name.ToUpper().StartsWith("CUSTOM_ROLER_"))
                .ToList();

            foreach (var role in roles)
            {
                HelperX.AddLog($"get Rolle '{role.Name}'");
                // Change Roles to dbo, due to the target db may do not contain other owners
                role.Owner = "dbo";
                role.Script(scriptOptions);
            }
        }

        private void GenerateDatabaseSchemas(Database database)
        {
            HelperX.AddLog("Process Database Schemas...");
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = false,
                DriIncludeSystemNames = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true,
                SchemaQualifyForeignKeysReferences = true,
                SchemaQualify = true
            };

            foreach (Schema schema in database.Schemas)
            {
                if (schema.Name == "dbo") continue;

                HelperX.AddLog($"get CREATE Schema '{schema.Name}'");
                schema.Script(scriptOptions);
            }
        }

        private void GenerateTables(Database database)
        {
            HelperX.AddLog("Process Tables...");
            /* With ScriptingOptions you can specify different scripting
             * options, for example to include IF NOT EXISTS, DROP
             * statements, output location etc*/
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = false,
                DriIncludeSystemNames = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true,
                SchemaQualifyForeignKeysReferences = true,
                SchemaQualify = true
            };

            foreach (Table myTable in database.Tables)
            {
                GenerateTable(scriptOptions, myTable);
            }
        }

        private bool _lastRunWithError;
        private void GenerateTable(ScriptingOptions scriptOptions, Table myTable)
        {
            try
            {
                GenerateTableInternal(scriptOptions, myTable);
                _lastRunWithError = false;
            }
            catch (Exception ex)
            {
                // Wenn es nicht die erste Ausnahme ist, dann nicht mehr wiederholen
                if (_lastRunWithError)
                {
                    _lastRunWithError = false; // reset flag
                    throw;
                }

                HelperX.AddLog($@"Ignored (once) Exception: {ex.Message}");

                _lastRunWithError = true;
            }

            // re-try if exception
            if (_lastRunWithError)
            {
                GenerateTableInternal(scriptOptions, myTable);
            }
        }

        private void GenerateTableInternal(ScriptingOptions scriptOptions, Table myTable)
        {
            HelperX.AddLog($"generate CREATE Table '{myTable.Name}'");
            // dump the table
            myTable.Script(scriptOptions);

            // dump the indexes 
            foreach (var myIndex in OrderIndexes(myTable.Indexes))
            {
                HelperX.AddLog($"generate CREATE INDEX '{myIndex.Name}'");
                myIndex.Script(scriptOptions);

                //if (myIndex.IsXmlIndex || myIndex.IndexKeyType.Equals(IndexKeyType.DriPrimaryKey)) continue;

                //var lines = File.ReadAllLines(_fileName, Encoding.Unicode);
                //File.WriteAllLines(_fileName, lines.Take(lines.Length - 1).ToArray(), Encoding.Unicode);
                //File.AppendAllText(_fileName, string.Format("ON [PRIMARY]{0}GO{0}", Environment.NewLine), Encoding.Unicode);

            }

            SetPermissions(myTable);
        }

        private void GenerateTableBackup(Database database, List<string> tableNames)
        {
            var tableNameList = tableNames.Select(x => x.ToLower().Trim()).ToList();

            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = false,
                DriIncludeSystemNames = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true,
                SchemaQualifyForeignKeysReferences = true,
                SchemaQualify = true,
                ScriptData = true,
                ScriptDrops = false,
                ScriptSchema = true,
            };

            foreach (Table dbTable in database.Tables)
            {
                if (dbTable.IsSystemObject) continue;

                var strTable = $"{dbTable.Name.ToLower()}";
                var strSchemaAndTable = $"{dbTable.Schema.ToLower()}.{strTable}";

                var strTableToRestore = tableNameList.FirstOrDefault(x => x == strTable || x == strSchemaAndTable);

                if (strTableToRestore.IsNullOrEmpty())
                {
                    continue;
                }

                HelperX.AddLog($" Backup Tabelle '{dbTable.Schema}.{dbTable.Name}' nach '{_fileName}'");

                var scripter = new Scripter(_myServer) { Options = scriptOptions };

                scripter.EnumScript(new[] { dbTable.Urn });

                tableNameList.Remove(strTableToRestore);
            }

            if (tableNameList.Any())
            {
                HelperX.AddLog(@" -------------------------");
                HelperX.AddLog(@"WARNING. The following backup-tables are not found in the target database");
                foreach (var str in tableNameList.OrderBy(x => x).ToList())
                {
                    HelperX.AddLog($" --- {str}");
                }
                HelperX.AddLog(@" -------------------------");
            }
        }

        private void GenerateTableVariables(Database db)
        {
            HelperX.AddLog("generate schemas of Table-Types functions");
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true
            };

            foreach (TableViewTableTypeBase schema in db.UserDefinedTableTypes)
            {
                HelperX.AddLog($"get Table-Type '{schema.Name}'");
                schema.Script(scriptOptions);
            }
        }

        private void GenerateStoreProcedures(Database db)
        {
            HelperX.AddLog("generate schemas of Stored Procedures");
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true
            };

            foreach (var sp in db.StoredProcedures.Cast<StoredProcedure>().Where(sp => !sp.IsSystemObject))
            {
                HelperX.AddLog($"get CREATE Stored Procedure '{sp.Name}'");
                sp.Script(scriptOptions);
            }
        }

        private void GenerateUserDefinedFunctions(Database db)
        {
            HelperX.AddLog("generate schemas of user-defined functions");
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true
            };

            var functionList = db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(f => !f.IsSystemObject).ToList();
            var sortedFunctionList = new List<UserDefinedFunction>();
            while (functionList.Count > 0)
                sortedFunctionList.AddRange(GetSortedFunctionList(functionList));

            foreach (var sp in sortedFunctionList)
            {
                if (sp.IsSystemObject)
                    continue;

                HelperX.AddLog($"get CREATE User defined Function '{sp.Name}'");
                sp.Script(scriptOptions);

                SetPermissions(sp);
            }
        }

        private void GenerateTableRestrictions(Database db)
        {
            HelperX.AddLog("Process constraints ...");
            /* With ScriptingOptions you can specify different scripting
             * options, for example to include IF NOT EXISTS, DROP
             * statements, output location etc*/
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = false,
                DriIncludeSystemNames = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true,
                SchemaQualifyForeignKeysReferences = true,
                SchemaQualify = true,
                Default = false,
                DriChecks = true,
                DriDefaults = true,
                DriForeignKeys = true
            };

            //Indexes (including  Clustered, NonClustered, PrimaryKeys & UniqueKeys) are created in function 'Generate tables'

            foreach (Table myTable in db.Tables)
            {
                HelperX.AddLog($"get CREATE DRI-Objects Script '{myTable.Name}'");
                myTable.Script(scriptOptions);
            }
        }

        private void GenerateViews(Database database)
        {
            /* With ScriptingOptions you can specify different scripting
             * options, for example to include IF NOT EXISTS, DROP
             * statements, output location etc*/
            var scriptOptions = new ScriptingOptions
            {
                FileName = _fileName,
                AppendToFile = true,
                ToFileOnly = true,
                IncludeIfNotExists = true,
                DriAll = true,
                NoCollation = true,
                NoFileGroup = true,
                Triggers = true,
                Permissions = false
            };

            // Default: false, Ausnahme siehe unten

            var orderedViews = new List<View>();

            foreach (var view in database.Views.Cast<View>().Where(view => !view.IsSystemObject))
            {
                var view1 = view;

                // this is quite primitive, but it will do for now
                foreach (var v in orderedViews.Where(v => v.TextBody.Contains(view1.Name)))
                {
                    orderedViews.Insert(orderedViews.IndexOf(v), view);
                    break;
                }
                if (!orderedViews.Contains(view))
                    orderedViews.Add(view);
            }

            foreach (var view in orderedViews)
            {
                view.Script(scriptOptions);

                HelperX.AddLog($"get CREATE View '{view.Name}'");

                foreach (var myIndex in OrderIndexes(view.Indexes))
                {
                    HelperX.AddLog($"get CREATE Index '{myIndex.Name}'");
                    myIndex.Script(scriptOptions);
                }

                SetPermissions(view);
            }
        }

        private void GenerateDatabaseSchemaPermissions(Database database)
        {
            foreach (Schema schema in database.Schemas)
            {
                SetPermissions(schema);
            }
        }
        #endregion


        public Table GetTableByName(string tableName, string database)
        {
            OpenConnectionContext(database);

            var db = _myServer.Databases[database];
            foreach (Table dbTable in db.Tables)
            {
                if (dbTable.IsSystemObject) continue;

                var strTable = $"{dbTable.Name.ToLower()}";
                var strSchemaAndTable = $"{dbTable.Schema.ToLower()}.{strTable}";

                if (tableName.Equals(strTable, StringComparison.InvariantCultureIgnoreCase) ||
                    tableName.Equals(strSchemaAndTable, StringComparison.InvariantCultureIgnoreCase))
                {
                    return dbTable;
                }
            }

            return null;
        }

        public Tuple<string, string> GetSchemaAndTable(string tableName, string database)
        {
            var table = GetTableByName(tableName, database);
            return new Tuple<string, string>(table.Schema, table.Name);
        }

        #region helper methods ------------------------------------------------

        private void OpenConnectionContext(string database)
        {
            if (!_myServer.ConnectionContext.IsOpen)
            {
                var connContext = _myServer.ConnectionContext;
                connContext.DatabaseName = database;
                connContext.Connect();
                HelperX.AddLog($"connection to the database '{database}' is open");
            }
        }

        private void CloseConnectionContext(string database)
        {
            if (_myServer.ConnectionContext.IsOpen)
            {
                _myServer.ConnectionContext.Disconnect();
                HelperX.AddLog($"connection to the database '{database}' is closed");
            }
        }

        private void SetPermissions(IObjectPermission obj)
        {
            var objectPermissionSet = new ObjectPermissionSet();
            var permissions = obj.EnumObjectPermissions(objectPermissionSet);

            if (permissions == null) return;

            var pList = permissions.Where(IsValidRole).ToList();

            foreach (var p in pList)
            {
                var grantDeny = p.PermissionState.ToString().ToUpper();

                var target = $"[{p.ObjectSchema}].[{p.ObjectName}]";

                if (p.ObjectClass.ToString().ToUpper() == "SCHEMA")
                {
                    target = $"SCHEMA :: [{p.ObjectName}]";
                }

                var str =
                    $"{grantDeny} {p.PermissionType} ON {target} TO [{p.Grantee}] AS [{p.Grantor}]";

                AddLineAndGo(str);
            }
        }

        private bool IsValidRole(PermissionInfo pInfo)
        {
            var ret =
                pInfo.GranteeType == PrincipalType.DatabaseRole &&
                pInfo.Grantee.ToUpper().StartsWith("CUSTOM_ROLE_") &&
                pInfo.Grantor.ToUpper() == "DBO";

            return ret;
        }

        private void AddLineAndGo(string str)
        {
            File.AppendAllLines(
                _fileName,
                new[]
                {
                    str,
                    "GO"
                },
                Encoding.Unicode);
        }

        private static List<UserDefinedFunction> GetSortedFunctionList(List<UserDefinedFunction> functionList)
        {
            var sortedList = new List<UserDefinedFunction>();
            foreach (var function in functionList.ToArray())
            {
                if (function.TextBody != null &&
                    !functionList.Any(f => function.TextBody.Contains(f.Name) &&
                    f.Name != function.Name))
                {
                    sortedList.Add(function);
                    functionList.Remove(function);
                }
            }
            return sortedList;
        }

        private static IEnumerable<Index> OrderIndexes(IEnumerable indexCol)
        {
            var result = new List<Index>();
            foreach (Index i in indexCol)
            {
                if (i.IsClustered)
                {
                    result.Insert(0, i);
                }
                else
                {
                    result.Add(i);
                }
            }
            return result;
        }
        #endregion
    }
}
