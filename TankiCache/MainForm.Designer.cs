namespace ImageViewerWithBase64Names
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button selectFolderButton;
        private System.Windows.Forms.ListBox fileListBox;
        private System.Windows.Forms.PictureBox imagePreview;
        private System.Windows.Forms.Button openInExplorerButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.selectFolderButton = new System.Windows.Forms.Button();
            this.fileListBox = new System.Windows.Forms.ListBox();
            this.imagePreview = new System.Windows.Forms.PictureBox();
            this.openInExplorerButton = new System.Windows.Forms.Button();
            this.fileNameLabel = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.imagePreview)).BeginInit();
            this.SuspendLayout();
            // 
            // selectFolderButton
            // 
            this.selectFolderButton.Location = new System.Drawing.Point(12, 12);
            this.selectFolderButton.Name = "selectFolderButton";
            this.selectFolderButton.Size = new System.Drawing.Size(150, 30);
            this.selectFolderButton.TabIndex = 1;
            this.selectFolderButton.Text = "Выбрать директорию";
            this.selectFolderButton.Click += new System.EventHandler(this.SelectFolderButton_Click);
            // 
            // fileListBox
            // 
            this.fileListBox.Location = new System.Drawing.Point(12, 48);
            this.fileListBox.Name = "fileListBox";
            this.fileListBox.Size = new System.Drawing.Size(275, 485);
            this.fileListBox.TabIndex = 0;
            // 
            // imagePreview
            // 
            this.imagePreview.BackColor = System.Drawing.Color.White;
            this.imagePreview.Location = new System.Drawing.Point(293, 138);
            this.imagePreview.Name = "imagePreview";
            this.imagePreview.Size = new System.Drawing.Size(505, 395);
            this.imagePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imagePreview.TabIndex = 0;
            this.imagePreview.TabStop = false;
            // 
            // openInExplorerButton
            // 
            this.openInExplorerButton.Location = new System.Drawing.Point(168, 12);
            this.openInExplorerButton.Name = "openInExplorerButton";
            this.openInExplorerButton.Size = new System.Drawing.Size(150, 30);
            this.openInExplorerButton.TabIndex = 2;
            this.openInExplorerButton.Text = "Открыть в проводнике";
            this.openInExplorerButton.Click += new System.EventHandler(this.OpenInExplorerButton_Click);
            // 
            // fileNameLabel
            // 
            this.fileNameLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.fileNameLabel.Location = new System.Drawing.Point(293, 48);
            this.fileNameLabel.Name = "fileNameLabel";
            this.fileNameLabel.Size = new System.Drawing.Size(505, 87);
            this.fileNameLabel.TabIndex = 1;
            this.fileNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(324, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(150, 30);
            this.button1.TabIndex = 3;
            this.button1.Text = "Сохранить файл";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.CopyFileButton_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(807, 540);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.fileListBox);
            this.Controls.Add(this.openInExplorerButton);
            this.Controls.Add(this.imagePreview);
            this.Controls.Add(this.selectFolderButton);
            this.Controls.Add(this.fileNameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tanki Cache Reader";
            ((System.ComponentModel.ISupportInitialize)(this.imagePreview)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label fileNameLabel;
        private System.Windows.Forms.Button button1;
    }
}