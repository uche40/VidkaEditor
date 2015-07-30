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

		// what we are working with
		private IVideoEditor editor;
		private IVideoPlayer videoPlayer;
		
		// helper logic classes
		private PreviewThreadLauncher previewLauncher;
		private EditOperationAbstract CurEditOp;
		private VidkaIO ioOps;
		private DragAndDropManager dragAndDropMan;

		#region operations
		private EditOperationAbstract
			EditOp_TrimLeft,
			EditOp_TrimRight,
			EditOp_TrimLeftOriginal,
			EditOp_TrimRightOriginal,
			EditOp_MoveVideoClip,
			EditOp_CopyVideoClip;
		private EditOperationAbstract[] EditOpsAll;
		#endregion

		// ... for other helper classes see the "object exchange" region

		// my own state shit
		private string curFilename;
		private int mouseX;
		private bool mouseMoveLocked = false; // I believe we are not using this at the moment...


		public EditorLogic(IVideoEditor editor, IVideoPlayer videoPlayer)
		{
			this.editor = editor;
			this.videoPlayer = videoPlayer;
			Proj = new VidkaProj();
			Dimdim = new ProjectDimensions(Proj);
			UiObjects = new VidkaUiStateObjects();
			previewLauncher = new PreviewThreadLauncher(videoPlayer, editor);
			ioOps = new VidkaIO();

			EditOpsAll = new EditOperationAbstract[] {
				EditOp_TrimLeft = new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Left, false),
				EditOp_TrimRight = new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Right, false),
				EditOp_TrimLeftOriginal = new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Left, true),
				EditOp_TrimRightOriginal = new EditOperationTrimVideo(this, UiObjects, Dimdim, editor, videoPlayer, TrimDirection.Right, true),
				EditOp_MoveVideoClip = new EditOperationMoveVideo(this, UiObjects, Dimdim, editor, videoPlayer),
				EditOp_CopyVideoClip = new EditOperationMoveVideo(this, UiObjects, Dimdim, editor, videoPlayer, true),
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

//==================================================================================== debug
			LoadProjFromFile(@"C:\Users\Mikhail\Desktop\asd2");
		}

		#region ============================= drag-drop =============================

		// TODO: do not use global varialbles
		//private VideoMeta.VideoMetadataUseful dragMeta;

		public void MediaFileDragEnter(string[] filenames, int w)
		{
			var framesSampleQuarterScreen = (int)Dimdim.convert_AbsX2Frame(w / 4);
			dragAndDropMan.NewFilesDragged(filenames, framesSampleQuarterScreen);
			UiObjects.ClearStateChangeFlag();
			UiObjects.InitDraggyFromDragAndDropMan(dragAndDropMan, framesSampleQuarterScreen, mouseX);
			RepaintIfNeedBe();
		}

		public void MediaFileDragMove(int x)
		{
			UiObjects.ClearStateChangeFlag();
			UiObjects.SetDraggyCoordinates(mouseX: x);
			RepaintIfNeedBe();
		}

		public void MediaFileDragDrop(string[] filenames)
		{
			if (dragAndDropMan.Mode == DragAndDropManagerMode.DraggingVideo)
			{
				var vclips = dragAndDropMan.FinalizeDragAndMakeVideoClips();
				int draggyVideoShoveIndex = Dimdim.GetVideoClipDraggyShoveIndex(UiObjects.Draggy);
				Proj.ClipsVideo.InsertRange(draggyVideoShoveIndex, vclips);
				Proj.Compile();
				CanvasWidthNeedsToBeUpdated();
			}
			else if (dragAndDropMan.Mode == DragAndDropManagerMode.DraggingAudio)
			{
				var aclips = dragAndDropMan.FinalizeDragAndMakeAudioClips();
				cxzxc("TODO: DragAndDropManagerMode.DraggingAudio");
				Proj.Compile();
				CanvasWidthNeedsToBeUpdated();
			}
			UiObjects.ClearDraggy();
			editor.PleaseRepaint();
		}

		public void CancelDragDrop()
		{
			dragAndDropMan.CancelDragDrop();
			UiObjects.ClearDraggy();
			editor.PleaseRepaint();
		}

		private void dragAndDropMan_MetaReadyForDraggy(string filename, VideoMetadataUseful meta)
		{
			var newLengthFrames = dragAndDropMan.Draggies.FirstOrDefault().LengthInFrames;
			UiObjects.ClearStateChangeFlag();
			UiObjects.SetDraggyCoordinates(
				text: "" + newLengthFrames + "\nframes",
				frameLength: newLengthFrames
			);
			editor.PleaseRepaint();
		}

		private void dragAndDropMan_MetaReadyForOutstandingVideo(VidkaClipVideo vclip, VideoMetadataUseful meta)
		{
			cxzxc("outst meta: " + meta.VideoDurationFrames);
			CanvasWidthNeedsToBeUpdated();
			editor.PleaseRepaint();
		}

		private void dragAndDropMan_MetaReadyForOutstandingAudio(VidkaClipAudio aclip, VideoMetadataUseful meta)
		{
			editor.PleaseRepaint();
		}

		private void dragAndDropMan_ThumbOrWaveReady()
		{
			editor.PleaseRepaint();
		}

		

		//	// generate thumbs and shit
		//	var filenameXml = Proj.Mapping.AddGetMetaFilename(filename);
		//	var filenameThumb = Proj.Mapping.AddGetThumbnailFilename(filename);
		//	var filenameWaveDat = Proj.Mapping.AddGetWaveFilenameDat(filename);
		//	var filenameWaveJpg = Proj.Mapping.AddGetWaveFilenameJpg(filename);

		//	metaGenerator.QueueThisUpPlease(new MetaGeneratorInOtherThread_QueueRequest {
		//		Filename = filename,
		//		FilenameThumb = filenameThumb,
		//		FilenameWaveDat = filenameWaveDat,
		//		FilenameWaveJpg = filenameWaveJpg,
		//	});
		//}

		// TODO: reuse this code in listeners for dragAndDropManager
		//private void metaGenerator_OneItemFinished(MetaGeneratorInOtherThread_QueueRequest item)
		//{
		//	// just repaint the damn thing
		//	editor.PleaseRepaint();
		//}
		//private void genericListener_AppendToConsole(VidkaConsoleLogLevel level, string text)
		//{
		//	editor.AppendToConsole(level, text);
		//}
		//private void metaGenerator_MetaGeneratorDone(MetaGeneratorInOtherThread_QueueRequest item, VideoMetadataUseful meta)
		//{
		//	UiObjects.SetDraggyCoordinates(frameLength: meta.VideoDurationFrames);
		//	editor.PleaseRepaint();
		//}
		
		#endregion

		#region ============================= file save =============================

		public void SaveTriggered() {
			//TODO: request open file dialog
			if (String.IsNullOrEmpty(curFilename))
				curFilename = editor.OpenProjectSaveDialog();
			if (String.IsNullOrEmpty(curFilename)) // still null? => user cancelled
				return;

			ioOps.SaveProjToFile(Proj, curFilename);
		}

		public void OpenTriggered() {
			var filename = editor.OpenProjectOpenDialog();
			if (String.IsNullOrEmpty(filename)) // still null? => user cancelled
				return;
			LoadProjFromFile(filename);
		}

		public void LoadProjFromFile(string filename)
		{
			// load...
			Proj = ioOps.LoadProjFromFile(filename);

			// init...
			Proj.Compile(); // set up filenames, etc, dunno

			// set proj to all objects who care
			Dimdim.setProj(Proj);
			dragAndDropMan.SetProj(Proj);
			setProjToAllEditOps(Proj);

			// repaint...
			CanvasWidthNeedsToBeUpdated();
			UiObjects.SetCurrentMarkerFrame(0);
		}

		public void ExportToAvs()
		{
			if (String.IsNullOrEmpty(curFilename))
				throw new Exception("TODO: handle filenames, open/save XML and export");
			var fileOut = curFilename + ".avs";
			VidkaIO.ExportToAvs(Proj, fileOut);
			editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Exported to " + fileOut);
		}

		// TODO: future
		//public void SaveProjToFile(string filename)
		//{
		//	throw new NotImplementedException();
		//}
		//public void ExportToAvs(string filename)
		//{
		//	throw new NotImplementedException();
		//}

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

		public void UiInitialized()
		{
			CanvasWidthNeedsToBeUpdated();
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame);
		}

		#endregion

		#region ============================= mouse tracking =============================

		/// <param name="h">Height of the canvas</param>
		/// <param name="w">Width of the canvas</param>
		public void MouseMoved(int x, int y, int w, int h)
		{
			if (mouseMoveLocked)
				return;
			UiObjects.ClearStateChangeFlag();
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
			RepaintIfNeedBe();
		}

		public void MouseLeave()
		{
			if (mouseMoveLocked)
				return;
			UiObjects.ClearStateChangeFlag();
			UiObjects.SetTimelineHover(ProjectDimensionsTimelineType.None);
			UiObjects.SetHoverVideo(null);
			RepaintIfNeedBe();
		}

		//------------------------ helpers -------------------------------

		/// <summary>
		/// Call this at the end of every serious interaction method
		/// </summary>
		private void RepaintIfNeedBe() {
			if (UiObjects.DidSomethingChange())
				editor.PleaseRepaint();
		}

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
			if (blockWidth < 2 * BOUND_THRESH_MAX)
				boundThres = BOUND_THRESH_MIN;
			if (x - Dimdim.lastCollision_x1 <= boundThres)
				UiObjects.SetTrimHover(TrimDirection.Left);
			else if (Dimdim.lastCollision_x2 - x <= boundThres)
				UiObjects.SetTrimHover(TrimDirection.Right);
			else
				UiObjects.SetTrimHover(TrimDirection.None);
		}

		#endregion

		#region ============================= frame of view (scroll/zoom) =============================

		public void setNewHorizontalScrollOffset(int x)
		{
			Dimdim.setScroll(x);
		}

		public void ZoomIn(int width)
		{
			//Dimdim.ZoomIn(mouseX); // I decided not to zoom into the mouse... too unstable
			Dimdim.ZoomIn(Dimdim.convert_Frame2ScreenX(UiObjects.CurrentMarkerFrame), width);
			CanvasWidthAndScrollXNeedsToBeUpdated();
		}
		/// <summary>
		/// width parameter is needed here to prevent user from zooming out too much
		/// </summary>
		public void ZoomOut(int width)
		{
			Dimdim.ZoomOut(mouseX, width);
			CanvasWidthAndScrollXNeedsToBeUpdated();
		}

		/// <summary>
		/// Call this in ALL spots where proj length is subject to change
		/// </summary>
		private void CanvasWidthNeedsToBeUpdated() {
			var widthNeedsToBeSet = Dimdim.getTotalWidthPixelsForceRecalc();
			editor.UpdateCanvasWidth(widthNeedsToBeSet);
			editor.PleaseRepaint();
		}

		/// <summary>
		/// Call this in ALL spots where scrollx is subject to change
		/// </summary>
		private void CanvasWidthAndScrollXNeedsToBeUpdated() {
			var widthNeedsToBeSet = Dimdim.getTotalWidthPixelsForceRecalc();
			editor.UpdateCanvasWidth(widthNeedsToBeSet);
			var scrollx = Dimdim.getCurrentScrollX();
			editor.UpdateCanvasHorizontalScroll(scrollx);
			editor.PleaseRepaint();
		}

		#region ============================= marker =============================

		// TODO: the code below does not use UiObjects.ClearStateChangeFlag()
		// - Mon, June 15, 2015

		public void FrameMarkerToBeginning(int w)
		{
			UiObjects.SetCurrentMarkerFrame(0);
			checkMarkerOnScreenAndTakeAppropriateRepaintAction(w);
		}

		private void checkMarkerOnScreenAndTakeAppropriateRepaintAction(int w)
		{
			//var frame = UiObjects.CurrentMarkerFrame;
			var screenX = Dimdim.convert_Frame2ScreenX(UiObjects.CurrentMarkerFrame);
			var absX = Dimdim.convert_FrameToAbsX(UiObjects.CurrentMarkerFrame);
			if (screenX < 0)
			{
				// screen jumps back
				int scrollX = absX - w + SCREEN_MARKER_JUMP_LEEWAY;
				if (scrollX < 0)
					scrollX = 0;
				Dimdim.setScroll(scrollX);
				editor.UpdateCanvasHorizontalScroll(scrollX);
			}
			else if (screenX >= w)
			{
				// screen jumps forward
				int scrollX = absX - SCREEN_MARKER_JUMP_LEEWAY;
				Dimdim.setScroll(scrollX);
				editor.UpdateCanvasHorizontalScroll(scrollX);
			}
			else
				editor.PleaseRepaint();

			// one more thing... unrelated... update the doggamn WMP
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame);
		}

		/// <summary>
		/// This is a marker-related function, so we keep it in the marker region
		/// </summary>
		private void updateCurFrameMarkerPosition(int frameDelta, int w)
		{
			UiObjects.IncCurrentMarkerFrame(frameDelta);
			checkMarkerOnScreenAndTakeAppropriateRepaintAction(w);
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

		/// <summary>
		/// Navigate to that frame in the damn AVI file and pause the damn WMP
		/// </summary>
		public void ShowFrameInVideoPlayer(long frame) {
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

		#region ---------------------- mouse dragging operations -----------------------------

		public void LeftRightArrowKeys(Keys keyData, int w)
		{
			if (CurEditOp != null)
			{
				UiObjects.ClearStateChangeFlag();
				CurEditOp.KeyPressedArrow(keyData);
			}
			int frameDelta = ArrowKey2FrameDelta(keyData);
			if (frameDelta != 0)
			{
				if (CurEditOp != null)
					CurEditOp.ApplyFrameDelta(frameDelta);
				else
					updateCurFrameMarkerPosition(frameDelta, w);
			}
			if (CurEditOp != null)
				RepaintIfNeedBe();
		}

		public void MouseDragStart(int x, int y, int w, int h)
		{
			// if was mouse move was locked and not working...
			if (mouseMoveLocked)
			{
				// ... quickly wake him up and make him do his thing... lol
				mouseMoveLocked = false;
				MouseMoved(x, y, w, h);
			}

			mouseX = x; // prob not needed, since it is always set in mouseMove, but whatever
			UiObjects.ClearStateChangeFlag();

			if (CurEditOp == null || CurEditOp.DoesNewMouseDragCancelMe)
			{
				// unless we have an active op that requests this drag action,
				// use the mouse press to calculate click collision
				CheckCollisionOnMousePress(x, y, w, h);
				
				// if previous op is still active and it allows us to 
				CurEditOp = WhatMouseDragOperationDoWeGoInto();
				if (CurEditOp != null)
				{
					CurEditOp.Init();
					editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Edit mode: " + CurEditOp.Description);
				}
			}
			if (CurEditOp != null)
				CurEditOp.MouseDragStart(x, y, w, h);
			RepaintIfNeedBe();
		}

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			UiObjects.ClearStateChangeFlag();
			if (CurEditOp != null)
				CurEditOp.MouseDragged(x, y, deltaX, deltaY, w, h);
			RepaintIfNeedBe();
		}

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			UiObjects.ClearStateChangeFlag();
			if (CurEditOp != null)
			{
				CurEditOp.MouseDragEnd(x, y, deltaX, deltaY, w, h);
				if (CurEditOp.IsDone) {
					CapitulateCurOp();
				}
			}
			RepaintIfNeedBe();
		}

		public void EnterPressed()
		{
			if (CurEditOp == null)
				return;
			UiObjects.ClearStateChangeFlag();
			CurEditOp.EnterPressed();
			if (CurEditOp.IsDone)
				CapitulateCurOp();
			RepaintIfNeedBe();
		}

		public void EscapePressed()
		{
			if (CurEditOp == null)
				return;
			UiObjects.ClearStateChangeFlag();
			CapitulateCurOp();
			RepaintIfNeedBe();
		}

		/// <summary>
		/// This will be called by ops to enter keyboard mode. Locks mouse move only
		/// </summary>
		public void LockMouseMovements() {
			mouseMoveLocked = true;
			editor.PleaseRepaint();
		}

		#endregion

		#region ---------------------- misc operations -----------------------------

		public void DeleteCurSelectedClip()
		{
			if (UiObjects.CurrentVideoClip != null)
			{
				Proj.ClipsVideo.Remove(UiObjects.CurrentVideoClip);
				UiObjects.SetActiveVideo(null, Proj);
				UiObjects.SetHoverVideo(null);
				CanvasWidthNeedsToBeUpdated();
			}
			else if (UiObjects.CurrentAudioClip != null)
			{
				Proj.ClipsAudio.Remove(UiObjects.CurrentAudioClip);
				UiObjects.SetActiveAudio(null);
				UiObjects.SetHoverAudio(null);
				CanvasWidthNeedsToBeUpdated();
			}
		}

		#endregion

		#region ---------------------- split clips -----------------------------

		public void SplitCurClipVideo()
		{
			VidkaClipVideo left, right;
			SplitCurClipVideo(out left, out right);
			editor.PleaseRepaint();
		}

		public void SplitCurClipVideo_DeleteLeft()
		{
			VidkaClipVideo left, right;
			SplitCurClipVideo(out left, out right);
			Proj.ClipsVideo.Remove(left);
			CanvasWidthNeedsToBeUpdated();
		}

		public void SplitCurClipVideo_DeleteRight()
		{
			VidkaClipVideo left, right;
			SplitCurClipVideo(out left, out right);
			Proj.ClipsVideo.Remove(right);
			if (right == UiObjects.CurrentClip) {
				UiObjects.SetActiveVideo(left, Proj);
				UiObjects.SetHoverVideo(null);
			}
			CanvasWidthNeedsToBeUpdated();
		}

		/// <summary>
		/// left will be copied and inserted before the original clip.
		/// right will be assigned to original clip.
		/// Both will be trimmed to accomodate the split action...
		/// </summary>
		private void SplitCurClipVideo(out VidkaClipVideo left, out VidkaClipVideo right)
		{
			left = right = null;
			long frameOffset;
			var clipIndex = Proj.GetVideoClipIndexAtFrame(UiObjects.CurrentMarkerFrame, out frameOffset);
			if (clipIndex == -1)
			{
				cxzxc("No clip here... Cannot split!");
				return;
			}
			var vclip = Proj.GetVideoClipAtIndex(clipIndex);
			if (frameOffset == vclip.FrameStart)
			{
				cxzxc("On the seam... Cannot split!");
				return;
			}
			right = vclip;
			left = vclip.MakeCopy();
			// remember, frameOffset is returned relative to start of the media file
			left.FrameEnd = frameOffset;
			right.FrameStart = frameOffset;
			//cxzxc("left.FrameEnd=" + left.FrameEnd);
			//cxzxc("right.FrameStart=" + right.FrameStart);
			Proj.ClipsVideo.Insert(clipIndex, left);
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
			mouseMoveLocked = false;
			editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Edit mode: none");
		}

		/// <summary>
		/// This used to be called on mouse click but then there was a conflict b/w click and mouse-up
		/// </summary>
		private void CheckCollisionOnMousePress(int x, int y, int w, int h)
		{
			var timeline = Dimdim.collision_whatTimeline(y, h);
			UiObjects.SetTimelineHover(timeline);
			switch (timeline)
			{
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

			var cursorFrame = (timeline == ProjectDimensionsTimelineType.Original && UiObjects.CurrentClip != null)
				? (UiObjects.CurrentClipFrameAbsPos ?? 0) - UiObjects.CurrentClip.FrameStart + UiObjects.CurrentClip.FileLengthFrames * x / w
				: Dimdim.convert_ScreenX2Frame(x);
			// NOTE: if you want for negative frames to show original clip's thumb in player, remove this first  
			if (cursorFrame < 0)
				cursorFrame = 0;
			UiObjects.SetCurrentMarkerFrame(cursorFrame);
			ShowFrameInVideoPlayer(UiObjects.CurrentMarkerFrame);
		}

		/// <summary>
		/// returns 1, -1, MANY_FRAMES_STEP, -MANY_FRAMES_STEP
		/// </summary>
		private int ArrowKey2FrameDelta(Keys keyData)
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

		/// <summary>
		/// Tries to understand the state of UiObjects and returns the right operation.
		/// Used from MouseDragStart to "switch to the right gear"
		/// </summary>
		private EditOperationAbstract WhatMouseDragOperationDoWeGoInto()
		{
			if (UiObjects.TimelineHover == ProjectDimensionsTimelineType.Main)
			{
				if (UiObjects.CurrentVideoClipHover != null)
				{
					if (UiObjects.TrimHover == TrimDirection.Left)
						return EditOp_TrimLeft;
					else if (UiObjects.TrimHover == TrimDirection.Right)
						return EditOp_TrimRight;
					else if (Form.ModifierKeys == Keys.Control)
						return EditOp_CopyVideoClip;
					else
						return EditOp_MoveVideoClip;

				}
			}
			else if (UiObjects.TimelineHover == ProjectDimensionsTimelineType.Original)
			{
				if (UiObjects.TrimHover == TrimDirection.Left)
					return EditOp_TrimLeftOriginal;
				else if (UiObjects.TrimHover == TrimDirection.Right)
					return EditOp_TrimRightOriginal;
			}
			return null;
		}

		/// <summary>
		/// Debug print to UI console
		/// </summary>
		private void cxzxc(string text) {
			editor.AppendToConsole(VidkaConsoleLogLevel.Debug, text);
		}

		#endregion

		#endregion
	}
}
