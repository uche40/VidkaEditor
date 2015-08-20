using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vidka.Core.Model;

namespace Vidka.Core
{

	public class PreviewThreadMutex {
		public PreviewThreadMutex() {
			IsPlaying = false;
		}
		public bool IsPlaying { get; set; }
		public VidkaProj Proj { get; set; }
		public int CurClipIndex { get; set; }
		public long CurFrame { get; set; }
		public double CurStopPositionSec { get; set; }
		// both of these used for frame marker positioning
		public long CurClipAbsFrameLeft { get; set; }
		public long CurClipStartFrame { get; set; }

		public bool OnlyLockedClips { get; set; }
	}

	public class PreviewThreadLauncher
	{
		private const double SECONDS_PAUSE_MIN = 0.2;
		private const double SECONDS_PAUSE_MAX = 0.3;
		private const double STOP_BEFORE_THRESH = 1/30.0;
		private IVideoPlayer player;
		private ISomeCommonEditorOperations editor;
		private PreviewThreadMutex mutex;
		private Timer ticker;

		//current state


		public PreviewThreadLauncher(IVideoPlayer player, ISomeCommonEditorOperations editor) {
			this.player = player;
			this.editor = editor;
			mutex = new PreviewThreadMutex();
			ticker = new Timer();
			ticker.Tick += PlaybackTickerFunc;
		}

		// called when swappng players for fast mode
		public void SetPreviewPlayer(IVideoPlayer videoPlayer)
		{
			this.player = videoPlayer;
		}

		public void StartPreviewPlayback(VidkaProj proj, long frameStart, bool onlyLockedClips)
		{
			lock (mutex)
			{
				// ... what we are going to play
				long frameOffset;
				var curClipIndex = proj.GetVideoClipIndexAtFrame(frameStart, out frameOffset);
				var clip = onlyLockedClips
					? proj.GetNextLockedVideoClipStartingAtIndex(curClipIndex, out curClipIndex)
					: proj.GetVideoClipAtIndex(curClipIndex);
				if (clip == null) {
					editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Nothing to play!");
					return;
				}
				// ... set up mutex
				mutex.Proj = proj;
				mutex.OnlyLockedClips = onlyLockedClips;
				mutex.IsPlaying = true;
				mutex.CurClipIndex = curClipIndex;
				mutex.CurFrame = frameStart;
				StartPlaybackOfClip(clip, frameOffset);
				// ... set up ticker
				ticker.Interval = (int)(1000 * proj.FrameToSec(1)); // 1 tick per frame... its a hack but im too lazy
				ticker.Start();
				editor.AppendToConsole(VidkaConsoleLogLevel.Debug, "StartPlayback");
			}
		}

		private void PlaybackTickerFunc(object sender, EventArgs e)
		{
			lock (mutex)
			{
				mutex.CurFrame++;
				var secCurClip = player.GetPositionSec();
				var frameMarkerPosition = mutex.CurClipAbsFrameLeft + mutex.Proj.SecToFrame(secCurClip) - mutex.CurClipStartFrame;
				editor.SetFrameMarker_ForceRepaint(frameMarkerPosition);
				if (secCurClip >= mutex.CurStopPositionSec - STOP_BEFORE_THRESH || player.IsStopped())
				{
					var newIndex = mutex.CurClipIndex + 1;
					var clip = mutex.OnlyLockedClips
						? mutex.Proj.GetNextLockedVideoClipStartingAtIndex(newIndex, out newIndex)
						: mutex.Proj.GetVideoClipAtIndex(newIndex);
					mutex.CurClipIndex = newIndex;
					if (clip == null)
					{
						StopPlayback();
					}
					else
					{
						StartPlaybackOfClip(clip);
						editor.AppendToConsole(VidkaConsoleLogLevel.Debug, "Next clip: " + mutex.CurClipIndex);
					}
				}

			}

		}

		private void StartPlaybackOfClip(VidkaClipVideo clip, long? frameOffsetCustom = null)
		{
			mutex.CurClipAbsFrameLeft = mutex.Proj.GetVideoClipAbsFramePositionLeft(clip);
			mutex.CurClipStartFrame = clip.FrameStart;
			var clipSecStart = mutex.Proj.FrameToSec(frameOffsetCustom ?? clip.FrameStart); //hacky, i know
			var clipSecEnd = mutex.Proj.FrameToSec(clip.FrameEnd); //hacky, i know
			mutex.CurStopPositionSec = clipSecEnd;
			editor.SetCurrentVideoClip_ForceRepaint(clip);
			player.PlayVideoClip(clip.FileName, clipSecStart, clipSecEnd);
		}

		public void StopPlayback()
		{
			lock (mutex)
			{
				ticker.Stop();
				mutex.IsPlaying = false;
				player.StopWhateverYouArePlaying();
				editor.AppendToConsole(VidkaConsoleLogLevel.Debug, "StopPlayback");
			}

			//curThread
			
		}

		internal void SplitPerformedIncrementClipIndex()
		{
			lock (mutex)
			{
				mutex.CurClipIndex++;
			}
		}

		public bool IsPlaying { get { return mutex.IsPlaying; } }
	}
}
