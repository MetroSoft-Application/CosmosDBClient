namespace CosmosDBClient
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            buttonLoadData = new Button();
            numericUpDownMaxCount = new NumericUpDown();
            textBoxConnectionString = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBoxDatabaseName = new TextBox();
            label3 = new Label();
            label4 = new Label();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStripStatusLabel3 = new ToolStripStatusLabel();
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            toolStripStatusLabel5 = new ToolStripStatusLabel();
            toolStripStatusLabel6 = new ToolStripStatusLabel();
            toolStripStatusLabel7 = new ToolStripStatusLabel();
            toolStripStatusLabel8 = new ToolStripStatusLabel();
            toolStripStatusLabel9 = new ToolStripStatusLabel();
            toolStripStatusLabel10 = new ToolStripStatusLabel();
            toolStripStatusLabel11 = new ToolStripStatusLabel();
            toolStripStatusLabel12 = new ToolStripStatusLabel();
            toolStripStatusLabel13 = new ToolStripStatusLabel();
            cmbBoxContainerName = new ComboBox();
            buttonInsert = new Button();
            groupBox1 = new GroupBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            txtUniqueKey = new TextBox();
            label8 = new Label();
            txtPartitionKey = new TextBox();
            label7 = new Label();
            label6 = new Label();
            nupTimeToLiveSeconds = new NumericUpDown();
            radioTimeToLiveOn = new RadioButton();
            radioTimeToLiveOff = new RadioButton();
            label5 = new Label();
            tabPage2 = new TabPage();
            txtIndexingPolicy = new RichTextBox();
            groupBox2 = new GroupBox();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            dataGridViewResults = new DataGridView();
            richTextBoxSelectedCell = new RichTextBox();
            splitContainer3 = new SplitContainer();
            splitContainer4 = new SplitContainer();
            buttonUpdate = new Button();
            buttonDelete = new Button();
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).BeginInit();
            statusStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nupTimeToLiveSeconds).BeginInit();
            tabPage2.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer4).BeginInit();
            splitContainer4.Panel1.SuspendLayout();
            splitContainer4.Panel2.SuspendLayout();
            splitContainer4.SuspendLayout();
            SuspendLayout();
            // 
            // buttonLoadData
            // 
            buttonLoadData.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonLoadData.Location = new Point(273, 18);
            buttonLoadData.Name = "buttonLoadData";
            buttonLoadData.Size = new Size(61, 23);
            buttonLoadData.TabIndex = 0;
            buttonLoadData.Text = "Exec";
            buttonLoadData.UseVisualStyleBackColor = true;
            buttonLoadData.Click += buttonLoadData_Click;
            // 
            // numericUpDownMaxCount
            // 
            numericUpDownMaxCount.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            numericUpDownMaxCount.Location = new Point(119, 108);
            numericUpDownMaxCount.Maximum = new decimal(new int[] { 1569325056, 23283064, 0, 0 });
            numericUpDownMaxCount.Name = "numericUpDownMaxCount";
            numericUpDownMaxCount.Size = new Size(148, 23);
            numericUpDownMaxCount.TabIndex = 5;
            numericUpDownMaxCount.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // textBoxConnectionString
            // 
            textBoxConnectionString.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxConnectionString.Location = new Point(119, 18);
            textBoxConnectionString.Name = "textBoxConnectionString";
            textBoxConnectionString.Size = new Size(148, 23);
            textBoxConnectionString.TabIndex = 6;
            textBoxConnectionString.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(13, 21);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 7;
            label1.Text = "ConnectionString";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(13, 51);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 8;
            label2.Text = "Database";
            // 
            // textBoxDatabaseName
            // 
            textBoxDatabaseName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxDatabaseName.Location = new Point(119, 48);
            textBoxDatabaseName.Name = "textBoxDatabaseName";
            textBoxDatabaseName.Size = new Size(148, 23);
            textBoxDatabaseName.TabIndex = 9;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(13, 81);
            label3.Name = "label3";
            label3.Size = new Size(58, 15);
            label3.TabIndex = 10;
            label3.Text = "Container";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Location = new Point(13, 110);
            label4.Name = "label4";
            label4.Size = new Size(53, 15);
            label4.TabIndex = 12;
            label4.Text = "MaxRow";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3, toolStripStatusLabel4 });
            statusStrip1.Location = new Point(0, 659);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1394, 22);
            statusStrip1.TabIndex = 13;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel5
            // 
            toolStripStatusLabel5.Name = "toolStripStatusLabel5";
            toolStripStatusLabel5.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel6
            // 
            toolStripStatusLabel6.Name = "toolStripStatusLabel6";
            toolStripStatusLabel6.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel7
            // 
            toolStripStatusLabel7.Name = "toolStripStatusLabel7";
            toolStripStatusLabel7.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel8
            // 
            toolStripStatusLabel8.Name = "toolStripStatusLabel8";
            toolStripStatusLabel8.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel9
            // 
            toolStripStatusLabel9.Name = "toolStripStatusLabel9";
            toolStripStatusLabel9.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel10
            // 
            toolStripStatusLabel10.Name = "toolStripStatusLabel10";
            toolStripStatusLabel10.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel11
            // 
            toolStripStatusLabel11.Name = "toolStripStatusLabel11";
            toolStripStatusLabel11.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel12
            // 
            toolStripStatusLabel12.Name = "toolStripStatusLabel12";
            toolStripStatusLabel12.Size = new Size(23, 23);
            // 
            // toolStripStatusLabel13
            // 
            toolStripStatusLabel13.Name = "toolStripStatusLabel13";
            toolStripStatusLabel13.Size = new Size(23, 23);
            // 
            // cmbBoxContainerName
            // 
            cmbBoxContainerName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            cmbBoxContainerName.FormattingEnabled = true;
            cmbBoxContainerName.Location = new Point(119, 78);
            cmbBoxContainerName.Name = "cmbBoxContainerName";
            cmbBoxContainerName.Size = new Size(148, 23);
            cmbBoxContainerName.TabIndex = 14;
            // 
            // buttonInsert
            // 
            buttonInsert.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonInsert.Enabled = false;
            buttonInsert.Location = new Point(273, 47);
            buttonInsert.Name = "buttonInsert";
            buttonInsert.Size = new Size(61, 23);
            buttonInsert.TabIndex = 15;
            buttonInsert.Text = "Insert";
            buttonInsert.UseVisualStyleBackColor = true;
            buttonInsert.Click += buttonInsert_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(buttonInsert);
            groupBox1.Controls.Add(buttonLoadData);
            groupBox1.Controls.Add(cmbBoxContainerName);
            groupBox1.Controls.Add(numericUpDownMaxCount);
            groupBox1.Controls.Add(textBoxConnectionString);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(textBoxDatabaseName);
            groupBox1.Location = new Point(1047, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(339, 137);
            groupBox1.TabIndex = 16;
            groupBox1.TabStop = false;
            groupBox1.Text = "Search Condition";
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(3, 19);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(386, 115);
            tabControl1.TabIndex = 17;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(txtUniqueKey);
            tabPage1.Controls.Add(label8);
            tabPage1.Controls.Add(txtPartitionKey);
            tabPage1.Controls.Add(label7);
            tabPage1.Controls.Add(label6);
            tabPage1.Controls.Add(nupTimeToLiveSeconds);
            tabPage1.Controls.Add(radioTimeToLiveOn);
            tabPage1.Controls.Add(radioTimeToLiveOff);
            tabPage1.Controls.Add(label5);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(378, 87);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Settings";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtUniqueKey
            // 
            txtUniqueKey.Location = new Point(100, 58);
            txtUniqueKey.Name = "txtUniqueKey";
            txtUniqueKey.ReadOnly = true;
            txtUniqueKey.Size = new Size(270, 23);
            txtUniqueKey.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(7, 63);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 12;
            label8.Text = "Unique Key";
            // 
            // txtPartitionKey
            // 
            txtPartitionKey.Location = new Point(100, 33);
            txtPartitionKey.Name = "txtPartitionKey";
            txtPartitionKey.ReadOnly = true;
            txtPartitionKey.Size = new Size(270, 23);
            txtPartitionKey.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(7, 37);
            label7.Name = "label7";
            label7.Size = new Size(74, 15);
            label7.TabIndex = 10;
            label7.Text = "Partition Key";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(319, 11);
            label6.Name = "label6";
            label6.Size = new Size(51, 15);
            label6.TabIndex = 5;
            label6.Text = "Seconds";
            label6.Visible = false;
            // 
            // nupTimeToLiveSeconds
            // 
            nupTimeToLiveSeconds.Enabled = false;
            nupTimeToLiveSeconds.Location = new Point(193, 6);
            nupTimeToLiveSeconds.Maximum = new decimal(new int[] { -559939585, 902409669, 54, 0 });
            nupTimeToLiveSeconds.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            nupTimeToLiveSeconds.Name = "nupTimeToLiveSeconds";
            nupTimeToLiveSeconds.ReadOnly = true;
            nupTimeToLiveSeconds.Size = new Size(120, 23);
            nupTimeToLiveSeconds.TabIndex = 4;
            nupTimeToLiveSeconds.Visible = false;
            // 
            // radioTimeToLiveOn
            // 
            radioTimeToLiveOn.AutoSize = true;
            radioTimeToLiveOn.Enabled = false;
            radioTimeToLiveOn.Location = new Point(146, 9);
            radioTimeToLiveOn.Name = "radioTimeToLiveOn";
            radioTimeToLiveOn.Size = new Size(41, 19);
            radioTimeToLiveOn.TabIndex = 3;
            radioTimeToLiveOn.TabStop = true;
            radioTimeToLiveOn.Text = "On";
            radioTimeToLiveOn.UseVisualStyleBackColor = true;
            // 
            // radioTimeToLiveOff
            // 
            radioTimeToLiveOff.AutoSize = true;
            radioTimeToLiveOff.Enabled = false;
            radioTimeToLiveOff.Location = new Point(103, 9);
            radioTimeToLiveOff.Name = "radioTimeToLiveOff";
            radioTimeToLiveOff.Size = new Size(41, 19);
            radioTimeToLiveOff.TabIndex = 1;
            radioTimeToLiveOff.TabStop = true;
            radioTimeToLiveOff.Text = "Off";
            radioTimeToLiveOff.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(7, 11);
            label5.Name = "label5";
            label5.Size = new Size(70, 15);
            label5.TabIndex = 0;
            label5.Text = "Time to Live";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(txtIndexingPolicy);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(378, 87);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Indexing Policy";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtIndexingPolicy
            // 
            txtIndexingPolicy.Dock = DockStyle.Fill;
            txtIndexingPolicy.Location = new Point(3, 3);
            txtIndexingPolicy.Name = "txtIndexingPolicy";
            txtIndexingPolicy.ReadOnly = true;
            txtIndexingPolicy.Size = new Size(372, 81);
            txtIndexingPolicy.TabIndex = 0;
            txtIndexingPolicy.Text = "";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox2.Controls.Add(tabControl1);
            groupBox2.Location = new Point(649, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(392, 137);
            groupBox2.TabIndex = 18;
            groupBox2.TabStop = false;
            groupBox2.Text = "Container Settings";
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(631, 128);
            panel1.TabIndex = 19;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 146);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer3);
            splitContainer1.Size = new Size(1374, 510);
            splitContainer1.SplitterDistance = 1034;
            splitContainer1.TabIndex = 20;
            // 
            // splitContainer2
            // 
            splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(dataGridViewResults);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(richTextBoxSelectedCell);
            splitContainer2.Size = new Size(1034, 510);
            splitContainer2.SplitterDistance = 478;
            splitContainer2.TabIndex = 0;
            // 
            // dataGridViewResults
            // 
            dataGridViewResults.AllowUserToAddRows = false;
            dataGridViewResults.AllowUserToDeleteRows = false;
            dataGridViewResults.AllowUserToOrderColumns = true;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.ActiveCaption;
            dataGridViewCellStyle1.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Dock = DockStyle.Fill;
            dataGridViewResults.Location = new Point(0, 0);
            dataGridViewResults.Name = "dataGridViewResults";
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewResults.RowHeadersWidth = 51;
            dataGridViewResults.Size = new Size(1034, 478);
            dataGridViewResults.TabIndex = 19;
            dataGridViewResults.CellClick += dataGridViewResults_CellClick;
            dataGridViewResults.CellFormatting += dataGridViewResults_CellFormatting;
            dataGridViewResults.RowPostPaint += dataGridViewResults_RowPostPaint;
            dataGridViewResults.KeyUp += dataGridViewResults_KeyUp;
            // 
            // richTextBoxSelectedCell
            // 
            richTextBoxSelectedCell.BackColor = SystemColors.ButtonFace;
            richTextBoxSelectedCell.Dock = DockStyle.Fill;
            richTextBoxSelectedCell.Location = new Point(0, 0);
            richTextBoxSelectedCell.Name = "richTextBoxSelectedCell";
            richTextBoxSelectedCell.ReadOnly = true;
            richTextBoxSelectedCell.Size = new Size(1034, 28);
            richTextBoxSelectedCell.TabIndex = 18;
            richTextBoxSelectedCell.Text = "";
            richTextBoxSelectedCell.MouseUp += richTextBoxSelectedCell_MouseUp;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.FixedPanel = FixedPanel.Panel2;
            splitContainer3.IsSplitterFixed = true;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(splitContainer4);
            splitContainer3.Size = new Size(336, 510);
            splitContainer3.SplitterDistance = 477;
            splitContainer3.TabIndex = 0;
            // 
            // splitContainer4
            // 
            splitContainer4.Dock = DockStyle.Fill;
            splitContainer4.IsSplitterFixed = true;
            splitContainer4.Location = new Point(0, 0);
            splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            splitContainer4.Panel1.Controls.Add(buttonUpdate);
            // 
            // splitContainer4.Panel2
            // 
            splitContainer4.Panel2.Controls.Add(buttonDelete);
            splitContainer4.Size = new Size(336, 29);
            splitContainer4.SplitterDistance = 166;
            splitContainer4.TabIndex = 0;
            // 
            // buttonUpdate
            // 
            buttonUpdate.Dock = DockStyle.Fill;
            buttonUpdate.Enabled = false;
            buttonUpdate.Location = new Point(0, 0);
            buttonUpdate.Name = "buttonUpdate";
            buttonUpdate.Size = new Size(166, 29);
            buttonUpdate.TabIndex = 21;
            buttonUpdate.Text = "Update";
            buttonUpdate.UseVisualStyleBackColor = true;
            buttonUpdate.Click += buttonUpdate_Click;
            // 
            // buttonDelete
            // 
            buttonDelete.Dock = DockStyle.Fill;
            buttonDelete.Enabled = false;
            buttonDelete.Location = new Point(0, 0);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(166, 29);
            buttonDelete.TabIndex = 22;
            buttonDelete.Text = "Delete";
            buttonDelete.UseVisualStyleBackColor = true;
            buttonDelete.Click += buttonDelete_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1394, 681);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(statusStrip1);
            Name = "FormMain";
            Text = "CosmosDB Client Tool";
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nupTimeToLiveSeconds).EndInit();
            tabPage2.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            splitContainer4.Panel1.ResumeLayout(false);
            splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer4).EndInit();
            splitContainer4.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonLoadData;
        private NumericUpDown numericUpDownMaxCount;
        private TextBox textBoxConnectionString;
        private Label label1;
        private Label label2;
        private TextBox textBoxDatabaseName;
        private Label label3;
        private Label label4;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private ToolStripStatusLabel toolStripStatusLabel5;
        private ToolStripStatusLabel toolStripStatusLabel6;
        private ToolStripStatusLabel toolStripStatusLabel7;
        private ToolStripStatusLabel toolStripStatusLabel8;
        private ToolStripStatusLabel toolStripStatusLabel9;
        private ToolStripStatusLabel toolStripStatusLabel10;
        private ToolStripStatusLabel toolStripStatusLabel11;
        private ToolStripStatusLabel toolStripStatusLabel12;
        private ToolStripStatusLabel toolStripStatusLabel13;
        private ComboBox cmbBoxContainerName;
        private Button buttonInsert;
        private GroupBox groupBox1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private GroupBox groupBox2;
        private RadioButton radioTimeToLiveOff;
        private Label label5;
        private Label label6;
        private NumericUpDown nupTimeToLiveSeconds;
        private RadioButton radioTimeToLiveOn;
        private TextBox txtUniqueKey;
        private Label label8;
        private TextBox txtPartitionKey;
        private Label label7;
        private RichTextBox txtIndexingPolicy;
        private Panel panel1;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private DataGridView dataGridViewResults;
        private RichTextBox richTextBoxSelectedCell;
        private SplitContainer splitContainer3;
        private SplitContainer splitContainer4;
        private Button buttonUpdate;
        private Button buttonDelete;
    }
}
