namespace TerraTechModManager
{
    partial class Update
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
            System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
            System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelReleaseName = new System.Windows.Forms.Label();
            this.labelVersionNumber = new System.Windows.Forms.Label();
            this.buttonDownloadUpdate = new System.Windows.Forms.Button();
            this.buttonIgnore = new System.Windows.Forms.Button();
            this.checkBoxIgnore = new System.Windows.Forms.CheckBox();
            this.richTextBoxBody = new System.Windows.Forms.RichTextBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 2);
            tableLayoutPanel1.Controls.Add(this.richTextBoxBody, 0, 1);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            tableLayoutPanel1.Size = new System.Drawing.Size(481, 270);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelReleaseName);
            this.panel1.Controls.Add(this.labelVersionNumber);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(475, 34);
            this.panel1.TabIndex = 0;
            // 
            // labelReleaseName
            // 
            this.labelReleaseName.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelReleaseName.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.labelReleaseName.Location = new System.Drawing.Point(80, 0);
            this.labelReleaseName.Name = "labelReleaseName";
            this.labelReleaseName.Size = new System.Drawing.Size(395, 34);
            this.labelReleaseName.TabIndex = 2;
            this.labelReleaseName.Text = "Name";
            this.labelReleaseName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelVersionNumber
            // 
            this.labelVersionNumber.AutoSize = true;
            this.labelVersionNumber.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelVersionNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.labelVersionNumber.Location = new System.Drawing.Point(0, 0);
            this.labelVersionNumber.Name = "labelVersionNumber";
            this.labelVersionNumber.Size = new System.Drawing.Size(80, 24);
            this.labelVersionNumber.TabIndex = 1;
            this.labelVersionNumber.Text = "Version ";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 3;
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            tableLayoutPanel2.Controls.Add(this.buttonDownloadUpdate, 2, 0);
            tableLayoutPanel2.Controls.Add(this.buttonIgnore, 1, 0);
            tableLayoutPanel2.Controls.Add(this.checkBoxIgnore, 0, 0);
            tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(3, 238);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new System.Drawing.Size(475, 29);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // buttonDownloadUpdate
            // 
            this.buttonDownloadUpdate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonDownloadUpdate.Location = new System.Drawing.Point(253, 3);
            this.buttonDownloadUpdate.Name = "buttonDownloadUpdate";
            this.buttonDownloadUpdate.Size = new System.Drawing.Size(219, 23);
            this.buttonDownloadUpdate.TabIndex = 0;
            this.buttonDownloadUpdate.Text = "Download and Install";
            this.buttonDownloadUpdate.UseVisualStyleBackColor = true;
            this.buttonDownloadUpdate.Click += new System.EventHandler(this.buttonDownloadUpdate_Click);
            // 
            // buttonIgnore
            // 
            this.buttonIgnore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonIgnore.Location = new System.Drawing.Point(103, 3);
            this.buttonIgnore.Name = "buttonIgnore";
            this.buttonIgnore.Size = new System.Drawing.Size(144, 23);
            this.buttonIgnore.TabIndex = 1;
            this.buttonIgnore.Text = "Ignore";
            this.buttonIgnore.UseVisualStyleBackColor = true;
            this.buttonIgnore.Click += new System.EventHandler(this.buttonIgnore_Click);
            // 
            // checkBoxIgnore
            // 
            this.checkBoxIgnore.AutoSize = true;
            this.checkBoxIgnore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxIgnore.Location = new System.Drawing.Point(3, 3);
            this.checkBoxIgnore.Name = "checkBoxIgnore";
            this.checkBoxIgnore.Size = new System.Drawing.Size(94, 23);
            this.checkBoxIgnore.TabIndex = 2;
            this.checkBoxIgnore.Text = "Always Ignore";
            this.checkBoxIgnore.UseVisualStyleBackColor = true;
            this.checkBoxIgnore.CheckedChanged += new System.EventHandler(this.checkBoxIgnore_CheckedChanged);
            // 
            // richTextBoxBody
            // 
            this.richTextBoxBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxBody.Location = new System.Drawing.Point(3, 43);
            this.richTextBoxBody.Name = "richTextBoxBody";
            this.richTextBoxBody.Size = new System.Drawing.Size(475, 189);
            this.richTextBoxBody.TabIndex = 2;
            this.richTextBoxBody.Text = "";
            // 
            // Update
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 270);
            this.Controls.Add(tableLayoutPanel1);
            this.Name = "Update";
            this.Text = "An Update is available";
            tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelReleaseName;
        private System.Windows.Forms.Label labelVersionNumber;
        private System.Windows.Forms.Button buttonDownloadUpdate;
        private System.Windows.Forms.Button buttonIgnore;
        private System.Windows.Forms.CheckBox checkBoxIgnore;
        private System.Windows.Forms.RichTextBox richTextBoxBody;
    }
}