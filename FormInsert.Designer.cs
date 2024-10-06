namespace CosmosDBClient
{
    partial class FormInsert
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            richTextBoxInsertJson = new RichTextBox();
            buttonJsonInsert = new Button();
            SuspendLayout();
            // 
            // richTextBoxInsertJson
            // 
            richTextBoxInsertJson.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxInsertJson.Location = new Point(12, 12);
            richTextBoxInsertJson.Name = "richTextBoxInsertJson";
            richTextBoxInsertJson.Size = new Size(473, 375);
            richTextBoxInsertJson.TabIndex = 0;
            richTextBoxInsertJson.Text = "";
            // 
            // buttonJsonInsert
            // 
            buttonJsonInsert.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonJsonInsert.Location = new Point(410, 393);
            buttonJsonInsert.Name = "buttonJsonInsert";
            buttonJsonInsert.Size = new Size(75, 23);
            buttonJsonInsert.TabIndex = 1;
            buttonJsonInsert.Text = "Insert";
            buttonJsonInsert.UseVisualStyleBackColor = true;
            buttonJsonInsert.Click += buttonJsonInsert_Click;
            // 
            // FormInsert
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(497, 428);
            Controls.Add(buttonJsonInsert);
            Controls.Add(richTextBoxInsertJson);
            Name = "FormInsert";
            Text = "FormInsert";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBoxInsertJson;
        private Button buttonJsonInsert;
    }
}