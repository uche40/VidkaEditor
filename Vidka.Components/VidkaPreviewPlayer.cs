using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Core;
using WMPLib;
using System.Threading;

namespace Vidka.Components
{
	public enum VidkaPreviewPlayerMode {
		None = 0,
		StillFrame = 1,
		SequentialPlayback = 2,
	}
	public partial class VidkaPreviewPlayer : UserControl, IVideoPlayer
	{
		private const double SHORTEST_TIME_TO_STOP = 0.2;

		private IWMPControls2 Ctlcontrols2;
		private VidkaPreviewPlayerMode CurMode;
		private string curUrl;
		private double stillPositionSec;
		private double curClipSecEnd;

		public VidkaPreviewPlayer()
		{
			InitializeComponent();

			MediaPlayer.PlayStateChange += MediaPlayer_PlayStateChange;
			MediaPlayer.MediaError += MediaPlayer_MediaError;
			MediaPlayer.uiMode = "none";
			//MediaPlayer.settings.autoStart = false;
			Ctlcontrols2 = (IWMPControls2)MediaPlayer.Ctlcontrols;

			CurMode = VidkaPreviewPlayerMode.None;
		}

		private void VidkaPreviewPlayer_Load(object sender, EventArgs e) {}

		#region ============================== IVideoPlayer members =========================

		public void StopWhateverYouArePlaying() {
			Ctlcontrols2.pause();
		}
		public void SetStillFrameNone() {
			MediaPlayer.URL = curUrl = null;			
		}
		public void SetStillFrame(string filename, double offsetSeconds)
		{
			// prepare the message
			CurMode = VidkaPreviewPlayerMode.StillFrame;
			stillPositionSec = offsetSeconds;

			if (curUrl == filename) {
				Ctlcontrols2.currentPosition = stillPositionSec;
				Ctlcontrols2.step(1); //jump to the next frame. This force the player to update frame.
				//MediaPlayer.Refresh();
			}
			else {
				MediaPlayer.URL = curUrl = filename;
				MediaPlayer.Ctlcontrols.currentPosition = stillPositionSec - SHORTEST_TIME_TO_STOP;
				//VideoShitbox.ConsoleSingleton.cxzxc("newURL:" + curUrl);
			}
		}

		public void PlayVideoClip(string filename, double clipSecStart, double clipSecEnd) {
			CurMode = VidkaPreviewPlayerMode.SequentialPlayback;
			curClipSecEnd = clipSecEnd;
			MediaPlayer.URL = curUrl = filename;
			Ctlcontrols2.currentPosition = clipSecStart;
			Ctlcontrols2.play();
		}

		public double GetPositionSec() {
			return Ctlcontrols2.currentPosition;
		}
		public bool IsStopped() {
			return MediaPlayer.playState == WMPPlayState.wmppsStopped;
		}

		#endregion

		#region ============================== WMP callbacks =========================

		private void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
		{
			if (e.newState == 3) // playing
			{
				if (CurMode == VidkaPreviewPlayerMode.StillFrame)
				{
					Ctlcontrols2.pause();
					Ctlcontrols2.currentPosition = stillPositionSec;
					Ctlcontrols2.step(1);
					CurMode = VidkaPreviewPlayerMode.None;
				}
				else if (CurMode == VidkaPreviewPlayerMode.SequentialPlayback)
				{
					if (Ctlcontrols2.currentPosition >= curClipSecEnd)
					{
						Ctlcontrols2.pause();
						CurMode = VidkaPreviewPlayerMode.None;
					}
				}
			}

			//if ((WMPLib.WMPPlayState)e.newState == WMPLib.WMPPlayState.wmppsStopped)
			//{
			//	MessageBox.Show("WMPLib.WMPPlayState.wmppsStopped");
			//}
		}

		private void MediaPlayer_MediaError(object sender, AxWMPLib._WMPOCXEvents_MediaErrorEvent e)
		{
			MessageBox.Show("Cannot play media file.");
		}

		#endregion

	}
}
