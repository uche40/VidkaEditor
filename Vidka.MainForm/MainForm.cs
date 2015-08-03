using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Core;

namespace Vidka.MainForm {
	public partial class MainForm : Form
	{
		private EditorLogic logic;

		public MainForm() {
			InitializeComponent();
			CustomLayout();

			logic = new EditorLogic(videoShitbox, vidkaPreviewPlayer);
			videoShitbox.setLogic(logic);
			videoShitbox.GuessWhoIsConsole(txtConsole);
		}

		private void CustomLayout()
		{
			var resolutionW = Screen.PrimaryScreen.Bounds.Width;
			var resolutionH = Screen.PrimaryScreen.Bounds.Height;
			var sidebarW = resolutionW * 500 / 3200;
			var playerH = resolutionH * 400 / 1800;

			var panelLeft = new Panel();
			this.Controls.Remove(this.txtConsole);
			this.Controls.Remove(this.vidkaPreviewPlayer);
			//this.Controls.Remove(this.videoShitbox);
			panelLeft.Controls.Add(this.txtConsole);
			panelLeft.Controls.Add(this.vidkaPreviewPlayer);
			this.txtConsole.Dock = DockStyle.Fill;
			this.vidkaPreviewPlayer.Dock = DockStyle.Top;
			this.vidkaPreviewPlayer.MinimumSize = new Size(sidebarW, playerH);
			this.vidkaPreviewPlayer.MaximumSize = new Size(sidebarW, playerH);
			this.videoShitbox.Dock = DockStyle.Fill;
			panelLeft.Dock = DockStyle.Right;
			panelLeft.MinimumSize = new Size(sidebarW, 200);
			this.Controls.Add(panelLeft);
			this.Width = (int)(resolutionW / 1.5f);
			this.Height = (int)(resolutionH / 1.5f);
		}

		private void Form1_DragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
		}

		private void Form1_Load(object sender, EventArgs e) {

		}

		private void txtConsole_TextChanged(object sender, EventArgs e)
		{
			txtConsole.SelectionStart = txtConsole.Text.Length; //Set the current caret position at the end
			txtConsole.ScrollToCaret(); //Now scroll it automatically
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}
	}
}
