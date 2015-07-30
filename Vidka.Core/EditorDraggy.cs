using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidka.Core.Error;
using Vidka.Core.Model;

namespace Vidka.Core
{
	/// <summary>
	/// stores information about dragging clips around video or audio
	/// </summary>
	public class EditorDraggy
	{
		public EditorDraggyMode Mode { get; private set; }
		public long FrameLength { get; private set; }
		public string Text { get; private set; }
		public int MouseX { get; private set; }
		public int MouseXOffset { get; private set; }
		/// <summary>
		/// Only set this value in UiObjects's setDraggyVideo()
		/// </summary>
		public VidkaClipVideo VideoClip { get; set; }
		/// <summary>
		/// Only set this value in UiObjects's setDraggyAudio()
		/// </summary>
		public VidkaClipAudio AudioClip { get; set; }

		public EditorDraggy() {
			SetCoordinates();
		}

		/// <summary>
		/// Make sure u dont set mode to none from here! Do it only in clear!
		/// </summary>
		internal void SetCoordinates(
			EditorDraggyMode? mode = null,
			long? frameLength = null,
			string text = null,
			int? mouseX = null,
			int? mouseXOffset = null)
		{
			if (mode.HasValue && mode.Value == EditorDraggyMode.None)
				throw new HowTheFuckDidThisHappenException(null, "Trying to set draggy mode to None in setCoordinates! Should do it in clear!");
			if (mode.HasValue)
				Mode = mode.Value;
			if (frameLength.HasValue)
				FrameLength = frameLength.Value;
			if (text != null)
				Text = text;
			if (mouseX.HasValue)
				MouseX = mouseX.Value;
			if (mouseXOffset.HasValue)
				MouseXOffset = mouseXOffset.Value;
		}

		internal void Clear()
		{
			Mode = EditorDraggyMode.None;
			Text = null;
			FrameLength = 0;
			MouseX = 0;
			MouseXOffset = 0;
			VideoClip = null;
			AudioClip = null;
		}
	}

	public enum EditorDraggyMode {
		None = 0,
		VideoTimeline = 1,
		AudioTimeline = 2,
	}
}
