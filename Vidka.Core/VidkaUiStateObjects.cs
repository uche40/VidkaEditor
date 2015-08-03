using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidka.Core.Model;

namespace Vidka.Core
{
	/// <summary>
	/// This class serves 2 functions:
	/// 1 - keeps all interactive UI objects in one place.
	/// 2 - provides easier management of when a state changes
	///	 to help trigger repaint only when neccesary.
	/// </summary>
	public class VidkaUiStateObjects
	{
		private bool stateChanged;

		// settable properties
		public ProjectDimensionsTimelineType TimelineHover { get; private set; }
		public VidkaClipVideo CurrentVideoClip { get; private set; }
		public VidkaClipAudio CurrentAudioClip { get; private set; }
		public VidkaClipVideo CurrentVideoClipHover { get; private set; }
		public VidkaClipAudio CurrentAudioClipHover { get; private set; }
		public long? CurrentClipFrameAbsPos { get; private set; }
		public TrimDirection TrimHover { get; private set; }
		public long CurrentMarkerFrame { get; private set; }
		public long MouseDragFrameDelta { get; private set; }
		public EditorDraggy Draggy { get; private set; }
		
		// additional helpers
		public VidkaClip CurrentClip { get {
			return (VidkaClip)CurrentVideoClip ?? (VidkaClip)CurrentAudioClip;
		} }

		public VidkaUiStateObjects() {
			// all the above should be null by default... but what the hell
			TimelineHover = ProjectDimensionsTimelineType.None;
			CurrentVideoClip = null;
			CurrentAudioClip = null;
			CurrentVideoClipHover = null;
			CurrentAudioClipHover = null;
			TrimHover = TrimDirection.None;
			CurrentMarkerFrame = 0;
			MouseDragFrameDelta = 0;
			Draggy = new EditorDraggy();
		}

		#region state change management

		/// <summary>
		/// Call this before every serious interaction method. Then do some shit.
		/// Then call DidSomethingChange() to see if you need to repaint. 
		/// </summary>
		public void ClearStateChangeFlag() {
			stateChanged = false;
		}
		/// <summary>
		/// Call if a repaint is needed anyway, regardless
		/// </summary>
		public void UiStateChanged() {
			stateChanged = true;
		}
		/// <summary>
		/// Call this at the end of every serious interaction method.
		/// if this returns true, then you probably need to repaint. 
		/// </summary>
		public bool DidSomethingChange() {
			return stateChanged;
		}

		/// <summary>
		/// Forces state change to true. Used by EditOp classes, who don't see the whole picture
		/// </summary>
		internal void SomethingDidChangeITellYou() {
			stateChanged = true;
		}

		#endregion

		#region functions that change shit

		internal void SetTimelineHover(ProjectDimensionsTimelineType hover)
		{
			if (TimelineHover != hover)
				stateChanged = true;
			TimelineHover = hover;
		}

		/// <summary>
		/// There can only be one hover b/w video and audio line, so audio will be set to null
		/// </summary>
		public void SetHoverVideo(VidkaClipVideo hover) 
		{
			if (CurrentVideoClipHover != hover ||
				CurrentAudioClipHover != null)
				stateChanged = true;
			CurrentVideoClipHover = hover;
			CurrentAudioClipHover = null;
		}

		/// <summary>
		/// There can only be one hover b/w video and audio line, so video will be set to null
		/// </summary>
		public void SetHoverAudio(VidkaClipAudio hover)
		{
			if (CurrentAudioClipHover != hover ||
				CurrentVideoClipHover != null)
				stateChanged = true;
			CurrentAudioClipHover = hover;
			CurrentVideoClipHover = null;
		}

		/// <summary>
		/// There can only be one selected (active) b/w video and audio line, so audio will be set to null
		/// Needs proj to find absolute frame position (CurrentClipFrameAbsPos)
		/// </summary>
		public void SetActiveVideo(VidkaClipVideo active, VidkaProj proj)
		{
			if (CurrentVideoClip != active ||
				CurrentAudioClip != null)
				stateChanged = true;
			CurrentVideoClip = active;
			CurrentAudioClip = null;
			CurrentClipFrameAbsPos = (active != null)
				? (long?)proj.GetVideoClipAbsFramePositionLeft(active)
				: null;
		}

		/// <summary>
		/// There can only be one selected (active) b/w video and audio line, so video will be set to null
		/// </summary>
		public void SetActiveAudio(VidkaClipAudio active)
		{
			if (CurrentAudioClip != active ||
				CurrentVideoClip != null)
				stateChanged = true;
			CurrentAudioClip = active;
			CurrentVideoClip = null;
			CurrentClipFrameAbsPos = (active != null) ? (long?)active.FrameStart : null;
		}

		public void SetCurrentMarkerFrame(long frame) {
			if (CurrentMarkerFrame != frame)
				stateChanged = true;
			CurrentMarkerFrame = frame;
		}

		internal void IncCurrentMarkerFrame(int frameInc)
		{
			var oldMarker = CurrentMarkerFrame;
			CurrentMarkerFrame += frameInc;
			if (CurrentMarkerFrame < 0)
				CurrentMarkerFrame = 0;
			if (CurrentMarkerFrame != oldMarker)
				stateChanged = true;
		}

		internal void SetTrimHover(TrimDirection trimHover)
		{
			if (trimHover != TrimHover)
				stateChanged = true;
			TrimHover = trimHover;
		}

		internal void setMouseDragFrameDelta(long frameDelta)
		{
			if (MouseDragFrameDelta != frameDelta)
				stateChanged = true;
			MouseDragFrameDelta = frameDelta;
		}

		internal void SetDraggyCoordinates(
			EditorDraggyMode? mode = null,
			long? frameLength = null,
			string text = null,
			int? mouseX = null,
			int? mouseXOffset = null)
		{
			if (mode.HasValue && mode.Value != Draggy.Mode)
				stateChanged = true;
			if (frameLength.HasValue && frameLength.Value != Draggy.FrameLength)
				stateChanged = true;
			if (text != Draggy.Text)
				stateChanged = true;
			if (mouseX.HasValue && mouseX.Value != Draggy.MouseX)
				stateChanged = true;
			if (mouseXOffset.HasValue && mouseXOffset.Value != Draggy.MouseXOffset)
				stateChanged = true;
			Draggy.SetCoordinates(
				mode: mode,
				frameLength: frameLength,
				text: text,
				mouseX: mouseX,
				mouseXOffset: mouseXOffset);
		}

		internal void ClearDraggy() {
			if (Draggy.Mode == EditorDraggyMode.None)
				return;
			Draggy.Clear();
			stateChanged = true;
		}

		internal void SetDraggyVideo(VidkaClipVideo clip)
		{
			if (Draggy.VideoClip != clip)
				stateChanged = true;
			Draggy.VideoClip = clip;
		}

		internal void SetDraggyAudio(VidkaClipAudio clip)
		{
			if (Draggy.AudioClip != clip)
				stateChanged = true;
			Draggy.AudioClip = clip;
		}

		#endregion

		internal void InitDraggyFromDragAndDropMan(
			DragAndDropManager dragAndDropMan,
			int framesSampleQuarterScreen,
			int mouseX)
		{
			// TODO: copied from EditorLogic.... implement please...
			// When u drag and drop a file, how big is it
			SetDraggyCoordinates(
				mode: EditorDraggyMode.VideoTimeline,
				mouseX: mouseX,
				mouseXOffset: framesSampleQuarterScreen / 2,
				text: "Analyzing...",
				frameLength: framesSampleQuarterScreen);
		}
	}

	public enum TrimDirection {
		None = 0,
		Left = 1,
		Right = 2,
	}
}
