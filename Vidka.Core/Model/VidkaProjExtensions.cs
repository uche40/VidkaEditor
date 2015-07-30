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

		public static void SwitchVideoClips(this VidkaProj proj, int oldIndex, int newIndex)
		{
			var clip = proj.ClipsVideo[oldIndex];
			proj.ClipsVideo.RemoveAt(oldIndex);
			if (newIndex > oldIndex)
				newIndex -= 1; // we removed it, so it will be 1 less
			if (newIndex >= proj.ClipsVideo.Count)
				proj.ClipsVideo.Add(clip);
			else
				proj.ClipsVideo.Insert(newIndex, clip);
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
