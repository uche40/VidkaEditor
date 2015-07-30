namespace Vidka.Components
{
	partial class VidkaPreviewPlayer
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VidkaPreviewPlayer));
			this.labelBottom = new System.Windows.Forms.Label();
			this.MediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
			((System.ComponentModel.ISupportInitialize)(this.MediaPlayer)).BeginInit();
			this.SuspendLayout();
			// 
			// labelBottom
			// 
			this.labelBottom.AutoSize = true;
			this.labelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.labelBottom.Location = new System.Drawing.Point(0, 497);
			this.labelBottom.Name = "labelBottom";
			this.labelBottom.Size = new System.Drawing.Size(208, 32);
			this.labelBottom.TabIndex = 0;
			this.labelBottom.Text = "Preview screen";
			this.labelBottom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// MediaPlayer
			// 
			this.MediaPlayer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MediaPlayer.Enabled = true;
			this.MediaPlayer.Location = new System.Drawing.Point(0, 0);
			this.MediaPlayer.Name = "MediaPlayer";
			this.MediaPlayer.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("MediaPlayer.OcxState")));
			this.MediaPlayer.Size = new System.Drawing.Size(1220, 497);
			this.MediaPlayer.TabIndex = 1;
			// 
			// VidkaPreviewPlayer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MediaPlayer);
			this.Controls.Add(this.labelBottom);
			this.Name = "VidkaPreviewPlayer";
			this.Size = new System.Drawing.Size(1220, 529);
			((System.ComponentModel.ISupportInitialize)(this.MediaPlayer)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelBottom;
		private AxWMPLib.AxWindowsMediaPlayer MediaPlayer;
	}
}
