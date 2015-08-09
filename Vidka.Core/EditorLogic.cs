using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Vidka.Core.Model;
using System.Threading;
using System.Xml.Serialization;
using System.Windows.Forms;
using Vidka.Core.VideoMeta;
using Vidka.Core.Ops;
using Vidka.Core.Properties;

namespace Vidka.Core
{

	public class EditorLogic : ISomeCommonEditorOperations
	{
		// constants

		/// <summary>
		/// Big step Alt+left/Alt+right
		/// </summary>
		private const int MANY_FRAMES_STEP = 50;
		/// <summary>
		/// Scroll adjustment when marker gets out of bounds and triggers scroll change
		/// </summary>
		private const int SCREEN_MARKER_JUMP_LEEWAY = 10;
		/// <summary>
		/// Pixels to clip border within which bound drag is enabled
		/// </summary>
		private const int BOUND_THRESH_MAX = 30;
		/// <summary>
		/// Once zoomed out and clips are too small
		/// </summary>
		private const int BOUND_THRESH_MIN = 5;
		/// <summary>
		/// how many seconds to play preview
		/// </summary>
		private const double SecMplayerPreview = 5;

		// what we are working with
		private IVideoEditor editor;
		private IVideoPlayer videoPlayer;
		
		// helper logic classes
		private PreviewThreadLauncher previewLauncher;
		private EditOperationAbstract CurEditOp;
		private VidkaIO ioOps;
		private DragAndDropManager dragAndDropMan;
		private EditOperationAbstract[] EditOpsAll;

		// ... for other helper classes see the "object exchange" region

		// my own state shit
		private string curFilename;
		private Stack<UndoableAction> undoStack = new Stack<UndoableAction>();
		private Stack<UndoableAction> redoStack = new Stack<UndoableAction>();
		private int mouseX;
		private int? needToChangeCanvasWidth;
		private int? needToChangeScrollX;

		public EditorLogic(IVideoEditor editor, IVideoPlayer videoPlayer)
		{
			this.editor = editor;
			this.videoPlayer = videoPlayer;
			Proj = new VidkaProj();
			Dimdim = new ProjectDimensions(Proj);
			UiObjects = new VidkaUiStateObjects();
			previewLauncher = new PreviewThreadLauncher(videoPlayer, this);
			ioOps = new VidkaIO();
			IsFileChanged = false;

			EditOpsAll = new EditOperationAbstract[] {
				new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Left, ProjectDimensionsTimelineType.Main),
				new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Right, ProjectDimensionsTimelineType.Main),
				new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Left, ProjectDimensionsTimelineType.Original),
				new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Right, ProjectDimensionsTimelineType.Original),
				new EditOperationMoveVideo(this, UiObjects, Dimdim, editor, videoPlayer),
				new EditOperationSelectOriginalSegment(this, UiObjects, Dimdim, editor, videoPlayer),
			};
			setProjToAllEditOps(Proj);

			FileMapping = Settings.Default.DataNearProject
				? (VidkaFileMapping)new VidkaFileMapping_proj()
				: (VidkaFileMapping)new VidkaFileMapping_resource();
			dragAndDropMan = new DragAndDropManager(editor, Proj, FileMapping);
			dragAndDropMan.MetaReadyForDraggy += dragAndDropMan_MetaReadyForDraggy;
			dragAndDropMan.MetaReadyForOutstandingVideo += dragAndDropMan_MetaReadyForOutstandingVideo;
			dragAndDropMan.MetaReadyForOutstandingAudio += dragAndDropMan_MetaReadyForOutstandingAudio;
			dragAndDropMan.ThumbOrWaveReady += dragAndDropMan_ThumbOrWaveReady;
		}

		#region ============================= drag-drop =============================

		// TODO: do not use global varialbles
		//private VideoMeta.VideoMetadataUseful dragMeta;

		public void MediaFileDragEnter(string[] filenames, int w)
		{
			var framesSampleQuarterScreen = (int)Dimdim.convert_AbsX2Frame(w / 4);
			dragAndDropMan.NewFilesDragged(filenames, framesSampleQuarterScreen);
			___UiTransactionBegin();
			UiObjects.InitDraggyFromDragAndDropMan(dragAndDropMan, framesSampleQuarterScreen, mouseX);
			___UiTransactionEnd();
		}

		public void MediaFileDragMove(int x)
		{
			___UiTransactionBegin();
			UiObjects.SetDraggyCoordinates(mouseX: x);
			___UiTransactionEnd();
		}

		public void MediaFileDragDrop(string[] filenames)
		{
			___UiTransactionBegin();
			if (dragAndDropMan.Mode == DragAndDropManagerMode.DraggingVideo)
			{
				var vclips = dragAndDropMan.FinalizeDragAndMakeVideoClips();
				int draggyVideoShoveIndex = Dimdim.GetVideoClipDraggyShoveIndex(UiObjects.Draggy);
				Proj.ClipsVideo.InsertRange(draggyVideoShoveIndex, vclips);
				Proj.Compile();
				UpdateCanvasWidthFromProjAndDimdim();
			}
			else if (dragAndDropMan.Mode == DragAndDropManagerMode.DraggingAudio)
			{
				var aclips = dragAndDropMan.FinalizeDragAndMakeAudioClips();
				cxzxc("TODO: DragAndDropManagerMode.DraggingAudio");
				Proj.Compile();
				UpdateCanvasWidthFromProjAndDimdim();
			}
			UiObjects.ClearDraggy();
			___UiTransactionEnd();
		}

		public void CancelDragDrop()
		{
			___UiTransactionBegin();
			dragAndDropMan.CancelDragDrop();
			UiObjects.ClearDraggy();
			___UiTransactionEnd();
		}

		private void dragAndDropMan_MetaReadyForDraggy(string filename, VideoMetadataUseful meta)
		{
			___UiTransactionBegin();
			var newLengthFrames = dragAndDropMan.Draggies.FirstOrDefault().LengthInFrames;
			UiObjects.SetDraggyCoordinates(
				text: "" + newLengthFrames + "\nframes",
				frameLength: newLengthFrames
			);
			___UiTransactionEnd();
		}

		private void dragAndDropMan_MetaReadyForOutstandingVideo(VidkaClipVideo vclip, VideoMetadataUseful meta)
		{
			___UiTransactionBegin();
			UpdateCanvasWidthFromProjAndDimdim();
			___UiTransactionEnd();
		}

		private void dragAndDropMan_MetaReadyForOutstandingAudio(VidkaClipAudio aclip, VideoMetadataUseful meta)
		{
			editor.PleaseRepaint();
		}

		private void dragAndDropMan_ThumbOrWaveReady()
		{
			editor.PleaseRepaint();
		}
		
		#endregion

		#region ============================= file save =============================

		public void NewProjectPlease()
		{
			SetProj(new VidkaProj());
			curFilename = null;
			SetFileChanged(false);
			___UiTransactionBegin();
			UiObjects.ClearAll();
			videoPlayer.SetStillFrameNone();
			editor.AskTo_PleaseSetPlayerAbsPosition(PreviewPlayerAbsoluteLocation.TopRight);
			___UiTransactionEnd();
		}

		public void OpenTriggered()
		{
			var filename = editor.OpenProjectOpenDialog();
			if (String.IsNullOrEmpty(filename)) // still null? => user cancelled
				return;
			LoadProjFromFile(filename);
		}

		public void LoadProjFromFile(string filename)
		{
			curFilename = filename;
			editor.AskTo_PleaseSetFormTitle(curFilename);
			SetFileChanged(false);

			// load...
			var proj = ioOps.LoadProjFromFile(curFilename);
			SetProj(proj);

			// update UI...
			___UiTransactionBegin();
			SetFrameMarker_0_ForceRepaint();
			UpdateCanvasWidthFromProjAndDimdim();
			___UiTransactionEnd();
		}

		public void SaveTriggered()
		{
			SaveProject(curFilename);
		}
		public void SaveAsTriggered()
		{
			SaveProject(null);
		}

		public void ExportToAvs()
		{
			if (String.IsNullOrEmpty(curFilename))
				throw new Exception("TODO: handle filenames, open/save XML and export");
			var fileOutAvs = curFilename + ".avs";
			var fileOutVideo = curFilename + Settings.Default.ExportVideoExtension;
			VidkaIO.ExportToAvs(Proj, fileOutAvs);
			editor.iiii("------ export to " + Settings.Default.ExportVideoExtension + "------");
			editor.iiii("Exported to " + fileOutAvs);
			editor.iiii("Exporting to " + fileOutVideo);
			editor.iiii("------ executing: ------");
			var mencoding = new MEncoderMaveVideoFile(fileOutAvs, fileOutVideo);
			editor.iiii(mencoding.FullCommand);
			editor.iiii("------");
			mencoding.RunMEncoder();
			editor.iiii("Exported to " + fileOutVideo);
			editor.iiii("Done export.");
		}

		private void SetProj(VidkaProj proj)
		{
			Proj = proj;

			// init...
			Proj.Compile(); // set up filenames, etc, dunno

			// set proj to all objects who care
			Dimdim.setProj(Proj);
			dragAndDropMan.SetProj(Proj);
			setProjToAllEditOps(Proj);
		}

		private void SaveProject(string filename)
		{
			if (String.IsNullOrEmpty(filename))
				filename = editor.OpenProjectSaveDialog();
			if (String.IsNullOrEmpty(filename)) // still null? => user cancelled
				return;

			ioOps.SaveProjToFile(Proj, filename);
			curFilename = filename;
			SetFileChanged(false);
		}

		#endregion

		#region ============================= object exchange =============================

		/// <summary>
		/// The project... It would be a crime for Logic class not to share it
		/// </summary>
		public VidkaProj Proj { get; private set; }
		/// <summary>
		/// Project dimensions helper class also used in the paint method
		/// </summary>
		public ProjectDimensions Dimdim { get; private set; }
		/// <summary>
		/// Hovers, selected clips, enabled clip bounds
		/// </summary>
		public VidkaUiStateObjects UiObjects { get; private set; }
		public VidkaFileMapping FileMapping { get; private set; }
		public bool IsFileChanged { get; private set; }
		public string CurFileName { get {
			return curFilename;
		} }
		public string CurFileNameShort { get {
			return Path.GetFileName(curFilename);
		} }

		public void SetPreviewPlayer(IVideoPlayer videoPlayer)
		{
			this.videoPlayer = videoPlayer;
			previewLauncher.SetPreviewPlayer(videoPlayer);
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame);
			// TODO: have a common interface maybe?
			foreach (var op in EditOpsAll) {
				op.SetVideoPlayer(videoPlayer);
			}
		}

		public void UiInitialized()
		{
			___UiTransactionBegin();
			UpdateCanvasWidthFromProjAndDimdim();
			___UiTransactionEnd();
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame);
		}

		#endregion

		#region ============================= state tracking for resizing, scrolling and repaint =============================

		/// <summary>
		/// Call this at the begging of every method that potentially changes the state of UI
		/// </summary>
		private void ___UiTransactionBegin() {
			UiObjects.ClearStateChangeFlag();
			needToChangeCanvasWidth = null;
			needToChangeScrollX = null;
		}

		/// <summary>
		/// Call this at the end of every method that potentially changes the state of UI
		/// </summary>
		private void ___UiTransactionEnd() {
			if (needToChangeCanvasWidth.HasValue) {
				editor.UpdateCanvasWidth(needToChangeCanvasWidth.Value);
				___Ui_stateChanged();
			}
			if (needToChangeScrollX.HasValue) {
				editor.UpdateCanvasHorizontalScroll(needToChangeScrollX.Value);
				___Ui_stateChanged();
			}
			if (UiObjects.DidSomethingChange())
				editor.PleaseRepaint();
			if (UiObjects.DidSomethingChange_originalTimeline())
				editor.AskTo_PleaseSetPlayerAbsPosition((UiObjects.CurrentClip != null)
					? PreviewPlayerAbsoluteLocation.BottomRight
					: PreviewPlayerAbsoluteLocation.TopRight);
		}

		/// <summary>
		/// Call this b/w _begin and _end to force repaint
		/// </summary>
		private void ___Ui_stateChanged() {
			UiObjects.UiStateChanged();
		}

		/// <summary>
		/// Call this to update scrollX (forces repaint)
		/// </summary>
		private void ___Ui_updateScrollX(int scrollX) {
			needToChangeScrollX = scrollX;
		}

		/// <summary>
		/// Call this to update canvas width (forces repaint)
		/// </summary>
		private void ___Ui_updateCanvasWidth(int w) {
			needToChangeCanvasWidth = w;
		}

		#endregion

		#region ============================= mouse tracking =============================

		/// <param name="h">Height of the canvas</param>
		/// <param name="w">Width of the canvas</param>
		public void MouseMoved(int x, int y, int w, int h)
		{
			___UiTransactionBegin();
			mouseX = x;
			var timeline = Dimdim.collision_whatTimeline(y, h);
			UiObjects.SetTimelineHover(timeline);
			switch (timeline) {
				case ProjectDimensionsTimelineType.Main:
					var clip = Dimdim.collision_main(x);
					UiObjects.SetHoverVideo(clip);
					CheckClipTrimCollision(x);
					break;
				case ProjectDimensionsTimelineType.Original:
					if (UiObjects.CurrentVideoClip == null)
						break;
					Dimdim.collision_original(x, w,
						UiObjects.CurrentVideoClip.FileLengthFrames,
						UiObjects.CurrentVideoClip.FrameStart,
						UiObjects.CurrentVideoClip.FrameEnd);
					if (Dimdim.lastCollision_succeeded)
						UiObjects.SetHoverVideo(UiObjects.CurrentVideoClip);
					else
						UiObjects.SetHoverVideo(null);
					CheckClipTrimCollision(x);
					break;
				case ProjectDimensionsTimelineType.Audios:
					var aclip = Dimdim.collision_audio(x);
					UiObjects.SetHoverAudio(aclip);
					CheckClipTrimCollision(x);
					break;
				default:
					UiObjects.SetHoverVideo(null);
					UiObjects.SetHoverAudio(null);
					UiObjects.SetTrimHover(TrimDirection.None);
					break;
			}
			//cxzxc("t-hvr:" + UiObjects.TrimHover.ToString() + ",clip:" + UiObjects.CurrentVideoClipHover.cxzxc());
			___UiTransactionEnd();
		}

		public void MouseLeave()
		{
			___UiTransactionBegin();
			UiObjects.SetTimelineHover(ProjectDimensionsTimelineType.None);
			UiObjects.SetHoverVideo(null);
			___UiTransactionEnd();
		}

		//------------------------ helpers -------------------------------

		/// <summary>
		/// Check trim mouse collision and set TrimHover in UiObjects.
		/// recycled lastCollision_x1 and lastCollision_x2 are used.
		/// </summary>
		private void CheckClipTrimCollision(int x)
		{
			if (!Dimdim.lastCollision_succeeded)
			{
				UiObjects.SetTrimHover(TrimDirection.None);
				return;
			}
			var boundThres = BOUND_THRESH_MAX;
			var blockWidth = Dimdim.lastCollision_x2 - Dimdim.lastCollision_x1;
			if (blockWidth < 4 * BOUND_THRESH_MAX)
				boundThres = blockWidth / 4;
			if (x - Dimdim.lastCollision_x1 <= boundThres)
				UiObjects.SetTrimHover(TrimDirection.Left);
			else if (Dimdim.lastCollision_x2 - x <= boundThres)
				UiObjects.SetTrimHover(TrimDirection.Right);
			else
				UiObjects.SetTrimHover(TrimDirection.None);
			UiObjects.SetTrimThreshPixels(boundThres);
		}

		#endregion

		#region ============================= frame of view (scroll/zoom) =============================

		/// <summary>
		/// Called by VideoShitbox when user scrolls with scrollbar or mousewheel
		/// </summary>
		public void setScrollX(int x)
		{
			Dimdim.setScroll(x);
		}

		public void ZoomIn(int width)
		{
			___UiTransactionBegin();
			//Dimdim.ZoomIn(mouseX); // I decided not to zoom into the mouse... too unstable
			Dimdim.ZoomIn(Dimdim.convert_Frame2ScreenX(UiObjects.CurrentMarkerFrame), width);
			UpdateCanvasWidthFromProjAndDimdim();
			UpdateCanvasScrollXFromDimdim();
			___UiTransactionEnd();
		}
		/// <summary>
		/// width parameter is needed here to prevent user from zooming out too much
		/// </summary>
		public void ZoomOut(int width)
		{
			___UiTransactionBegin();
			Dimdim.ZoomOut(mouseX, width);
			UpdateCanvasWidthFromProjAndDimdim();
			UpdateCanvasScrollXFromDimdim();
			___UiTransactionEnd();
		}

		/// <summary>
		/// Call this in ALL spots where proj length is subject to change
		/// </summary>
		private void UpdateCanvasWidthFromProjAndDimdim() {
			var widthNeedsToBeSet = Dimdim.getTotalWidthPixelsForceRecalc();
			___Ui_updateCanvasWidth(widthNeedsToBeSet);
		}

		/// <summary>
		/// Call this in ALL spots where scrollx is subject to change
		/// </summary>
		private void UpdateCanvasScrollXFromDimdim() {
			var scrollx = Dimdim.getCurrentScrollX();
			___Ui_updateScrollX(scrollx);
		}

		#region ============================= marker =============================

		// TODO: the code below does not use UiObjects.ClearStateChangeFlag()
		// - Mon, June 15, 2015

		/// <summary>
		/// Used during playback for animation of the marker (or cursor, if u like...)
		/// </summary>
		public void SetFrameMarker_ForceRepaint(long frame) {
			___UiTransactionBegin();
			UiObjects.SetCurrentMarkerFrame(frame);
			updateFrameOfViewFromMarker();
			___UiTransactionEnd();
		}

		/// <summary>
		/// Used when HOME key is pressed
		/// </summary>
		public void SetFrameMarker_0_ForceRepaint()
		{
			___UiTransactionBegin();
			SetFrameMarker_ShowFrameInPlayer(0);
			___UiTransactionEnd();
		}

		/// <summary>
		/// Used from within this class, when arrow keys are pressed,
		/// by drag ops and other ops (e.g. or when a clip is deleted)
		/// </summary>
		public long SetFrameMarker_ShowFrameInPlayer(long frame)
		{
			printFrameToConsole(frame);
			UiObjects.SetCurrentMarkerFrame(frame);
			updateFrameOfViewFromMarker();
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame); // one more thing... unrelated... update the doggamn WMP
			return frame;
		}

		/// <summary>
		/// Used from within this class, when mouse is Pressed (clicked)
		/// </summary>
		private void SetFrameMarker_NoRepaint_ShowFrameInPlayer(long frame)
		{
			printFrameToConsole(frame);
			UiObjects.SetCurrentMarkerFrame(frame);
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame); // one more thing... unrelated... update the doggamn WMP
		}

		private void printFrameToConsole(long frame) {
			var sec = Proj.FrameToSec(frame);
			var secFloor = (long)sec;
			var secFloorFrame = Proj.SecToFrame(secFloor);
			var frameRemainder = frame - secFloorFrame;
			var timeSpan = TimeSpan.FromSeconds(secFloor);
			cxzxc(String.Format("frame={0} ({1}.{2})"
				, frame
				, timeSpan.ToString((timeSpan.TotalHours >= 1) ? @"hh\:mm\:ss" : @"mm\:ss")
				, frameRemainder));
		}

		private void updateFrameOfViewFromMarker()
		{
			//var frame = UiObjects.CurrentMarkerFrame;
			var screenX = Dimdim.convert_Frame2ScreenX(UiObjects.CurrentMarkerFrame);
			var absX = Dimdim.convert_FrameToAbsX(UiObjects.CurrentMarkerFrame);
			if (screenX < 0)
			{
				// screen jumps back
				int scrollX = absX - editor.Width + SCREEN_MARKER_JUMP_LEEWAY;
				if (scrollX < 0)
					scrollX = 0;
				Dimdim.setScroll(scrollX);
				___Ui_updateScrollX(scrollX);
			}
			else if (screenX >= editor.Width)
			{
				// screen jumps forward
				int scrollX = absX - SCREEN_MARKER_JUMP_LEEWAY;
				var maxScrollValue = Dimdim.getTotalWidthPixels() - editor.Width;
				if (scrollX > maxScrollValue)
					scrollX = maxScrollValue;
				Dimdim.setScroll(scrollX);
				___Ui_updateScrollX(scrollX);
			}
		}

		/// <summary>
		/// This is a marker-related function, so we keep it in the marker region
		/// </summary>
		private long setCurFrameMarkerPosition_fromArrowKeys(Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Left) || keyData == (Keys.Control | Keys.Right))
			{
				long frameOffset = 0;
				var curClipIndex = Proj.GetVideoClipIndexAtFrame_forceOnLastClip(UiObjects.CurrentMarkerFrame, out frameOffset);
				if (curClipIndex == -1)
					return SetFrameMarker_ShowFrameInPlayer(0);
				var clip = Proj.ClipsVideo[curClipIndex];
				var framesToStartOfClip = frameOffset - clip.FrameStart;
				if (keyData == (Keys.Control | Keys.Left))
				{
					if (framesToStartOfClip > 0) // special case: go to beginning of this clip
						return SetFrameMarker_ShowFrameInPlayer(UiObjects.CurrentMarkerFrame - framesToStartOfClip);
					frameOffset = 0;
					if (curClipIndex > 0)
						clip = Proj.ClipsVideo[curClipIndex-1];
				}
				else if (keyData == (Keys.Control | Keys.Right))
				{
					frameOffset = 0;
					if (curClipIndex < Proj.ClipsVideo.Count - 1)
						clip = Proj.ClipsVideo[curClipIndex + 1];
					else
						frameOffset = clip.LengthFrameCalc;
				}
				var frameAbs = Proj.GetVideoClipAbsFramePositionLeft(clip);
				UiObjects.SetActiveVideo(clip, Proj);
				UiObjects.SetHoverVideo(null);
				SetFrameMarker_ShowFrameInPlayer(frameAbs + frameOffset);
				return 0;
			}
			// the usual ... left, right, alt+left, alt+right
			var frameDelta = ArrowKey2FrameDelta(keyData);
			if (frameDelta != 0)
				SetFrameMarker_ShowFrameInPlayer(UiObjects.CurrentMarkerFrame + frameDelta);
			return 0;
		}

		#endregion

		#endregion frome of view

		#region ============================= Playback/Feedback on WMP =============================

		public void PlayPause()
		{
			if (!previewLauncher.IsPlaying)
				previewLauncher.StartPreviewPlayback(Proj, UiObjects.CurrentMarkerFrame);
			else
				previewLauncher.StopPlayback();
		}

		public void PreviewAvsSegmentInMplayer(bool leaveOpen)
		{
			var mplayed = new MPlayerPlaybackSegment(Proj, UiObjects.CurrentMarkerFrame, (long)(Proj.FrameRate * SecMplayerPreview), leaveOpen);
			if (mplayed.ResultCode == OpResultCode.FileNotFound)
				editor.AppendToConsole(VidkaConsoleLogLevel.Error, "Error: please make sure mplayer is in your PATH!");
			else if (mplayed.ResultCode == OpResultCode.OtherError)
				editor.AppendToConsole(VidkaConsoleLogLevel.Error, "Error: " + mplayed.ErrorMessage);
		}

		/// <summary>
		/// Navigate to that frame in the damn AVI file and pause the damn WMP
		/// </summary>
		public void ShowFrameInVideoPlayer(long frame)
		{
			if (previewLauncher.IsPlaying)
				previewLauncher.StopPlayback();
			long frameOffset;
			var clipIndex = Proj.GetVideoClipIndexAtFrame(frame, out frameOffset);
			var secOffset = Proj.FrameToSec(frameOffset);
			if (clipIndex == -1) {
				videoPlayer.SetStillFrameNone();
			}
			else {
				var clip = Proj.ClipsVideo[clipIndex];
				videoPlayer.SetStillFrame(clip.FileName, secOffset);
				//cxzxc("preview1:" + secOffset);
			}
		}

		#endregion

		#region ============================= editing =============================

		#region ---------------------- UNDO/REDO -----------------------------
		
		public void Redo()
		{
			if (!redoStack.Any())
				return;
			var action = redoStack.Pop();
			undoStack.Push(action);

			___UiTransactionBegin();
			action.Redo();
			if (action.PostAction != null)
				action.PostAction();
			SetFileChanged(true);
			___Ui_stateChanged();
			___UiTransactionEnd();
		}
		public void Undo()
		{
			if (!undoStack.Any())
				return;
			var action = undoStack.Pop();
			redoStack.Push(action);

			___UiTransactionBegin();
			action.Undo();
			if (action.PostAction != null)
				action.PostAction();
			SetFileChanged(true);
			___Ui_stateChanged();
			___UiTransactionEnd();
		}
		public void AddUndableAction_andFireRedo(UndoableAction action)
		{
			undoStack.Push(action);
			if (redoStack.Any())
				cxzxc("----------");
			redoStack.Clear();

			___UiTransactionBegin();
			action.Redo();
			if (action.PostAction != null)
				action.PostAction();
			SetFileChanged(true);
			___Ui_stateChanged();
			___UiTransactionEnd();
		}

		private void SetFileChanged(bool changed)
		{
			IsFileChanged = changed;
			editor.AskTo_PleaseSetFormTitle((curFilename ?? "Untitled") + (changed ? " *" : ""));
		}

		#endregion
		
		#region ---------------------- mouse dragging operations -----------------------------

		public void MouseDragStart(MouseButtons button, int x, int y, int w, int h)
		{
			mouseX = x; // prob not needed, since it is always set in mouseMove, but whatever
			
			___UiTransactionBegin();
			if (CurEditOp == null || CurEditOp.DoesNewMouseDragCancelMe)
			{
				// unless we have an active op that requests this drag action,
				// use the mouse press to calculate click collision
				var timeline = Dimdim.collision_whatTimeline(y, h);
				UiObjects.SetTimelineHover(timeline);
				switch (timeline) {
					case ProjectDimensionsTimelineType.Main:
						var clip = Dimdim.collision_main(x);
						UiObjects.SetActiveVideo(clip, Proj);
						break;
					case ProjectDimensionsTimelineType.Original:
						break;
					case ProjectDimensionsTimelineType.Audios:
						var aclip = Dimdim.collision_audio(x);
						UiObjects.SetActiveAudio(aclip);
						break;
					default:
						UiObjects.SetActiveVideo(null, Proj);
						break;
				}

				ActivateCorrectOp((op) => {
					return op.TriggerBy_MouseDragStart(button, x, y);
				});

				// if previous op is still active and it allows us to 
				//CurEditOp = WhatMouseDragOperationDoWeGoInto();
				//if (CurEditOp != null)
				//{
				//	CurEditOp.Init();
				//	//editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Edit mode: " + CurEditOp.Description);
				//}

				// update current frame marker on mouse press
				var cursorFrame = (timeline == ProjectDimensionsTimelineType.Original && UiObjects.CurrentClip != null)
				? (UiObjects.CurrentClipFrameAbsPos ?? 0) - UiObjects.CurrentClip.FrameStart + UiObjects.CurrentClip.FileLengthFrames * x / w
				: Dimdim.convert_ScreenX2Frame(x);
				// NOTE: if you want for negative frames to show original clip's thumb in player, remove this first  
				if (cursorFrame < 0)
					cursorFrame = 0;
				SetFrameMarker_NoRepaint_ShowFrameInPlayer(cursorFrame);
			}
			if (CurEditOp != null)
				CurEditOp.MouseDragStart(x, y, w, h);
			___UiTransactionEnd();
		}

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			___UiTransactionBegin();
			if (CurEditOp != null)
				CurEditOp.MouseDragged(x, y, deltaX, deltaY, w, h);
			___UiTransactionEnd();
		}

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			___UiTransactionBegin();
			if (CurEditOp != null)
			{
				CurEditOp.MouseDragEnd(x, y, deltaX, deltaY, w, h);
				if (CurEditOp.IsDone) {
					CapitulateCurOp();
				}
			}
			___UiTransactionEnd();
		}

		public void EnterPressed()
		{
			if (CurEditOp == null)
				return;
			___UiTransactionBegin();
			CurEditOp.EnterPressed();
			if (CurEditOp.IsDone)
				CapitulateCurOp();
			___UiTransactionEnd();
		}

		public void EscapePressed()
		{
			if (CurEditOp == null)
				return;
			___UiTransactionBegin();
			CapitulateCurOp();
			___UiTransactionEnd();
		}

		public void LeftRightArrowKeys(Keys keyData)
		{
			___UiTransactionBegin();
			if (CurEditOp != null)
			{
				CurEditOp.KeyPressedArrow(keyData);
				var frameDelta = ArrowKey2FrameDelta(keyData);
				if (frameDelta != 0)
					CurEditOp.ApplyFrameDelta(frameDelta);
			}
			else
			{
				setCurFrameMarkerPosition_fromArrowKeys(keyData);
			}
			___UiTransactionEnd();
		}

		public void ControlPressed()
		{
			if (CurEditOp == null)
				return;
			___UiTransactionBegin();
			CurEditOp.ControlPressed();
			___UiTransactionEnd();
		}

		public void ShiftPressed()
		{
			if (CurEditOp == null)
				return;
			___UiTransactionBegin();
			CurEditOp.ShiftPressed();
			___UiTransactionEnd();
		}

		#endregion

		#region ---------------------- split clips -----------------------------

		public void SplitCurClipVideo()
		{
			VidkaClipVideo clip;
			int clipIndex = 0;
			long frameOffsetStartOfVideo = 0;
			if (!DoVideoSplitCalculations(out clip, out clipIndex, out frameOffsetStartOfVideo))
				return;
			var clip_oldStart = clip.FrameStart;
			var clipNewOnTheLeft = clip.MakeCopy();
			clipNewOnTheLeft.FrameEnd = frameOffsetStartOfVideo; // remember, frameOffset is returned relative to start of the media file
			AddUndableAction_andFireRedo(new UndoableAction
			{
				Undo = () =>
				{
					cxzxc("UNDO split");
					Proj.ClipsVideo.Remove(clipNewOnTheLeft);
					clip.FrameStart = clip_oldStart;
				},
				Redo = () =>
				{
					cxzxc("split: location=" + frameOffsetStartOfVideo);
					Proj.ClipsVideo.Insert(clipIndex, clipNewOnTheLeft);
					clip.FrameStart = frameOffsetStartOfVideo;
				}
			});
		}

		public void SplitCurClipVideo_DeleteLeft()
		{
			VidkaClipVideo clip;
			int clipIndex = 0;
			long frameOffsetStartOfVideo = 0;
			if (!DoVideoSplitCalculations(out clip, out clipIndex, out frameOffsetStartOfVideo))
				return;
			var clip_oldStart = clip.FrameStart;
			AddUndableAction_andFireRedo(new UndoableAction
			{
				Undo = () =>
				{
					cxzxc("UNDO splitL: start=" + clip_oldStart);
					clip.FrameStart = clip_oldStart;
					UpdateCanvasWidthFromProjAndDimdim();
				},
				Redo = () =>
				{
					cxzxc("splitL: start=" + frameOffsetStartOfVideo);
					clip.FrameStart = frameOffsetStartOfVideo;
					UpdateCanvasWidthFromProjAndDimdim();
				}
			});
		}

		public void SplitCurClipVideo_DeleteRight()
		{
			VidkaClipVideo clip;
			int clipIndex = 0;
			long frameOffsetStartOfVideo = 0;
			if (!DoVideoSplitCalculations(out clip, out clipIndex, out frameOffsetStartOfVideo))
				return;
			var clip_oldEnd = clip.FrameEnd;
			AddUndableAction_andFireRedo(new UndoableAction
			{
				Undo = () =>
				{
					cxzxc("UNDO splitR: end=" + clip_oldEnd);
					clip.FrameEnd = clip_oldEnd;
					UpdateCanvasWidthFromProjAndDimdim();
				},
				Redo = () =>
				{
					cxzxc("splitR: end=" + frameOffsetStartOfVideo);
					clip.FrameEnd = frameOffsetStartOfVideo;
					UpdateCanvasWidthFromProjAndDimdim();
				}
			});
		}

		/// <summary>
		/// Returns clip being split, its index within video timeline
		/// and how many frames from its FrameStart to cut
		/// </summary>
		private bool DoVideoSplitCalculations(
			out VidkaClipVideo clip,
			out int clipIndex,
			out long frameOffsetStartOfVideo)
		{
			clip = null;
			clipIndex = Proj.GetVideoClipIndexAtFrame(UiObjects.CurrentMarkerFrame, out frameOffsetStartOfVideo);
			if (clipIndex == -1)
			{
				cxzxc("No clip here... Cannot split!");
				return false;
			}
			clip = Proj.GetVideoClipAtIndex(clipIndex);
			if (frameOffsetStartOfVideo == clip.FrameStart)
			{
				cxzxc("On the seam... Cannot split!");
				return false;
			}
			return true;
		}

		#endregion
		
		#region ---------------------- misc operations -----------------------------

		public void DeleteCurSelectedClip()
		{
			if (UiObjects.CurrentVideoClip != null)
			{
				var toRemove = UiObjects.CurrentVideoClip;
				var clipIndex = Proj.ClipsVideo.IndexOf(toRemove);
				AddUndableAction_andFireRedo(new UndoableAction {
					Redo = () => {
						cxzxc("delete vclip " + clipIndex);
						Proj.ClipsVideo.Remove(toRemove);
					},
					Undo = () => {
						cxzxc("UNDO delete vclip " + clipIndex);
						Proj.ClipsVideo.Insert(clipIndex, toRemove);
					},
					PostAction = () => {
						UiObjects.SetHoverVideo(null);
						if (Proj.ClipsVideo.Count == 0) {
							UiObjects.SetActiveVideo(null, Proj);
							SetFrameMarker_0_ForceRepaint();
						}
						else {
							var highlightIndex = clipIndex;
							if (highlightIndex >= Proj.ClipsVideo.Count)
								highlightIndex = Proj.ClipsVideo.Count - 1;
							var clipToSelect = Proj.ClipsVideo[highlightIndex];
							var firstFrameOfSelected = Proj.GetVideoClipAbsFramePositionLeft(clipToSelect);
							UiObjects.SetActiveVideo(clipToSelect, Proj);
							//UiObjects.SetCurrentMarkerFrame(firstFrameOfSelected);
							// TODO: don't repaint twice, rather keep track of whether to repaint or not
							SetFrameMarker_ShowFrameInPlayer(firstFrameOfSelected);
						}
						UpdateCanvasWidthFromProjAndDimdim();
					}
				});
			}
			else if (UiObjects.CurrentAudioClip != null)
			{
				// TODO: undo redo...
				Proj.ClipsAudio.Remove(UiObjects.CurrentAudioClip);
				UiObjects.SetActiveAudio(null);
				UiObjects.SetHoverAudio(null);
				UpdateCanvasWidthFromProjAndDimdim();
			}
		}

		#endregion

		#region ----------------- helpers ------------------------------------

		/// <summary>
		/// Calls setProj for all our EditOps. Call whenever Proj gets reassigned to
		/// </summary>
		private void setProjToAllEditOps(VidkaProj Proj)
		{
			foreach (var op in EditOpsAll)
				op.setProj(Proj);
		}

		/// <summary>
		/// Reset gears to neutral... :P
		/// </summary>
		private void CapitulateCurOp()
		{
			CurEditOp.EndOperation();
			CurEditOp = null;
			//editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Edit mode: none");
		}

		/// <summary>
		/// returns 1, -1, MANY_FRAMES_STEP, -MANY_FRAMES_STEP
		/// </summary>
		private long ArrowKey2FrameDelta(Keys keyData)
		{
			if (keyData == Keys.Left)
				return -1;
			else if (keyData == Keys.Right)
				return 1;
			else if (keyData == (Keys.Alt | Keys.Left)) // like virtualDub :)
				return -MANY_FRAMES_STEP;
			else if (keyData == (Keys.Alt | Keys.Right)) // like virtualDub :)
				return MANY_FRAMES_STEP;
			return 0;
		}

		private void ActivateCorrectOp(
			Func<EditOperationAbstract, bool> trigger
			//, Action<EditOperationAbstract> init
			)
		{
			CurEditOp = EditOpsAll.FirstOrDefault(op => trigger(op));
			if (CurEditOp != null)
				CurEditOp.Init();
		}

		/// <summary>
		/// Debug print to UI console
		/// </summary>
		public void cxzxc(string text) {
			AppendToConsole(VidkaConsoleLogLevel.Debug, text);
		}

		public void AppendToConsole(VidkaConsoleLogLevel level, string s) {
			editor.AppendToConsole(level, s);
		}

		#endregion

		#endregion
	}
}
