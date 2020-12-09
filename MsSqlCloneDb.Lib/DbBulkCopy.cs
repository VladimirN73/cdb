using Microsoft.SqlServer.Management.Smo;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable UseStringInterpolation

namespace MsSqlCloneDb.Lib
{
    public class DbBulkCopy : IDisposable
    {
        private readonly string _sourceServer;
        private readonly string _sourceDb;
        private readonly string _sourceUser;
        private readonly string _sourcePassword;
        private readonly string _sourceNetworkLibrary;

        private readonly string _destServer;
        private readonly string _destDb;
        private readonly string _destUser;
        private readonly string _destPassword;
        private readonly string _destNetworkLibrary;

        private Server _myServer;

        public DbBulkCopy(
            string sourceServer, string sourceDb, string sourceUser, string sourcePassword, string sourceNetworkLibrary,
            string destServer, string destDb, string destUser, string destPassword, string destNetworkLibrary)
        {
            _sourceServer = sourceServer;
            _sourceDb = sourceDb;
            _sourceUser = sourceUser;
            _sourcePassword = sourcePassword;
            _sourceNetworkLibrary = sourceNetworkLibrary;
            _destServer = destServer;
            _destDb = destDb;
            _destUser = destUser;
            _destPassword = destPassword;
            _destNetworkLibrary = destNetworkLibrary;

            _myServer = new Server(sourceServer);

            //Using windows authentication
            _myServer.ConnectionContext.LoginSecure = false;
            _myServer.ConnectionContext.Login = sourceUser;
            _myServer.ConnectionContext.Password = sourcePassword;
        }

        #region IDisposable Member

        public void Dispose()
        {
            if (_myServer.ConnectionContext.IsOpen)
                _myServer.ConnectionContext.Disconnect();
            _myServer = null;
        }

        #endregion IDisposable Member

        private static SqlBulkCopy GetSqlBulkCopy(DbConnection dbConnection, SqlTransaction transaction = null)
        {
            var options =
                SqlBulkCopyOptions.TableLock |
                SqlBulkCopyOptions.KeepIdentity |
                SqlBulkCopyOptions.KeepNulls;

            if (transaction == null)
            {
                options =
                    options |
                    SqlBulkCopyOptions.UseInternalTransaction;
            }

            var result = new SqlBulkCopy((SqlConnection)dbConnection, options, transaction)
            {
                BulkCopyTimeout = 14400
            };
            return result;
        }

        // ReSharper disable once UnusedParameter.Local
        private static string GetConnectionString(string server, string db, string user, string password, string networkLibrary)
        {
            var consb =
                new SqlConnectionStringBuilder(
                    "Data Source=;Network Library=;Initial Catalog=;Persist Security Info=True;User ID=;Password=;MultipleActiveResultSets=False")
                { DataSource = server, InitialCatalog = db, UserID = user, Password = password }; //, NetworkLibrary = networkLibrary };

            return consb.ConnectionString;
        }

        public void Copy(IReadOnlyCollection<string> tablesToSkip, IReadOnlyCollection<PartialTableTranfer> tablesPartialTransfer)
        {
            using (DbConnection sourceConn = new SqlConnection(GetConnectionString(_sourceServer, _sourceDb, _sourceUser, _sourcePassword, _sourceNetworkLibrary)))
            using (DbConnection destConn = new SqlConnection(GetConnectionString(_destServer, _destDb, _destUser, _destPassword, _destNetworkLibrary)))
            {
                if (tablesToSkip.Count > 0)
                {
                    HelperX.AddLog("Daten der folgenden Tabellen werden nicht komplett uebergetragen");
                    foreach (var tbl in tablesToSkip)
                    {
                        HelperX.AddLog($"-- {tbl}");
                    }
                }

                sourceConn.Open();
                destConn.Open();

                var sourceTran = (SqlTransaction)sourceConn.BeginTransaction(IsolationLevel.Snapshot);

                //var destTran = (SqlTransaction) destConn.BeginTransaction(isolationLevel: IsolationLevel.ReadCommitted);

                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetry(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    }, (exception, timeSpan, retryCount, context) =>
                    {
                        HelperX.AddLog($"Retry #{retryCount} after error");
                        CloseConnection(sourceConn, sourceTran);
                        CloseConnection(destConn);

                        sourceConn.Open();

                        destConn.Open();

                        sourceTran = (SqlTransaction)sourceConn.BeginTransaction(IsolationLevel.Snapshot);

                        TruncateTable(context["processed_table"] as Table, destConn);
                    });

                if (!_myServer.ConnectionContext.IsOpen)
                {
                    _myServer.ConnectionContext.DatabaseName = _sourceDb;
                    _myServer.ConnectionContext.Connect();
                }

                var db = _myServer.Databases[_sourceDb];

                var tblIndex = 0;
                foreach (Table t in db.Tables)
                {
                    tblIndex += 1;
                    var tblCount = $"{tblIndex} / {db.Tables.Count}";
                    if (tablesToSkip.Any(x => x.IsEqualToPattern(t.Schema, t.Name)) &&
                        tablesPartialTransfer.All(x => !x.TableName.IsEqualToPattern(t.Schema, t.Name)))
                    {
                        HelperX.AddLog($@"-- Ueberspringe Tabelle ({tblCount}) {t.Name}");
                        continue;
                    }

                    var strLog = $@"Uebertrage Tabelle ({tblCount}) {t.Schema}.{t.Name}";
                    var strWhere = "";

                    var partialTransfer = tablesPartialTransfer
                        .FirstOrDefault(x => x.TableName.IsEqualToPattern(t.Schema, t.Name));

                    if (partialTransfer != null)
                    {
                        strWhere = partialTransfer.WhereCondition;
                        strLog += $" Bedingung '{strWhere}'";
                    }

                    HelperX.AddLog(strLog);

                    var retryPolicyContext = new Polly.Context { { "processed_table", t } };
                    retryPolicy.Execute(context => CopyTable(t, strWhere, sourceConn, sourceTran, destConn), retryPolicyContext);
                }

                CloseConnection(sourceConn, sourceTran);
                CloseConnection(destConn);
            }
        }

        private void CopyTable(Table table, string strWhere, DbConnection sourceConn, SqlTransaction sourceTran,
            DbConnection destConn)
        {
            try
            {
                using (var cmd = sourceConn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = $@"SELECT * FROM [{table.Schema}].[{table.Name}]";
                    if (!string.IsNullOrEmpty(strWhere))
                    {
                        cmd.CommandText += $@" WHERE {strWhere}";
                    }

                    cmd.CommandTimeout = 3000000;
                    cmd.Transaction = sourceTran;

                    using (var bulkCopy = GetSqlBulkCopy(destConn)) // destTran
                    using (var sourceDr = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        // Fragment to test ICLX-8716
                        // Clone only 'GlobalConfigurationStructure' & 'GlobalConfigurationStructureLang'
                        // if (!t.Name.Contains("GlobalConfigurationStructure")) continue;

                        // Set the destination table name
                        bulkCopy.DestinationTableName = $@"[{table.Schema}].[{table.Name}]";

                        // Set the ColumnMappings and sort computed columns out
                        foreach (var column in table.Columns.Cast<Column>().Where(column => !column.Computed))
                        {
                            bulkCopy.ColumnMappings.Add(column.Name, column.Name);
                        }

                        try
                        {
                            bulkCopy.WriteToServer(sourceDr);
                        }
                        catch (Exception e)
                        {
                            throw EvaluateException(e, bulkCopy);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HelperX.AddLog($"Fehler: Die Tabelle [{table.Schema}].[{table.Name}] konnte nicht uebertragen werden.");
                HelperX.AddLog("Stacktrace: " + ex.ToString());
                throw;
            }
        }

        private void CloseConnection(DbConnection conn, SqlTransaction tx = null)
        {
            try
            {
                if (tx != null)
                {
                    tx.Rollback();
                }

                conn.Close();
            }
            catch (Exception e)
            {
                HelperX.AddLog("Error while closing connection: " + e.ToString());
            }
        }

        private void TruncateTable(Table table, DbConnection con)
        {
            if (table == null)
            {
                HelperX.AddLog("TruncateTable skipped - table not set");
                return;
            }
            HelperX.AddLog($"TruncateTable  [{table.Schema}].[{table.Name}]");

            var commandText = $"DELETE FROM [{table.Schema}].[{table.Name}]";

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = commandText;

                cmd.CommandTimeout = 30;

                cmd.ExecuteNonQuery();
            }
        }

        private Exception EvaluateException(Exception ex, SqlBulkCopy bulkCopy)
        {
            if (ex.Message.Contains("Received an invalid column length from the bcp client for colid"))
            {
                try
                {
                    string pattern = @"\d+";
                    Match match = Regex.Match(ex.Message, pattern);
                    var index = Convert.ToInt32(match.Value) - 1;

                    FieldInfo fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var sortedColumns = fi.GetValue(bulkCopy);
                    var items = (object[])sortedColumns.GetType()
                        .GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sortedColumns);

                    FieldInfo itemdata = items[index].GetType()
                        .GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
                    var metadata = itemdata.GetValue(items[index]);

                    var column = metadata.GetType()
                        .GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(metadata);
                    var length = metadata.GetType()
                        .GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(metadata);
                    throw new InvalidOperationException(
                        string.Format("Column: {0} contains data with a length greater than: {1}", column, length));
                }
                catch (Exception)
                {
                    throw ex;
                }
            }

            return ex;
        }
    }
}