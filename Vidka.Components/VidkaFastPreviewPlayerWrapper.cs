using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidka.Core;

namespace Vidka.Components
{
	/// <summary>
	/// Since VidkaFastPreviewPlayer.cs does not support playback, we need to wrap inside
	/// this wrapper class to swap playerFast and playerWmp depending on whether we are doing playback
	/// </summary>
	public class VidkaFastPreviewPlayerWrapper : IVideoPlayer
	{
		private VidkaFastPreviewPlayer playerFast;
		private IVideoPlayer playerWmp;
		private IVidkaMainForm form;
		private bool isWmpEnabled = false;

		public VidkaFastPreviewPlayerWrapper(
			VidkaFastPreviewPlayer playerFast,
			IVideoPlayer playerWmp,
			IVidkaMainForm form)
		{
			this.playerFast = playerFast;
			this.playerWmp = playerWmp;
			this.form = form;
		}

		public void SetStillFrameNone() {
			setWmpEnabled(false);
			playerFast.SetStillFrameNone();
		}
		public void SetStillFrame(string filename, double offsetSeconds) {
			setWmpEnabled(false);
			playerFast.SetStillFrame(filename, offsetSeconds);
		}
		public void PlayVideoClip(string filename, double clipSecStart, double clipSecEnd) {
			setWmpEnabled(true);
			playerWmp.PlayVideoClip(filename, clipSecStart, clipSecEnd);
		}
		public void StopWhateverYouArePlaying() {
			setWmpEnabled(false);
			playerWmp.StopWhateverYouArePlaying();
		}
		public double GetPositionSec() {
			return playerWmp.GetPositionSec();
		}
		public bool IsStopped() {
			return playerWmp.IsStopped();
		}

		private void setWmpEnabled(bool enabled)
		{
			if (isWmpEnabled == enabled)
				return;
			isWmpEnabled = enabled;
			form.SwapPreviewPlayerUI(isWmpEnabled ? VidkaPreviewMode.Normal : VidkaPreviewMode.Fast);
		}
	}

	//====================== misc enum and interfaces =============================

	public interface IVidkaMainForm
	{
		void SwapPreviewPlayerUI(VidkaPreviewMode mode);
	}

	public enum VidkaPreviewMode
	{
		Normal = 1,
		Fast = 2,
	}
}
