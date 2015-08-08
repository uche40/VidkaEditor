using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vidka.Core.Error;
using Vidka.Core.Model;

namespace Vidka.Core
{
	class EditOperationMoveVideo : EditOperationAbstract
	{
		private bool copyMode;
		private bool keyboardMode;
		private int clipX;
		private int clipW;
		private int oldIndex;
		private bool isStarted; // TODO: future plan to only initialize draggy when drag index is different... will avoid flickering... however there is no information about this index without the draggy

		public EditOperationMoveVideo(ISomeCommonEditorOperations iEditor,
			VidkaUiStateObjects uiObjects,
			ProjectDimensions dimdim,
			IVideoEditor editor,
			IVideoPlayer videoPlayer)
			: base(iEditor, uiObjects, dimdim, editor, videoPlayer)
		{
			copyMode = false;
			keyboardMode = false;
		}

		public override string Description { get {
			return copyMode ? "Copy clip" : "Move cip";
		} }

		public override bool TriggerBy_MouseDragStart(MouseButtons button, int x, int y)
		{
			return (button == MouseButtons.Left)
				&& (uiObjects.TimelineHover == ProjectDimensionsTimelineType.Main)
				&& (uiObjects.CurrentVideoClipHover != null)
				&& (uiObjects.TrimHover == TrimDirection.None);
		}

		public override void MouseDragStart(int x, int y, int w, int h)
		{
			IsDone = false;
			// I assume its not null, otherwise how do u have CurrentAudioClipTrimHover?
			var clip = uiObjects.CurrentVideoClipHover;
			oldIndex = proj.ClipsVideo.IndexOf(clip);
			clipX = dimdim.getScreenX1(clip);
			clipW = dimdim.convert_FrameToAbsX(clip.LengthFrameCalc);
			uiObjects.SetActiveVideo(clip, proj);
			uiObjects.SetDraggyCoordinates(
				mode: EditorDraggyMode.VideoTimeline,
				//mode: EditorDraggyMode.AudioTimeline, //tmp f-b-f drag test
				frameLength: clip.LengthFrameCalc,
				mouseX: x,
				mouseXOffset: x-clipX
			);
			if (Form.ModifierKeys == Keys.Control)
				copyMode = true;
			if (!copyMode)
				uiObjects.SetDraggyVideo(clip);
			uiObjects.SetHoverVideo(null);
			isStarted = false;
			keyboardMode = false;
		}

		public override void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			//if (isStarted) {} // TODO: consider this later
			uiObjects.SetDraggyCoordinates(mouseX: x);
		}

		public override void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			var clip = uiObjects.CurrentVideoClip;
			var clip_oldIndex = oldIndex;
			int draggyVideoShoveIndex = dimdim.GetVideoClipDraggyShoveIndex(uiObjects.Draggy);
			if (copyMode)
			{
				var newClip = copyMode ? clip.MakeCopy() : null;
				iEditor.AddUndableAction_andFireRedo(new UndoableAction()
				{
					Redo = () => {
						cxzxc("copy: " + clip_oldIndex + "->" + draggyVideoShoveIndex);
						proj.ClipsVideo.Insert(draggyVideoShoveIndex, newClip);
						uiObjects.SetActiveVideo(newClip, proj);
						long frameMarker = proj.GetVideoClipAbsFramePositionLeft(newClip);
						iEditor.SetFrameMarker_ShowFrameInPlayer(frameMarker);
					},
					Undo = () => {
						cxzxc("UNDO copy");
						proj.ClipsVideo.Remove(newClip);
						uiObjects.SetActiveVideo(null, proj);
					}
				});
			}
			else
			{
				if (draggyVideoShoveIndex > clip_oldIndex)
					draggyVideoShoveIndex--;
				if (draggyVideoShoveIndex != clip_oldIndex)
				{
					iEditor.AddUndableAction_andFireRedo(new UndoableAction()
					{
						Redo = () =>
						{
							cxzxc("move: " + clip_oldIndex + "->" + draggyVideoShoveIndex);
							proj.ClipsVideo.Remove(clip);
							proj.ClipsVideo.Insert(draggyVideoShoveIndex, clip);
						},
						Undo = () =>
						{
							cxzxc("UNDO move: " + draggyVideoShoveIndex + "->" + clip_oldIndex);
							proj.ClipsVideo.Remove(clip);
							proj.ClipsVideo.Insert(clip_oldIndex, clip);
						},
						PostAction = () =>
						{
							long frameMarker = proj.GetVideoClipAbsFramePositionLeft(clip);
							iEditor.SetFrameMarker_ShowFrameInPlayer(frameMarker);
						}
					});
				}
			}
			IsDone = true;
			copyMode = false;
			uiObjects.ClearDraggy();
			uiObjects.UiStateChanged();
		}

		public override void KeyPressedArrow(Keys keyData)
		{
			//TODO: kb mode???
		}

		public override void ControlPressed()
		{
			copyMode = true;
			uiObjects.SetDraggyVideo(null);
			cxzxc("Changed to copy mode");
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
					String.Format(VidkaErrorMessages.MoveDragCurVideoNull));
		}
	}
}
