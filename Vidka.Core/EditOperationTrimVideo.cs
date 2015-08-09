using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vidka.Core.Error;
using Vidka.Core.Model;
using Vidka.Core.Properties;

namespace Vidka.Core
{
	class EditOperationTrimVideo : EditOperationAbstract
	{
		private TrimDirection side;
		private ProjectDimensionsTimelineType timeline;
		private bool keyboardMode;

		public EditOperationTrimVideo(ISomeCommonEditorOperations iEditor,
			VidkaUiStateObjects uiObjects,
			ProjectDimensions dimdim,
			IVideoEditor editor,
			IVideoPlayer videoPlayer,
			TrimDirection side,
			ProjectDimensionsTimelineType timeline)
			: base(iEditor, uiObjects, dimdim, editor, videoPlayer)
		{
			this.side = side;
			this.timeline = timeline;
			keyboardMode = false;
		}

		public override string Description { get {
			return "Trim video (" + side.ToString() + ")";
		} }

		public override bool TriggerBy_MouseDragStart(MouseButtons button, int x, int y)
		{
			return (button == MouseButtons.Left)
				&& (uiObjects.TimelineHover == timeline)
				&& (uiObjects.CurrentVideoClipHover != null)
				&& (uiObjects.TrimHover == side);
		}

		public override void MouseDragStart(int x, int y, int w, int h)
		{
			IsDone = false;
			// I assume its not null, otherwise how do u have CurrentAudioClipTrimHover?
			var clip = uiObjects.CurrentVideoClipHover;
			uiObjects.SetActiveVideo(clip, proj);
			keyboardMode = false;
		}

		public override void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			var clip = uiObjects.CurrentVideoClip;
			var frameDelta = (timeline == ProjectDimensionsTimelineType.Original)
				? (long)(clip.FileLengthFrames * deltaX / w)
				: dimdim.convert_AbsX2Frame(deltaX);
			//cxzxc("fd:" + frameDelta + ",isO:" + isOriginal);
			var frameDeltaContrained = clip.HowMuchCanBeTrimmed(side, frameDelta);
			
			// set UI objects...
			uiObjects.setMouseDragFrameDelta(frameDeltaContrained);

			// show in video player
			var frameEdge = (side == TrimDirection.Right) ? clip.FrameEnd-1 : clip.FrameStart;
			var second = proj.FrameToSec(frameEdge + frameDeltaContrained);
			videoPlayer.SetStillFrame(clip.FileName, second);
		}

		public override void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			var clip = uiObjects.CurrentVideoClip;
			var deltaConstrained = clip.HowMuchCanBeTrimmed(side, uiObjects.MouseDragFrameDelta);
			if (deltaConstrained != 0)
			{
				iEditor.AddUndableAction_andFireRedo(new UndoableAction() {
					Redo = () => {
						cxzxc("Trim " + side + ": " + deltaConstrained);
						if (side == TrimDirection.Left)
							clip.FrameStart += deltaConstrained;
						else if (side == TrimDirection.Right)
							clip.FrameEnd += deltaConstrained;
					},
					Undo = () => {
						cxzxc("UNDO Trim " + side + ": " + deltaConstrained);
						if (side == TrimDirection.Left)
							clip.FrameStart -= deltaConstrained;
						else if (side == TrimDirection.Right)
							clip.FrameEnd -= deltaConstrained;
					},
					PostAction = () => {
						long frameMarker = proj.GetVideoClipAbsFramePositionLeft(clip);
						var rightThreshFrames = proj.SecToFrame(Settings.Default.RightTrimMarkerOffsetSeconds);
						if (side == TrimDirection.Right && clip.LengthFrameCalc > rightThreshFrames)
							frameMarker += clip.LengthFrameCalc - rightThreshFrames;
						iEditor.SetFrameMarker_ShowFrameInPlayer(frameMarker);
					}
				});
			}
			if (uiObjects.MouseDragFrameDelta != 0)
			{
				// switch to KB mode
				keyboardMode = true;
				editor.AppendToConsole(VidkaConsoleLogLevel.Info, "Use arrow keys to adjust...");
			}
			else
			{
				// if there was no change (mouse click) then cancel this op
				IsDone = true;
			}
			uiObjects.setMouseDragFrameDelta(0);
		}

		public override void ApplyFrameDelta(long deltaFrame)
		{
			if (!keyboardMode)
				return;
			performDefensiveProgrammingCheck();
			var clip = uiObjects.CurrentVideoClip;
			var deltaConstrained = clip.HowMuchCanBeTrimmed(side, deltaFrame);
			if (deltaConstrained != 0)
			{
				iEditor.AddUndableAction_andFireRedo(new UndoableAction() {
					Redo = () => {
						cxzxc("Trim " + side + ": " + deltaConstrained);
						if (side == TrimDirection.Left)
							clip.FrameStart += deltaConstrained;
						else if (side == TrimDirection.Right)
							clip.FrameEnd += deltaConstrained;
					},
					Undo = () => {
						cxzxc("UNDO Trim " + side + ": " + deltaConstrained);
						if (side == TrimDirection.Left)
							clip.FrameStart -= deltaConstrained;
						else if (side == TrimDirection.Right)
							clip.FrameEnd -= deltaConstrained;
					},
					PostAction = () => {
						// show in video player
						var frameEdge = (side == TrimDirection.Right) ? clip.FrameEnd - 1 : clip.FrameStart;
						var second = proj.FrameToSec(frameEdge);
						videoPlayer.SetStillFrame(clip.FileName, second);
						//cxzxc("preview2:" + second);
					}
				});
			}
			// set ui objects (repaint regardless to give feedback to user that this operation is still in action)
			uiObjects.SetHoverVideo(clip);
			uiObjects.SetTrimHover(side);
			uiObjects.UiStateChanged();
		}

		public override void EnterPressed()
		{
			if (keyboardMode)
				IsDone = true;
		}

		public override void EndOperation()
		{
			IsDone = false;
			keyboardMode = false;
			uiObjects.SetTrimHover(TrimDirection.None);
		}

		//------------------------ privates --------------------------
		private void performDefensiveProgrammingCheck()
		{
			if (uiObjects.CurrentVideoClip == null) // should never happen but who knows
				throw new HowTheFuckDidThisHappenException(
					proj,
					String.Format(VidkaErrorMessages.TrimDragCurVideoNull, side.ToString()));
		}
	}
}
