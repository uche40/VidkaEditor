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
		/// call this whenever a new clip is added and frame rate changes to set helper variables
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
		/// position (frames) wrt project
		/// </summary>
		public long FrameStart { get; set; }
		/// <summary>
		/// position (frames) wrt project
		/// </summary>
		public long FrameEnd { get; set; }
		public double? FileLengthSec { get; set; }

		// helper
		/// <summary>
		/// (FrameEnd - FrameStart) of this clip
		/// </summary>
		[XmlIgnore]
		public long LengthFrameCalc { get { return FrameEnd - FrameStart; } }
		/// <summary>
		/// Needs to be set by multiplying FileLengthSec by frame rate of proj
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
		/// position (frames) wrt audio file of the beginning of audio clip
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
