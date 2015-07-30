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
	}

	public class PreviewThreadLauncher
	{
		private const double SECONDS_PAUSE_MIN = 0.2;
		private const double SECONDS_PAUSE_MAX = 0.3;
		private const double STOP_BEFORE_THRESH = 1/30.0;
		private IVideoPlayer player;
		private IVideoEditor editor;
		private PreviewThreadMutex mutex;
		private Timer ticker;

		//current state
		

		public PreviewThreadLauncher(IVideoPlayer player, IVideoEditor editor) {
			this.player = player;
			this.editor = editor;
			mutex = new PreviewThreadMutex();
			ticker = new Timer();
			ticker.Tick += PlaybackTickerFunc;
		}

		public void StartPreviewPlayback(VidkaProj proj, long frameStart)
		{
			lock (mutex)
			{
				// ... what we are going to play
				long frameOffset;
				var curClipIndex = proj.GetVideoClipIndexAtFrame(frameStart, out frameOffset);
				if (curClipIndex == -1)
					return;
				var clip = proj.ClipsVideo[curClipIndex];
				// ... set up mutex
				mutex.Proj = proj;
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
				//editor.PlaybackSetFrameMarker(mutex.CurFrame);
				var sec = player.GetPositionSec();
				if (sec >= mutex.CurStopPositionSec - STOP_BEFORE_THRESH)
				{
					player.StopWhateverYouArePlaying();
					mutex.CurClipIndex++;
					var clip = mutex.Proj.GetVideoClipAtIndex(mutex.CurClipIndex);
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
			var clipSecStart = mutex.Proj.FrameToSec(frameOffsetCustom ?? clip.FrameStart); //hacky, i know
			var clipSecEnd = mutex.Proj.FrameToSec(clip.FrameEnd); //hacky, i know
			mutex.CurStopPositionSec = clipSecEnd;
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

		public bool IsPlaying { get { return mutex.IsPlaying; } }
	}
}
