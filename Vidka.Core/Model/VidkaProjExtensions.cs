using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.Core.Model
{
	public static class VidkaProjExtensions
	{
		#region ================================= helper methods for proj class ========================

		public static double FrameToSec(this VidkaProj proj, long frame) {
			return frame / proj.FrameRate;
		}
		public static int SecToFrame(this VidkaProj proj, double sec) {
			return (int)(sec * proj.FrameRate);
		}

		/// <summary>
		/// used in ProjectDimensions.recalculateProjectWidth.
		/// The other part of this calculation is GetTotalLengthOfAudioClipsSec 
		/// </summary>
		public static long GetTotalLengthOfVideoClipsFrame(this VidkaProj proj)
		{
			long totalFrames = 0;
			foreach (var ccc in proj.ClipsVideo) {
				totalFrames += ccc.LengthFrameCalc;
			}
			return totalFrames;
		}

		/// <summary>
		/// used in ProjectDimensions.recalculateProjectWidth.
		/// </summary>
		public static long GetTotalLengthOfAudioClipsFrame(this VidkaProj proj)
		{
			long maxFrame = 0;
			foreach (var ccc in proj.ClipsAudio) {
				maxFrame = Math.Max(maxFrame, ccc.FrameEnd);
			}
			return maxFrame;
		}

		/// <summary>
		/// returns null if index is out of bounds
		/// </summary>
		public static VidkaClipVideo GetVideoClipAtIndex(this VidkaProj proj, int index)
		{
			if (index < 0 || index >= proj.ClipsVideo.Count)
				return null;
			return proj.ClipsVideo[index];
		}

		/// <summary>
		/// returns either the first or last clip if index is out of bounds respectively.
		/// If there are no clips at all, returns null
		/// </summary>
		public static VidkaClipVideo GetVideoClipAtIndexForce(this VidkaProj proj, int index)
		{
			if (proj.ClipsVideo.Count == 0)
				return null;
			if (index < 0)
				return proj.ClipsVideo.FirstOrDefault();
			if (index >= proj.ClipsVideo.Count)
				return proj.ClipsVideo.LastOrDefault();
			return proj.ClipsVideo[index];
		}
		

		/// <summary>
		/// Returns index of the clip under the given frame (curFrame) and also how far into the clip,
		/// the marker is (out frameOffset). NOTE: frameOffset is relative to beginning of the video file,
		/// not to clip.FrameStart!
		/// If curFrame is not on any of the clips -1 is returned
		/// </summary>
		public static int GetVideoClipIndexAtFrame(this VidkaProj proj, long curFrame, out long frameOffset)
		{
			frameOffset = 0;
			long totalFrame = 0;
			int index = 0;
			foreach (var ccc in proj.ClipsVideo)
			{
				if (curFrame >= totalFrame && curFrame < totalFrame + ccc.LengthFrameCalc)
				{
					frameOffset = curFrame - totalFrame + ccc.FrameStart;
					return index;
				}
				index++;
				totalFrame += ccc.LengthFrameCalc;
			}
			return -1;
		}

		/// <summary>
		/// Returns same thing as GetVideoClipIndexAtFrame, except when the marker is too far out,
		/// it returns the last clip's index and frameOffset = clip.FrameEnd (again, relative to start of file)
		/// If there are no clips at all, returns -1 and frameOffset is 0
		/// </summary>
		public static int GetVideoClipIndexAtFrame_forceOnLastClip(this VidkaProj proj, long curFrame, out long frameOffset)
		{
			var index = proj.GetVideoClipIndexAtFrame(curFrame, out frameOffset);
			if (index == -1 && proj.ClipsVideo.Count > 0) {
				// ze forcing...
				index = proj.ClipsVideo.Count - 1;
				frameOffset = proj.ClipsVideo[index].FrameEnd;
			}
			return index;
		}
		

		/// <summary>
		/// The inverse of GetVideoClipIndexAtFrame.
		/// Instead returns the frame of the clip (left side) within project absolute frame space.
		/// Returns -1 if the clip is not even in the project
		/// </summary>
		public static long GetVideoClipAbsFramePositionLeft(this VidkaProj proj, VidkaClipVideo clip)
		{
			long totalFrames = 0;
			foreach (var ccc in proj.ClipsVideo)
			{
				if (ccc == clip)
					return totalFrames;
				totalFrames += ccc.LengthFrameCalc;
			}
			return -1;
		}

		public static VidkaProj Crop(this VidkaProj proj, long frameStart, long framesLength, int? newW=null, int? newH=null)
		{
			var newProj = new VidkaProj() {
				FrameRate = proj.FrameRate,
				Width = newW ?? proj.Width,
				Height = newH ?? proj.Height,
			};
			long frameEnd = frameStart + framesLength;
			long curFrame = 0;
			foreach (var vclip in proj.ClipsVideo) {
				var curFrame2 = curFrame + vclip.LengthFrameCalc; // abs right bound of vclip
				// outside: too early
				if (curFrame2 <= frameStart) {
					curFrame += vclip.LengthFrameCalc;
					continue;
				}
				// outside: too late
				if (curFrame > frameEnd)
					break;
				var newVClip = vclip.MakeCopy();
				// trim start, if neccessary
				if (curFrame < frameStart)
					newVClip.FrameStart += (frameStart - curFrame);
				// trim end, if neccessary
				if (curFrame2 > frameEnd)
					newVClip.FrameEnd -= (curFrame2 - frameEnd);
				newProj.ClipsVideo.Add(newVClip);
				curFrame += vclip.LengthFrameCalc;
			}
			return newProj;
		}

		#endregion

		#region ================================= helper methods for clips ========================

		/// <summary>
		/// Returns what the delta should be not to violate the trimming of this clip
		/// </summary>
		public static long HowMuchCanBeTrimmed(this VidkaClipVideo clip, TrimDirection side, long delta)
		{
			if (clip == null)
				return 0;
			if (side == TrimDirection.Left)
			{
				var frame = clip.FrameStart + delta;
				if (frame < 0)
					return -clip.FrameStart; // to make 0
				else if (frame >= clip.FrameEnd)
					return -clip.FrameStart + clip.FrameEnd - 1; // to make frameEnd-1
				return delta;
			}
			else if (side == TrimDirection.Right)
			{
				var frame = clip.FrameEnd + delta;
				if (frame <= clip.FrameStart)
					return -clip.FrameEnd + clip.FrameStart + 1; // to male frameStart+1
				else if (frame >= clip.FileLengthFrames)
					return -clip.FrameEnd + clip.FileLengthFrames; // to make clip.LengthFrameCalc
				return delta;
			}
			return 0;
		}

		/// <summary>
		/// Debug description
		/// </summary>
		public static string cxzxc(this VidkaClipVideo clip) {
			if (clip == null)
				return "null";
			return Path.GetFileName(clip.FileName);
		}


		#endregion


	}
}
