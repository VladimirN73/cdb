using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo;

namespace cdb.Common
{
    public class DbBulkCopy : IDisposable
    {
        private readonly SqlConnectionStringBuilder _scsb;
        private readonly SqlConnectionStringBuilder _tcsb;
        private readonly IsolationLevel _isolationLevel;
        private readonly string _sourceDb;
        
        private Server _mySourceDbServer;

        public DbBulkCopy(
            SqlConnectionStringBuilder scsb,
            SqlConnectionStringBuilder tcsb, 
            IsolationLevel isolationLevel) 
        {
            _scsb = scsb;
            _tcsb = tcsb;
            _isolationLevel = isolationLevel;

            _sourceDb = scsb.InitialCatalog;

            _mySourceDbServer = new Server(scsb.DataSource);

            //Using windows authentication  //TODO  similar code (copy-paste) as in DbSchemaScripter.ctor
            _mySourceDbServer.ConnectionContext.LoginSecure = true;

            if (!string.IsNullOrEmpty(scsb.UserID))
            {
                _mySourceDbServer.ConnectionContext.LoginSecure = false;
                _mySourceDbServer.ConnectionContext.Login = scsb.UserID;
                _mySourceDbServer.ConnectionContext.Password = scsb.Password;
            }
        }

        #region IDisposable Member

        public void Dispose()
        {
            if (_mySourceDbServer.ConnectionContext.IsOpen)
                _mySourceDbServer.ConnectionContext.Disconnect();
            _mySourceDbServer = null;
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

        public void Copy(
            IReadOnlyCollection<string> tablesToSkip,
            IReadOnlyCollection<PartialTableTranfer> tablesPartialTransfer)
        {
            using DbConnection sourceConn = new SqlConnection(_scsb.ConnectionString);
            using DbConnection destConn = new SqlConnection(_tcsb.ConnectionString);

            tablesToSkip ??= new List<string>();

            tablesPartialTransfer ??= new List<PartialTableTranfer>();

            if (tablesToSkip.Count > 0)
            {
                HelperX.AddLog("Partial transfer for the following tables");
                foreach (var tbl in tablesToSkip)
                {
                    HelperX.AddLog($"-- {tbl}");
                }
            }

            sourceConn.Open();
            destConn.Open();

            var sourceTran = (SqlTransaction) sourceConn.BeginTransaction(_isolationLevel);
            
            if (!_mySourceDbServer.ConnectionContext.IsOpen)
            {
                _mySourceDbServer.ConnectionContext.DatabaseName = _sourceDb;
                _mySourceDbServer.ConnectionContext.Connect();
            }

            var db = _mySourceDbServer.Databases[_sourceDb];

            var tblIndex = 0;
            foreach (Table t in db.Tables)
            {
                tblIndex += 1;
                var tblCount = $"{tblIndex} / {db.Tables.Count}";
                if (tablesToSkip.Any(x => x.IsEqualToPattern(t.Schema, t.Name)) &&
                    tablesPartialTransfer.All(x => !x.TableName.IsEqualToPattern(t.Schema, t.Name)))
                {
                    HelperX.AddLog($@"-- skip table ({tblCount}) {t.Name}");
                    continue;
                }

                var strLog = $@"transfer table ({tblCount}) {t.Schema}.{t.Name}";
                var strWhere = "";

                var partialTransfer = tablesPartialTransfer
                    .FirstOrDefault(x => x.TableName.IsEqualToPattern(t.Schema, t.Name));

                if (partialTransfer != null)
                {
                    strWhere = partialTransfer.WhereCondition;
                    strLog += $" Condition '{strWhere}'";
                }

                HelperX.AddLog(strLog);

                TruncateTable(t, destConn);
                CopyTable(t, strWhere, sourceConn, sourceTran, destConn);
            }

            CloseConnection(sourceConn, sourceTran);
            CloseConnection(destConn);
        }

        private void CopyTable(Table table, string strWhere, DbConnection sourceConn, SqlTransaction sourceTran,
            DbConnection destConn)
        {
            try
            {
                using var cmd = sourceConn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $@"SELECT * FROM [{table.Schema}].[{table.Name}]";
                if (!string.IsNullOrEmpty(strWhere))
                {
                    cmd.CommandText += $@" WHERE {strWhere}";
                }

                cmd.CommandTimeout = 3000000;
                cmd.Transaction = sourceTran;

                using var bulkCopy = GetSqlBulkCopy(destConn);
                using var sourceDr = cmd.ExecuteReader(CommandBehavior.Default);

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
                catch (Exception ex)
                {
                    throw EvaluateException(ex, bulkCopy);
                }
            }
            catch (Exception ex)
            {
                HelperX.AddLog($"Error: Cannot transfer the table [{table.Schema}].[{table.Name}].");
                HelperX.AddLog("Stacktrace: " + ex);
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
            catch (Exception ex)
            {
                HelperX.AddLog("Error while closing connection: " + ex);
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

            using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = commandText;

            cmd.CommandTimeout = 30;

            cmd.ExecuteNonQuery();
        }

        private Exception EvaluateException(Exception ex, SqlBulkCopy bulkCopy)
        {
            if (ex.Message.Contains("Received an invalid column length from the bcp client for colid"))
            {
                try
                {
                    var pattern = @"\d+";
                    var match = Regex.Match(ex.Message, pattern);
                    var index = Convert.ToInt32(match.Value) - 1;

                    var fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", BindingFlags.NonPublic | BindingFlags.Instance);
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
