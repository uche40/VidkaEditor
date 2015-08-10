using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Serialization;

namespace Vidka.Core.Model
{
	public class VidkaProj
	{
		public VidkaProj()
		{
			ClipsVideo = new List<VidkaClipVideo>();
			ClipsAudio = new List<VidkaClipAudio>();
			FrameRate = 30;
			Width = 1280;
			Height = 720;
		}

		public string Name { get; set; }
		public double FrameRate { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public List<VidkaClipVideo> ClipsVideo { get; set; }
		public List<VidkaClipAudio> ClipsAudio { get; set; }
		
		/// <summary>
		/// call this whenever a new clip is added and frame rate changes.
		/// This will set all the helper variables in every clip
		/// </summary>
		public void Compile()
		{
			foreach (var vclip in ClipsVideo) {
				vclip.FileLengthFrames = this.SecToFrame(vclip.FileLengthSec ?? 0); //TODO qwe
			}
		}
	}

	public class VidkaClip
	{
		public string FileName { get; set; }
		/// <summary>
		/// position (frames) wrt file
		/// </summary>
		public long FrameStart { get; set; }
		/// <summary>
		/// position (frames) wrt file
		/// </summary>
		public long FrameEnd { get; set; }
		/// <summary>
		/// Stored in seconds, but we will only use it to convert to FileLengthFrames with proj-fps
		/// </summary>
		public double? FileLengthSec { get; set; }
		/// <summary>
		/// will not be able to trim this clip anymore, it is marked different in UI.
		/// This helps to tell good clips from the rest of the garbage
		/// </summary>
		public bool IsLocked { get; set; }

		// helpers

		/// <summary>
		/// (FrameEnd - FrameStart) of this clip
		/// </summary>
		[XmlIgnore]
		public long LengthFrameCalc { get { return FrameEnd - FrameStart; } }
		/// <summary>
		/// Needs to be set by multiplying FileLengthSec by proj-fps
		/// </summary>
		[XmlIgnore]
		public long FileLengthFrames { get; set; }
		[XmlIgnore]
		public bool IsNotYetAnalyzed { get; set; }
	}

	public class VidkaClipVideo : VidkaClip
	{
		public VidkaClipVideo() {
			Subtitles = new List<VidkaSubtitle>();
		}

		public List<VidkaSubtitle> Subtitles { get; private set; }

		public VidkaClipVideo MakeCopy()
		{
			var clip = (VidkaClipVideo)this.MemberwiseClone();
			// TODO: copy over non-shallow values (subtitles, etc)
			return clip;
		}
	}

	public class VidkaClipAudio : VidkaClip
	{
		public VidkaClipAudio() { }

		/// <summary>
		/// position (frames) wrt project's beginning of the start of this audio clip
		/// </summary>
		public int FrameOffset { get; set; }

		public VidkaClipAudio MakeCopy()
		{
			var clip = (VidkaClipAudio)this.MemberwiseClone();
			return clip;
		}
	}

	public class VidkaSubtitle {
		public VidkaSubtitle() {
		}

		public string Text { get; set; }
		/// <summary>
		/// relative to Frame 0 of video this subtitle pertains to
		/// </summary>
		public int FrameStart { get; set; }
		/// <summary>
		/// relative to Frame 0 of video this subtitle pertains to
		/// </summary>
		public int FrameEnd { get; set; }
	}
}
