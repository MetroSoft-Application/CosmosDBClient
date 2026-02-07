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
            buttonJsonInsert = new Button();
            panel1 = new Panel();
            buttonCancel = new Button();
            SuspendLayout();
            //
            // buttonJsonInsert
            //
            buttonJsonInsert.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonJsonInsert.Location = new Point(329, 393);
            buttonJsonInsert.Name = "buttonJsonInsert";
            buttonJsonInsert.Size = new Size(75, 23);
            buttonJsonInsert.TabIndex = 1;
            buttonJsonInsert.Text = "Insert";
            buttonJsonInsert.UseVisualStyleBackColor = true;
            buttonJsonInsert.Click += buttonJsonInsert_Click;
            //
            // panel1
            //
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(473, 375);
            panel1.TabIndex = 2;
            //
            // buttonCancel
            //
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(410, 393);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.TabIndex = 3;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            //
            // FormInsert
            //
            AcceptButton = buttonJsonInsert;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(497, 428);
            Controls.Add(buttonCancel);
            Controls.Add(panel1);
            Controls.Add(buttonJsonInsert);
            Name = "FormInsert";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Insert Record";
            ResumeLayout(false);
        }

        #endregion
        private Button buttonJsonInsert;
        private Panel panel1;
        private Button buttonCancel;
    }
}