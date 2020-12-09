using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MsSqlCloneDb.Lib;
using MsSqlCloneDb.Lib.Common;

// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable AccessToDisposedClosure
// ReSharper disable UseStringInterpolation

namespace MsSqlCloneDb
{
    public partial class MainForm : Form, ILogSink, ILogger
    {
        private readonly CloneProcessor _cloneProcessor;

        private string _lastFileName;

        // ReSharper disable once InconsistentNaming
        private const string STR_BITTE_WAEHLEN = "(Bitte wählen)";

        public MainForm()
        {
            InitializeComponent();

            // damit werden die Parameters aus App.Config gelesen und an CloneProcessor weitergereicht
            var config = new CloneParametersExt(); 
            config.AdaptParameters();
            _cloneProcessor = new CloneProcessor(this);
            _cloneProcessor.SetConfig(config);
     

            cmbSource.Items.Add(STR_BITTE_WAEHLEN);
            cmbTarget.Items.Add(STR_BITTE_WAEHLEN);
            foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
            {
                if (connectionString.Name != "LocalSqlServer")
                {
                    cmbSource.Items.Add(connectionString.Name);
                }

                // Exclude PROD-DB from the target DBs
                if (connectionString.Name.ToUpper() != "ICLX_P_DB" &&
                    !connectionString.ConnectionString.ToUpper().Contains("ICLX_P_DB") &&
                    connectionString.Name.ToUpper() != "ICLX_DB_P" &&
                    connectionString.Name != "LocalSqlServer")
                {
                    cmbTarget.Items.Add(connectionString.Name);
                }
            }
            cmbSource.SelectedIndex = 0;
            cmbTarget.SelectedIndex = 0;

            // Window-Title
            Text = $@"{Application.ProductName} v.{Application.ProductVersion}";
        }

        private void BtnSaveSchemaClick(object sender, EventArgs e)
        {
            if (!CheckSourceDbSelection())
                return;

            saveFileDialog1.FileName = (string)cmbSource.SelectedItem;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            _lastFileName = saveFileDialog1.FileName;
            Cursor = Cursors.WaitCursor;
            EnableDisableButtons(false);
            try
            {
                AddBoldLogEntry("Schema wird ermittelt", Color.DarkBlue);

                _cloneProcessor.DoCreateSchema(saveFileDialog1.FileName, SourceConnStringBuilder);

                AddBoldLogEntry("Das Datenbankschema wurde erfolgreich gespeichert", Color.DarkBlue);
            }
            finally
            {
                Cursor = Cursors.Default;
                EnableDisableButtons(true);
            }
        }

        private void BtnTransferSchemaClick(object sender, EventArgs e)
        {
            if (!CheckTargetDbSelection())
                return;

            openFileDialog1.FileName = _lastFileName;
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            var tcsb = TargetConnStringBuilder;

            if (MessageBox.Show(
                    string.Format(
                        "Achtung: Es werden alle Daten in '{0}' ({1}\\{2}) gelöscht!\nWollen Sie das Datenbankschema wirklich übertragen?",
                        cmbTarget.SelectedItem,
                        tcsb.DataSource,
                        tcsb.InitialCatalog),
                    @"Fortsetzen?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            Cursor = Cursors.WaitCursor;
            EnableDisableButtons(false);
            try
            {
                AddBoldLogEntry("Laden Schema gestartet", Color.DarkBlue);

                _cloneProcessor.DoLoadSchema(TargetConnStringBuilder, openFileDialog1.FileName);

                AddBoldLogEntry("Laden Schema beendet", Color.DarkBlue);

            }
            finally
            {
                Cursor = Cursors.Default;
                EnableDisableButtons(true);
            }
        }

        private void BtnTransferDataClick(object sender, EventArgs e)
        {
            if (!CheckSourceDbSelection())
                return;

            if (!CheckTargetDbSelection())
                return;

            Cursor = Cursors.WaitCursor;
            EnableDisableButtons(false);
            AddBoldLogEntry("DatenTransfer gestartet", Color.DarkBlue);

            try
            {
                _cloneProcessor.DoTransferData(SourceConnStringBuilder, TargetConnStringBuilder);

                AddBoldLogEntry("DatenTransfer beendet", Color.DarkBlue);
            }
            finally
            {
                Cursor = Cursors.Default;
                EnableDisableButtons(true);
            }
        }

        private void BtnActivateClick(object sender, EventArgs e)
        {
            if (!CheckTargetDbSelection())
                return;

            openFileDialog1.InitialDirectory =
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\Activate scripts"));

            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            Cursor = Cursors.WaitCursor;
            EnableDisableButtons(false);

            try
            {
                AddBoldLogEntry("Aktivierung gestartet", Color.DarkBlue);
                Activate(openFileDialog1.FileName);
                AddBoldLogEntry("Aktivierung beendet", Color.DarkBlue);
            }
            finally
            {
                Cursor = Cursors.Default;
                EnableDisableButtons(true);
            }
        }

        private bool CheckSourceDbSelection()
        {
            if (cmbSource.SelectedIndex <= 0)
            {
                MessageBox.Show(@"Bitte wählen Sie eine Quelldatenbank", @"Quelle wählen", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool CheckTargetDbSelection()
        {
            if (cmbTarget.SelectedIndex <= 0)
            {
                MessageBox.Show(@"Bitte wählen Sie eine Zieldatenbank", @"Ziel wählen", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void EnableDisableButtons(bool isEnabled)
        {
            btnSaveSchema.Enabled = isEnabled;
            btnTransferSchema.Enabled = isEnabled;
            btnTransferData.Enabled = isEnabled;
            btnActivate.Enabled = isEnabled;

            cmbTarget.Enabled = isEnabled;
            cmbSource.Enabled = isEnabled;
        }


        #region ILogSink Members ----------------------------------------------

        public void AddLogEntry(string logEntry)
        {
            AddLogEntry(logEntry, Color.Black);
        }

        public void AddLogEntry(string logEntry, Color color)
        {
            rtfLog.AppendText(GetLogText(logEntry), color);
            Application.DoEvents();
        }

        public void AddBoldLogEntry(string logEntry)
        {
            AddBoldLogEntry(logEntry, Color.Black);
        }

        public void AddBoldLogEntry(string logEntry, Color color)
        {
            rtfLog.AppendBoldText(GetLogText(logEntry), color);
            Application.DoEvents();
        }

        private static string GetLogText(string logEntry)
        {
            Trace.TraceInformation(logEntry);

            var ret = $"{DateTime.Now.ToLongTimeString()} {logEntry}{Environment.NewLine}";

            return ret;
        }

        #endregion

        private void Activate(string fileName)
        {
            var streamReader = new StreamReader(fileName);
            var activateScript = streamReader.ReadToEnd();
            streamReader.Close();

            var tcsb = TargetConnStringBuilder;

            using (var dbConnection = new SqlConnection(tcsb.ConnectionString))
            {
                dbConnection.Open();
                AddBoldLogEntry("Zieldatenbank wird aktiviert");
                foreach (var sql in Helper.SplitSqlScript(activateScript))
                {
                    try
                    {
                        CloneProcessor.ExecuteCommand(sql, dbConnection, false);
                    }
                    catch (Exception e)
                    {

                        AddLogEntry(
                            string.Format("Fehler: Das Query konnte nicht ausgeführt werden:{2}{0}{2}Meldung:{2}{1}",
                                sql, e.Message, Environment.NewLine), Color.Red);
                    }
                }
                dbConnection.Close();
            }
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtfLog.Clear();
        }

        private void cmbSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowConnectionString(cmbSource);
        }

        private void cmbTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowConnectionString(cmbTarget);
        }

        private void ShowConnectionString(ComboBox cbo)
        {
            var selectedValue = cbo.SelectedItem.ToString();
            if (selectedValue == STR_BITTE_WAEHLEN || string.IsNullOrEmpty(selectedValue))
            {
                return;
            }

            var connString = ConfigurationManager.ConnectionStrings[selectedValue].ConnectionString;
            try
            {
                connString = EncryptionServiceFactory.Create().Decrypt(connString);
            }
            catch (Exception)
            {
                // ignore. if the string could not be decrypted it maybe was just not encrypted in the first place.
            }
            rtfLog.Text += cbo.Name + @":  " + connString + Environment.NewLine;
        }

        private SqlConnectionStringBuilder TargetConnStringBuilder => GetSelectedConnectionString((string)cmbTarget.SelectedItem);

        private SqlConnectionStringBuilder SourceConnStringBuilder => GetSelectedConnectionString((string)cmbSource.SelectedItem);

        private SqlConnectionStringBuilder GetSelectedConnectionString(string str)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[str].ConnectionString;
            try
            {
                connectionString = EncryptionServiceFactory.Create().Decrypt(connectionString);
            }
            catch (Exception)
            {
                // ignore. if the string could not be decrypted it maybe was just not encrypted in the first place.
            }

            var ret = new SqlConnectionStringBuilder(connectionString);
            return ret;
        }

        public void AddLog(string str)
        {
            AddLogEntry(str);
        }
    }
}
