﻿namespace CosmosDBClient
{
    partial class Form1
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
            dataGridViewResults = new DataGridView();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            JsonData = new RichTextBox();
            richTextBoxSelectedCell = new RichTextBox();
            richTextBoxQuery = new RichTextBox();
            numericUpDownMaxCount = new NumericUpDown();
            textBoxConnectionString = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBoxDatabaseName = new TextBox();
            textBoxContainerName = new TextBox();
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
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // buttonLoadData
            // 
            buttonLoadData.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonLoadData.Location = new Point(1527, 15);
            buttonLoadData.Name = "buttonLoadData";
            buttonLoadData.Size = new Size(75, 23);
            buttonLoadData.TabIndex = 0;
            buttonLoadData.Text = "Exec";
            buttonLoadData.UseVisualStyleBackColor = true;
            buttonLoadData.Click += buttonLoadData_Click;
            // 
            // dataGridViewResults
            // 
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
            dataGridViewResults.ReadOnly = true;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewResults.Size = new Size(1267, 760);
            dataGridViewResults.TabIndex = 1;
            dataGridViewResults.CellClick += dataGridViewResults_CellClick;
            dataGridViewResults.RowPostPaint += dataGridViewResults_RowPostPaint;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 129);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(richTextBoxSelectedCell);
            splitContainer1.Size = new Size(1590, 814);
            splitContainer1.SplitterDistance = 760;
            splitContainer1.TabIndex = 2;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(dataGridViewResults);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(JsonData);
            splitContainer2.Size = new Size(1590, 760);
            splitContainer2.SplitterDistance = 1267;
            splitContainer2.TabIndex = 3;
            // 
            // JsonData
            // 
            JsonData.BackColor = SystemColors.ButtonFace;
            JsonData.Dock = DockStyle.Fill;
            JsonData.Location = new Point(0, 0);
            JsonData.Name = "JsonData";
            JsonData.ReadOnly = true;
            JsonData.Size = new Size(319, 760);
            JsonData.TabIndex = 2;
            JsonData.Text = "";
            // 
            // richTextBoxSelectedCell
            // 
            richTextBoxSelectedCell.BackColor = SystemColors.ButtonFace;
            richTextBoxSelectedCell.Dock = DockStyle.Fill;
            richTextBoxSelectedCell.Location = new Point(0, 0);
            richTextBoxSelectedCell.Name = "richTextBoxSelectedCell";
            richTextBoxSelectedCell.ReadOnly = true;
            richTextBoxSelectedCell.Size = new Size(1590, 50);
            richTextBoxSelectedCell.TabIndex = 0;
            richTextBoxSelectedCell.Text = "";
            // 
            // richTextBoxQuery
            // 
            richTextBoxQuery.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxQuery.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBoxQuery.Location = new Point(12, 12);
            richTextBoxQuery.Name = "richTextBoxQuery";
            richTextBoxQuery.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxQuery.Size = new Size(1131, 111);
            richTextBoxQuery.TabIndex = 4;
            richTextBoxQuery.Text = "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1\nORDER BY\n    c.id\n";
            // 
            // numericUpDownMaxCount
            // 
            numericUpDownMaxCount.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            numericUpDownMaxCount.Location = new Point(1254, 100);
            numericUpDownMaxCount.Maximum = new decimal(new int[] { 1569325056, 23283064, 0, 0 });
            numericUpDownMaxCount.Name = "numericUpDownMaxCount";
            numericUpDownMaxCount.Size = new Size(75, 23);
            numericUpDownMaxCount.TabIndex = 5;
            numericUpDownMaxCount.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // textBoxConnectionString
            // 
            textBoxConnectionString.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxConnectionString.Location = new Point(1254, 12);
            textBoxConnectionString.Name = "textBoxConnectionString";
            textBoxConnectionString.Size = new Size(267, 23);
            textBoxConnectionString.TabIndex = 6;
            textBoxConnectionString.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(1149, 15);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 7;
            label1.Text = "ConnectionString";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(1149, 45);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 8;
            label2.Text = "Database";
            // 
            // textBoxDatabaseName
            // 
            textBoxDatabaseName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxDatabaseName.Location = new Point(1254, 42);
            textBoxDatabaseName.Name = "textBoxDatabaseName";
            textBoxDatabaseName.Size = new Size(267, 23);
            textBoxDatabaseName.TabIndex = 9;
            // 
            // textBoxContainerName
            // 
            textBoxContainerName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxContainerName.Location = new Point(1254, 71);
            textBoxContainerName.Name = "textBoxContainerName";
            textBoxContainerName.Size = new Size(267, 23);
            textBoxContainerName.TabIndex = 11;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(1149, 75);
            label3.Name = "label3";
            label3.Size = new Size(58, 15);
            label3.TabIndex = 10;
            label3.Text = "Container";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Location = new Point(1149, 103);
            label4.Name = "label4";
            label4.Size = new Size(53, 15);
            label4.TabIndex = 12;
            label4.Text = "MaxRow";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3, toolStripStatusLabel4, toolStripStatusLabel5, toolStripStatusLabel6, toolStripStatusLabel7, toolStripStatusLabel8, toolStripStatusLabel9, toolStripStatusLabel10, toolStripStatusLabel11, toolStripStatusLabel12 });
            statusStrip1.Location = new Point(0, 933);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1614, 22);
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
            toolStripStatusLabel5.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel6
            // 
            toolStripStatusLabel6.Name = "toolStripStatusLabel6";
            toolStripStatusLabel6.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel7
            // 
            toolStripStatusLabel7.Name = "toolStripStatusLabel7";
            toolStripStatusLabel7.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel8
            // 
            toolStripStatusLabel8.Name = "toolStripStatusLabel8";
            toolStripStatusLabel8.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel9
            // 
            toolStripStatusLabel9.Name = "toolStripStatusLabel9";
            toolStripStatusLabel9.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel10
            // 
            toolStripStatusLabel10.Name = "toolStripStatusLabel10";
            toolStripStatusLabel10.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel11
            // 
            toolStripStatusLabel11.Name = "toolStripStatusLabel11";
            toolStripStatusLabel11.Size = new Size(0, 17);
            // 
            // toolStripStatusLabel12
            // 
            toolStripStatusLabel12.Name = "toolStripStatusLabel12";
            toolStripStatusLabel12.Size = new Size(0, 17);
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1614, 955);
            Controls.Add(statusStrip1);
            Controls.Add(label4);
            Controls.Add(textBoxContainerName);
            Controls.Add(label3);
            Controls.Add(textBoxDatabaseName);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBoxConnectionString);
            Controls.Add(numericUpDownMaxCount);
            Controls.Add(richTextBoxQuery);
            Controls.Add(splitContainer1);
            Controls.Add(buttonLoadData);
            Name = "Form1";
            Text = "CosmosDB Client Tool";
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonLoadData;
        private DataGridView dataGridViewResults;
        private SplitContainer splitContainer1;
        private RichTextBox richTextBoxSelectedCell;
        private RichTextBox richTextBoxQuery;
        private NumericUpDown numericUpDownMaxCount;
        private TextBox textBoxConnectionString;
        private Label label1;
        private Label label2;
        private TextBox textBoxDatabaseName;
        private TextBox textBoxContainerName;
        private Label label3;
        private Label label4;
        private SplitContainer splitContainer2;
        private RichTextBox JsonData;
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
    }
}
