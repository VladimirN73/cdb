namespace MsSqlCloneDb
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbSource = new System.Windows.Forms.ComboBox();
            this.cmbTarget = new System.Windows.Forms.ComboBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.btnSaveSchema = new System.Windows.Forms.Button();
            this.btnTransferSchema = new System.Windows.Forms.Button();
            this.btnTransferData = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speichernToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rtfLog = new System.Windows.Forms.RichTextBox();
            this.btnActivate = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panelControls = new System.Windows.Forms.Panel();
            this.panelCenter = new System.Windows.Forms.Panel();
            this.contextMenuStrip1.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelControls.SuspendLayout();
            this.panelCenter.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Right;
            this.label1.Location = new System.Drawing.Point(421, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 37);
            this.label1.TabIndex = 0;
            this.label1.Text = "Quellverbindung";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Right;
            this.label2.Location = new System.Drawing.Point(431, 37);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 37);
            this.label2.TabIndex = 2;
            this.label2.Text = "Zielverbindung";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmbSource
            // 
            this.cmbSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSource.FormattingEnabled = true;
            this.cmbSource.Location = new System.Drawing.Point(541, 4);
            this.cmbSource.Margin = new System.Windows.Forms.Padding(4);
            this.cmbSource.MinimumSize = new System.Drawing.Size(100, 0);
            this.cmbSource.Name = "cmbSource";
            this.cmbSource.Size = new System.Drawing.Size(399, 24);
            this.cmbSource.TabIndex = 1;
            this.cmbSource.SelectedIndexChanged += new System.EventHandler(this.cmbSource_SelectedIndexChanged);
            // 
            // cmbTarget
            // 
            this.cmbTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTarget.FormattingEnabled = true;
            this.cmbTarget.Location = new System.Drawing.Point(541, 41);
            this.cmbTarget.Margin = new System.Windows.Forms.Padding(4);
            this.cmbTarget.MinimumSize = new System.Drawing.Size(100, 0);
            this.cmbTarget.Name = "cmbTarget";
            this.cmbTarget.Size = new System.Drawing.Size(399, 24);
            this.cmbTarget.TabIndex = 3;
            this.cmbTarget.SelectedIndexChanged += new System.EventHandler(this.cmbTarget_SelectedIndexChanged);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "sql";
            this.saveFileDialog1.Filter = "sql files (*.sql)|*.sql|All files (*.*)|*.*";
            this.saveFileDialog1.RestoreDirectory = true;
            // 
            // btnSaveSchema
            // 
            this.btnSaveSchema.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSaveSchema.Location = new System.Drawing.Point(5, 5);
            this.btnSaveSchema.Margin = new System.Windows.Forms.Padding(9);
            this.btnSaveSchema.Name = "btnSaveSchema";
            this.btnSaveSchema.Size = new System.Drawing.Size(367, 28);
            this.btnSaveSchema.TabIndex = 7;
            this.btnSaveSchema.Text = "1. Quellschema speichern";
            this.btnSaveSchema.UseVisualStyleBackColor = true;
            this.btnSaveSchema.Click += new System.EventHandler(this.BtnSaveSchemaClick);
            // 
            // btnTransferSchema
            // 
            this.btnTransferSchema.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnTransferSchema.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnTransferSchema.Location = new System.Drawing.Point(5, 33);
            this.btnTransferSchema.Margin = new System.Windows.Forms.Padding(4);
            this.btnTransferSchema.Name = "btnTransferSchema";
            this.btnTransferSchema.Size = new System.Drawing.Size(367, 28);
            this.btnTransferSchema.TabIndex = 8;
            this.btnTransferSchema.Text = "2. Schema in Zieldatenbank laden";
            this.btnTransferSchema.UseVisualStyleBackColor = true;
            this.btnTransferSchema.Click += new System.EventHandler(this.BtnTransferSchemaClick);
            // 
            // btnTransferData
            // 
            this.btnTransferData.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnTransferData.Location = new System.Drawing.Point(5, 61);
            this.btnTransferData.Margin = new System.Windows.Forms.Padding(4);
            this.btnTransferData.Name = "btnTransferData";
            this.btnTransferData.Size = new System.Drawing.Size(367, 28);
            this.btnTransferData.TabIndex = 9;
            this.btnTransferData.Text = "3. Daten übertragen";
            this.btnTransferData.UseVisualStyleBackColor = true;
            this.btnTransferData.Click += new System.EventHandler(this.BtnTransferDataClick);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearLogToolStripMenuItem,
            this.speichernToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(144, 52);
            // 
            // clearLogToolStripMenuItem
            // 
            this.clearLogToolStripMenuItem.Name = "clearLogToolStripMenuItem";
            this.clearLogToolStripMenuItem.Size = new System.Drawing.Size(143, 24);
            this.clearLogToolStripMenuItem.Text = "Clear Log";
            this.clearLogToolStripMenuItem.Click += new System.EventHandler(this.clearLogToolStripMenuItem_Click);
            // 
            // speichernToolStripMenuItem
            // 
            this.speichernToolStripMenuItem.Enabled = false;
            this.speichernToolStripMenuItem.Name = "speichernToolStripMenuItem";
            this.speichernToolStripMenuItem.Size = new System.Drawing.Size(143, 24);
            this.speichernToolStripMenuItem.Text = "Speichern";
            // 
            // rtfLog
            // 
            this.rtfLog.ContextMenuStrip = this.contextMenuStrip1;
            this.rtfLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtfLog.Location = new System.Drawing.Point(0, 0);
            this.rtfLog.Margin = new System.Windows.Forms.Padding(4);
            this.rtfLog.Name = "rtfLog";
            this.rtfLog.Size = new System.Drawing.Size(944, 365);
            this.rtfLog.TabIndex = 10;
            this.rtfLog.Text = "";
            // 
            // btnActivate
            // 
            this.btnActivate.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnActivate.Location = new System.Drawing.Point(5, 89);
            this.btnActivate.Margin = new System.Windows.Forms.Padding(4);
            this.btnActivate.Name = "btnActivate";
            this.btnActivate.Size = new System.Drawing.Size(367, 28);
            this.btnActivate.TabIndex = 10;
            this.btnActivate.Text = "4. Activieren";
            this.btnActivate.UseVisualStyleBackColor = true;
            this.btnActivate.Click += new System.EventHandler(this.BtnActivateClick);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.tableLayoutPanel1);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(944, 74);
            this.panelTop.TabIndex = 12;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56.88559F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 43.11441F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmbTarget, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.cmbSource, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.61728F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 49.38272F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(944, 74);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.panelControls);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 439);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(944, 123);
            this.panelBottom.TabIndex = 13;
            // 
            // panelControls
            // 
            this.panelControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panelControls.Controls.Add(this.btnActivate);
            this.panelControls.Controls.Add(this.btnTransferData);
            this.panelControls.Controls.Add(this.btnTransferSchema);
            this.panelControls.Controls.Add(this.btnSaveSchema);
            this.panelControls.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelControls.Location = new System.Drawing.Point(567, 0);
            this.panelControls.MinimumSize = new System.Drawing.Size(100, 0);
            this.panelControls.Name = "panelControls";
            this.panelControls.Padding = new System.Windows.Forms.Padding(5);
            this.panelControls.Size = new System.Drawing.Size(377, 123);
            this.panelControls.TabIndex = 8;
            // 
            // panelCenter
            // 
            this.panelCenter.Controls.Add(this.rtfLog);
            this.panelCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCenter.Location = new System.Drawing.Point(0, 74);
            this.panelCenter.Name = "panelCenter";
            this.panelCenter.Size = new System.Drawing.Size(944, 365);
            this.panelCenter.TabIndex = 14;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 562);
            this.Controls.Add(this.panelCenter);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(409, 472);
            this.Name = "MainForm";
            this.Text = "MsSqlCloneDb";
            this.contextMenuStrip1.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelControls.ResumeLayout(false);
            this.panelCenter.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbSource;
        private System.Windows.Forms.ComboBox cmbTarget;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Button btnSaveSchema;
        private System.Windows.Forms.Button btnTransferSchema;
        private System.Windows.Forms.Button btnTransferData;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem clearLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem speichernToolStripMenuItem;
        private System.Windows.Forms.RichTextBox rtfLog;
        private System.Windows.Forms.Button btnActivate;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelControls;
        private System.Windows.Forms.Panel panelCenter;
    }
}

