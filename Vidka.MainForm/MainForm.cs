﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Components;
using Vidka.Core;

namespace Vidka.MainForm {
	public partial class MainForm : Form, IVidkaMainForm
	{
		private const float RATIO_sidebarW_of_screenW = 500f / 3200;
		private const float RATIO_playerH_of_screenH = 370f / 1800;
		private const float RATIO_absPlayerW_of_windowW = 0.25f;
		private const float RATIO_absPlayerH_of_windowH = 0.3f;
		private const float RATIO_windowW_of_screenW = 1 / 1.2f;
		private const float RATIO_windowH_of_screenH = 1 / 1.5f;

		private EditorLogic logic;
		private Panel panelLeft;
		private Panel panelPlayerHolder;
		private VidkaFastPreviewPlayerWrapper fastPlayerWrapper;

		// UI layout state
		private VidkaPreviewMode previewMode;
		private bool showConsole = true;
		private PreviewPlayerAbsoluteLocation playerAbsLocation;

		public MainForm() {
			InitializeComponent();
			CustomLayout();

			logic = new EditorLogic(videoShitbox, vidkaPreviewPlayer);
			fastPlayerWrapper = new VidkaFastPreviewPlayerWrapper(vidkaFastPreviewPlayer, vidkaPreviewPlayer, this);
			videoShitbox.setLogic(logic);
			videoShitbox.GuessWhoIsConsole(txtConsole);
			vidkaFastPreviewPlayer.SetFileMapping(logic.FileMapping);
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			videoShitbox.PleaseTogglePreviewMode += videoShitbox_PleaseTogglePreviewMode;
			videoShitbox.PleaseToggleConsoleVisibility += videoShitbox_PleaseToggleConsoleVisibility;
			videoShitbox.PleaseSetPlayerAbsPosition += videoShitbox_PleaseSetPlayerAbsPosition;
			
			//TODO: maybe load these from configuration?
			//setPreviewPlayer(VidkaPreviewMode.Normal);
			//setConsoleVisible(true);
			//setPlayerAbsoluteLocation(PreviewPlayerAbsoluteLocation.TopRight);
			setPreviewPlayer(VidkaPreviewMode.Fast);
			setConsoleVisible(false);
			setPlayerAbsoluteLocation(PreviewPlayerAbsoluteLocation.TopRight);
		}

		#region ------------------- callbacks ---------------------

		private void videoShitbox_PleaseToggleConsoleVisibility()
		{
			setConsoleVisible(!showConsole);
		}
		private void videoShitbox_PleaseTogglePreviewMode()
		{
			setPreviewPlayer((previewMode == VidkaPreviewMode.Fast)
					? VidkaPreviewMode.Normal
					: VidkaPreviewMode.Fast);
		}
		private void videoShitbox_PleaseSetPlayerAbsPosition(PreviewPlayerAbsoluteLocation location)
		{
			setPlayerAbsoluteLocation(location);
		}

		private void txtConsole_TextChanged(object sender, EventArgs e)
		{
			txtConsole.SelectionStart = txtConsole.Text.Length; //Set the current caret position at the end
			txtConsole.ScrollToCaret(); //Now scroll it automatically
		}

		private void MainForm_ResizeEnd(object sender, EventArgs e)
		{
			updatePlayerAbsoluteLocation();
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		#endregion

		#region ------------------- helpers ---------------------

		private void CustomLayout()
		{
			var resolutionW = Screen.PrimaryScreen.Bounds.Width;
			var resolutionH = Screen.PrimaryScreen.Bounds.Height;
			var sidebarW = (int)(resolutionW * RATIO_sidebarW_of_screenW);
			var playerH = (int)(resolutionH * RATIO_playerH_of_screenH);

			vidkaFastPreviewPlayer.Visible = false;
			vidkaFastPreviewPlayer.Dock = DockStyle.Fill;
			vidkaPreviewPlayer.Visible = false;
			vidkaPreviewPlayer.Dock = DockStyle.Fill;

			panelLeft = new Panel()
			{
				Dock = DockStyle.Right,
				MinimumSize = new Size(sidebarW, 200),
			};
			panelPlayerHolder = new Panel()
			{
				Dock = DockStyle.Top,
				MinimumSize = new Size(sidebarW, playerH),
			};
			this.Controls.Remove(this.txtConsole);
			this.Controls.Remove(this.vidkaPreviewPlayer);
			//this.Controls.Remove(this.videoShitbox);
			panelLeft.Controls.Add(this.txtConsole);
			panelLeft.Controls.Add(panelPlayerHolder);
			this.txtConsole.Dock = DockStyle.Fill;
			this.videoShitbox.Dock = DockStyle.Fill;
			this.Controls.Add(panelLeft);
			this.Width = (int)(resolutionW * RATIO_windowW_of_screenW);
			this.Height = (int)(resolutionH * RATIO_windowH_of_screenH);
		}

		/// <summary>
		/// Set the correct preview player in UI and logic.
		/// Called from Form.OnLoad and TogglePreviewMode.
		/// </summary>
		private void setPreviewPlayer(VidkaPreviewMode mode)
		{
			previewMode = mode;
			videoShitbox.iiii("Preview mode: " + previewMode);
			SwapPreviewPlayerUI(previewMode);
			logic.SetPreviewPlayer((previewMode == VidkaPreviewMode.Normal)
				? (IVideoPlayer)vidkaPreviewPlayer
				: (IVideoPlayer)fastPlayerWrapper);
		}

		/// <summary>
		/// UI: Swaps the correct player into panelPlayerHolder panel
		/// </summary>
		private void configureUi_PreviewMode(
			UserControl player2Activate,
			UserControl player2Deactivate)
		{
			panelPlayerHolder.Controls.Remove(player2Deactivate);
			panelPlayerHolder.Controls.Add(player2Activate);
			player2Activate.Visible = true;
			player2Deactivate.Visible = false;
		}

		/// <summary>
		/// Set the visibility of the entire left panel
		/// Called from Form.OnLoad and ToggleConsoleVisibility.
		/// </summary>
		private void setConsoleVisible(bool visible)
		{
			showConsole = visible;
			if (showConsole)
			{
				this.Controls.Remove(panelPlayerHolder);
				panelPlayerHolder.Dock = DockStyle.Top;
				panelPlayerHolder.Size = new Size(10, 10); // don't worry - the MinimumSize we set at the top will prevent this from being really 10x10
				panelLeft.Controls.Add(panelPlayerHolder);
			}
			else
			{
				panelPlayerHolder.Dock = DockStyle.None;
				panelLeft.Controls.Remove(panelPlayerHolder);
				updatePlayerAbsoluteLocation();
				this.Controls.Add(panelPlayerHolder);
				panelPlayerHolder.BringToFront();
			}
			panelLeft.Visible = showConsole;
		}

		/// <summary>
		/// Called by EditorLogic when mouse gets in the way
		/// </summary>
		/// <param name="playerAbsLocation"></param>
		private void setPlayerAbsoluteLocation(PreviewPlayerAbsoluteLocation playerAbsLocation)
		{
			this.playerAbsLocation = playerAbsLocation;
			updatePlayerAbsoluteLocation();
		}

		/// <summary>
		/// Called from setPlayerAbsoluteLocation or when window size changes
		/// </summary>
		private void updatePlayerAbsoluteLocation()
		{
			// if left panel (with console) is visible then WMP is not even floating...
			if (showConsole)
				return;

			var playerW = (int)(this.ClientSize.Width * RATIO_absPlayerW_of_windowW);
			var playerH = (int)(this.ClientSize.Height * RATIO_absPlayerH_of_windowH);
			
			//panelPlayerHolder.Visible = true;
			//panelPlayerHolder.BackColor = Color.Red;
			if (playerAbsLocation == PreviewPlayerAbsoluteLocation.TopRight)
			{
				panelPlayerHolder.Size = new Size(playerW, playerH);
				panelPlayerHolder.Location = new Point(this.ClientSize.Width - panelPlayerHolder.Width, 0);
			}
			else if (playerAbsLocation == PreviewPlayerAbsoluteLocation.BottomRight)
			{
				panelPlayerHolder.Size = new Size(playerW, playerH);
				panelPlayerHolder.Location = new Point(this.ClientSize.Width - panelPlayerHolder.Width, this.ClientSize.Height - panelPlayerHolder.Height);
			}
			
		}

		#endregion

		#region ------------------- IVidkaMainForm members ---------------------

		/// <summary>
		/// Called from within here (TogglePreviewMode), as well as by the VidkaFastPreviewPlayerWrapper
		/// </summary>
		public void SwapPreviewPlayerUI(VidkaPreviewMode mode)
		{
			if (mode == VidkaPreviewMode.Normal)
				configureUi_PreviewMode(vidkaPreviewPlayer, vidkaFastPreviewPlayer);
			else if (mode == VidkaPreviewMode.Fast)
				configureUi_PreviewMode(vidkaFastPreviewPlayer, vidkaPreviewPlayer);
		}

		#endregion

	}
}
