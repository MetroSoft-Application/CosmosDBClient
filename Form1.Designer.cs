namespace CosmosDBClient
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
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).BeginInit();
            SuspendLayout();
            // 
            // buttonLoadData
            // 
            buttonLoadData.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonLoadData.Location = new Point(1199, 15);
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
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewResults.Size = new Size(1262, 613);
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
            splitContainer1.Panel1.Controls.Add(dataGridViewResults);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(richTextBoxSelectedCell);
            splitContainer1.Size = new Size(1262, 655);
            splitContainer1.SplitterDistance = 613;
            splitContainer1.TabIndex = 2;
            // 
            // richTextBoxSelectedCell
            // 
            richTextBoxSelectedCell.BackColor = SystemColors.ButtonFace;
            richTextBoxSelectedCell.Dock = DockStyle.Fill;
            richTextBoxSelectedCell.Location = new Point(0, 0);
            richTextBoxSelectedCell.Name = "richTextBoxSelectedCell";
            richTextBoxSelectedCell.ReadOnly = true;
            richTextBoxSelectedCell.Size = new Size(1262, 38);
            richTextBoxSelectedCell.TabIndex = 0;
            richTextBoxSelectedCell.Text = "";
            // 
            // richTextBoxQuery
            // 
            richTextBoxQuery.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxQuery.Font = new Font("Yu Gothic UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
            richTextBoxQuery.Location = new Point(12, 12);
            richTextBoxQuery.Name = "richTextBoxQuery";
            richTextBoxQuery.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxQuery.Size = new Size(803, 111);
            richTextBoxQuery.TabIndex = 4;
            richTextBoxQuery.Text = "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1\nORDER BY\n    c.id\n";
            // 
            // numericUpDownMaxCount
            // 
            numericUpDownMaxCount.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            numericUpDownMaxCount.Location = new Point(926, 100);
            numericUpDownMaxCount.Maximum = new decimal(new int[] { 1569325056, 23283064, 0, 0 });
            numericUpDownMaxCount.Name = "numericUpDownMaxCount";
            numericUpDownMaxCount.Size = new Size(75, 23);
            numericUpDownMaxCount.TabIndex = 5;
            numericUpDownMaxCount.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // textBoxConnectionString
            // 
            textBoxConnectionString.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxConnectionString.Location = new Point(926, 12);
            textBoxConnectionString.Name = "textBoxConnectionString";
            textBoxConnectionString.Size = new Size(267, 23);
            textBoxConnectionString.TabIndex = 6;
            textBoxConnectionString.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(821, 15);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 7;
            label1.Text = "ConnectionString";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(821, 45);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 8;
            label2.Text = "Database";
            // 
            // textBoxDatabaseName
            // 
            textBoxDatabaseName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxDatabaseName.Location = new Point(926, 42);
            textBoxDatabaseName.Name = "textBoxDatabaseName";
            textBoxDatabaseName.Size = new Size(267, 23);
            textBoxDatabaseName.TabIndex = 9;
            // 
            // textBoxContainerName
            // 
            textBoxContainerName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxContainerName.Location = new Point(926, 71);
            textBoxContainerName.Name = "textBoxContainerName";
            textBoxContainerName.Size = new Size(267, 23);
            textBoxContainerName.TabIndex = 11;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(821, 75);
            label3.Name = "label3";
            label3.Size = new Size(58, 15);
            label3.TabIndex = 10;
            label3.Text = "Container";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Location = new Point(821, 103);
            label4.Name = "label4";
            label4.Size = new Size(53, 15);
            label4.TabIndex = 12;
            label4.Text = "MaxRow";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1286, 796);
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
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericUpDownMaxCount).EndInit();
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
    }
}
