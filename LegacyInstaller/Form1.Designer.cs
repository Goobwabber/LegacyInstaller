
namespace LegacyInstaller
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.versionDropdown = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bsPathTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bsPathBrowseButton = new System.Windows.Forms.Button();
            this.steamPathBrowseButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.steamPathTextBox = new System.Windows.Forms.TextBox();
            this.installButton = new System.Windows.Forms.Button();
            this.downloadInfoLabel = new System.Windows.Forms.Label();
            this.installStateLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // versionDropdown
            // 
            this.versionDropdown.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.versionDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.versionDropdown.FormattingEnabled = true;
            this.versionDropdown.Location = new System.Drawing.Point(63, 107);
            this.versionDropdown.Name = "versionDropdown";
            this.versionDropdown.Size = new System.Drawing.Size(121, 21);
            this.versionDropdown.TabIndex = 0;
            this.versionDropdown.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.versionDropdown_DrawItem);
            this.versionDropdown.SelectedIndexChanged += new System.EventHandler(this.versionDropdown_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 111);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Version:";
            // 
            // bsPathTextBox
            // 
            this.bsPathTextBox.Location = new System.Drawing.Point(12, 25);
            this.bsPathTextBox.Name = "bsPathTextBox";
            this.bsPathTextBox.Size = new System.Drawing.Size(274, 20);
            this.bsPathTextBox.TabIndex = 2;
            this.bsPathTextBox.TextChanged += new System.EventHandler(this.bsPathTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Beat Saber install folder:";
            // 
            // bsPathBrowseButton
            // 
            this.bsPathBrowseButton.Location = new System.Drawing.Point(292, 23);
            this.bsPathBrowseButton.Name = "bsPathBrowseButton";
            this.bsPathBrowseButton.Size = new System.Drawing.Size(93, 23);
            this.bsPathBrowseButton.TabIndex = 4;
            this.bsPathBrowseButton.Text = "Browse...";
            this.bsPathBrowseButton.UseVisualStyleBackColor = true;
            this.bsPathBrowseButton.Click += new System.EventHandler(this.bsPathBrowseButton_Click);
            // 
            // steamPathBrowseButton
            // 
            this.steamPathBrowseButton.Location = new System.Drawing.Point(292, 69);
            this.steamPathBrowseButton.Name = "steamPathBrowseButton";
            this.steamPathBrowseButton.Size = new System.Drawing.Size(93, 23);
            this.steamPathBrowseButton.TabIndex = 7;
            this.steamPathBrowseButton.Text = "Browse...";
            this.steamPathBrowseButton.UseVisualStyleBackColor = true;
            this.steamPathBrowseButton.Click += new System.EventHandler(this.steamPathBrowseButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Steam install folder:";
            // 
            // steamPathTextBox
            // 
            this.steamPathTextBox.Location = new System.Drawing.Point(12, 71);
            this.steamPathTextBox.Name = "steamPathTextBox";
            this.steamPathTextBox.Size = new System.Drawing.Size(274, 20);
            this.steamPathTextBox.TabIndex = 5;
            this.steamPathTextBox.TextChanged += new System.EventHandler(this.steamPathTextBox_TextChanged);
            // 
            // installButton
            // 
            this.installButton.Location = new System.Drawing.Point(190, 106);
            this.installButton.Name = "installButton";
            this.installButton.Size = new System.Drawing.Size(96, 23);
            this.installButton.TabIndex = 8;
            this.installButton.Text = "Install";
            this.installButton.UseVisualStyleBackColor = true;
            this.installButton.Click += new System.EventHandler(this.installButton_Click);
            // 
            // downloadInfoLabel
            // 
            this.downloadInfoLabel.AutoSize = true;
            this.downloadInfoLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.downloadInfoLabel.Location = new System.Drawing.Point(12, 148);
            this.downloadInfoLabel.Name = "downloadInfoLabel";
            this.downloadInfoLabel.Size = new System.Drawing.Size(0, 13);
            this.downloadInfoLabel.TabIndex = 9;
            // 
            // installStateLabel
            // 
            this.installStateLabel.AutoSize = true;
            this.installStateLabel.Location = new System.Drawing.Point(292, 111);
            this.installStateLabel.Name = "installStateLabel";
            this.installStateLabel.Size = new System.Drawing.Size(0, 13);
            this.installStateLabel.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(395, 207);
            this.Controls.Add(this.installStateLabel);
            this.Controls.Add(this.downloadInfoLabel);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.steamPathBrowseButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.steamPathTextBox);
            this.Controls.Add(this.bsPathBrowseButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.bsPathTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.versionDropdown);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "LegacyInstaller";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox versionDropdown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox bsPathTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bsPathBrowseButton;
        private System.Windows.Forms.Button steamPathBrowseButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox steamPathTextBox;
        private System.Windows.Forms.Button installButton;
        private System.Windows.Forms.Label downloadInfoLabel;
        private System.Windows.Forms.Label installStateLabel;
    }
}

