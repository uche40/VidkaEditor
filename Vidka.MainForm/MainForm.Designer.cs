namespace Vidka.MainForm
{
	partial class MainForm
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.txtConsole = new System.Windows.Forms.RichTextBox();
			this.vidkaFastPreviewPlayer = new Vidka.Components.VidkaFastPreviewPlayer();
			this.videoShitbox = new Vidka.Components.VideoShitbox();
			this.vidkaPreviewPlayer = new Vidka.Components.VidkaPreviewPlayer();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.ImageScalingSize = new System.Drawing.Size(40, 40);
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(2017, 49);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(75, 45);
			this.fileToolStripMenuItem.Text = "File";
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.Size = new System.Drawing.Size(156, 46);
			this.newToolStripMenuItem.Text = "New";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			// 
			// txtConsole
			// 
			this.txtConsole.BackColor = System.Drawing.Color.White;
			this.txtConsole.Location = new System.Drawing.Point(1373, 525);
			this.txtConsole.Name = "txtConsole";
			this.txtConsole.ReadOnly = true;
			this.txtConsole.Size = new System.Drawing.Size(613, 533);
			this.txtConsole.TabIndex = 3;
			this.txtConsole.Text = "";
			this.txtConsole.TextChanged += new System.EventHandler(this.txtConsole_TextChanged);
			// 
			// vidkaFastPreviewPlayer
			// 
			this.vidkaFastPreviewPlayer.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.vidkaFastPreviewPlayer.Location = new System.Drawing.Point(1195, 140);
			this.vidkaFastPreviewPlayer.Name = "vidkaFastPreviewPlayer";
			this.vidkaFastPreviewPlayer.Size = new System.Drawing.Size(407, 252);
			this.vidkaFastPreviewPlayer.TabIndex = 4;
			// 
			// videoShitbox
			// 
			this.videoShitbox.AllowDrop = true;
			this.videoShitbox.AutoScroll = true;
			this.videoShitbox.BackColor = System.Drawing.Color.White;
			this.videoShitbox.Location = new System.Drawing.Point(15, 71);
			this.videoShitbox.Margin = new System.Windows.Forms.Padding(6);
			this.videoShitbox.Name = "videoShitbox";
			this.videoShitbox.Size = new System.Drawing.Size(935, 678);
			this.videoShitbox.TabIndex = 2;
			// 
			// vidkaPreviewPlayer
			// 
			this.vidkaPreviewPlayer.Location = new System.Drawing.Point(1352, 102);
			this.vidkaPreviewPlayer.Name = "vidkaPreviewPlayer";
			this.vidkaPreviewPlayer.Size = new System.Drawing.Size(634, 417);
			this.vidkaPreviewPlayer.TabIndex = 1;
			// 
			// MainForm
			// 
			this.ClientSize = new System.Drawing.Size(2017, 1098);
			this.Controls.Add(this.vidkaFastPreviewPlayer);
			this.Controls.Add(this.txtConsole);
			this.Controls.Add(this.videoShitbox);
			this.Controls.Add(this.vidkaPreviewPlayer);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private Components.VidkaPreviewPlayer vidkaPreviewPlayer;
		private Components.VideoShitbox videoShitbox;
		private System.Windows.Forms.RichTextBox txtConsole;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private Components.VidkaFastPreviewPlayer vidkaFastPreviewPlayer;
	}
}

