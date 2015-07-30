namespace Vidka.Components {
	partial class VideoShitbox {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.SuspendLayout();
			// 
			// VideoShitbox
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.Margin = new System.Windows.Forms.Padding(6);
			this.Name = "VideoShitbox";
			this.Size = new System.Drawing.Size(578, 355);
			this.Load += new System.EventHandler(this.VideoShitbox_Load);
			this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.VideoShitbox_Scroll);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.VideoShitbox_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.VideoShitbox_DragEnter);
			this.DragOver += new System.Windows.Forms.DragEventHandler(this.VideoShitbox_DragOver);
			this.DragLeave += new System.EventHandler(this.VideoShitbox_DragLeave);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.VideoShitbox_Paint);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VideoShitbox_KeyDown);
			this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.VideoShitbox_MouseClick);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.VideoShitbox_MouseDown);
			this.MouseLeave += new System.EventHandler(this.VideoShitbox_MouseLeave);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.VideoShitbox_MouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.VideoShitbox_MouseUp);
			this.Resize += new System.EventHandler(this.VideoShitbox_Resize);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
