using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vidka.Core.Error;
using Vidka.Core.Model;

namespace Vidka.Core
{
	class EditOperationSelectOriginalSegment : EditOperationAbstract
	{
		private bool keyboardMode;
		private long origFrame1;
		private long origFrame2;
		private long prevStart;
		private long prevEnd;
		private bool isStarted;

		public EditOperationSelectOriginalSegment(ISomeCommonEditorOperations iEditor,
			VidkaUiStateObjects uiObjects,
			ProjectDimensions dimdim,
			IVideoEditor editor,
			IVideoPlayer videoPlayer)
			: base(iEditor, uiObjects, dimdim, editor, videoPlayer)
		{
			keyboardMode = false;
			isStarted = false;
		}

		public override string Description { get {
			return "Select Segment";
		} }

		public override bool TriggerBy_MouseDragStart(MouseButtons button, int x, int y)
		{
			return (button == MouseButtons.Right)
				&& (uiObjects.TimelineHover == ProjectDimensionsTimelineType.Original)
				&& (uiObjects.CurrentVideoClip != null);
		}

		public override void MouseDragStart(int x, int y, int w, int h)
		{
			IsDone = false;
			keyboardMode = false;
			var clip = uiObjects.CurrentVideoClip;
			prevStart = clip.FrameStart;
			prevEnd = clip.FrameEnd;
			origFrame1 = origFrame2 = ComputeOrigFrameFromMouseX(x, w);
			uiObjects.SetActiveVideo(clip, proj);
		}

		public override void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			origFrame2 = ComputeOrigFrameFromMouseX(x, w);
			var clip = uiObjects.CurrentVideoClip;
			if (origFrame1 != origFrame2) {
				clip.FrameStart = Math.Min(origFrame1, origFrame2);
				clip.FrameEnd = Math.Max(origFrame1, origFrame2);
				uiObjects.UiStateChanged();
			}
		}

		private long ComputeOrigFrameFromMouseX(int x, int w)
		{
			var clip = uiObjects.CurrentVideoClip;
			return (long)(clip.FileLengthFrames * x / w);
		}

		public override void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			if (origFrame1 != origFrame2)
			{
				var clip = uiObjects.CurrentVideoClip;
				var newStart = Math.Min(origFrame1, origFrame2);
				var newEnd = Math.Max(origFrame1, origFrame2);
				var oldStart = prevStart;
				var oldEnd = prevEnd;
				iEditor.AddUndableAction_andFireRedo(new UndoableAction()
				{
					Redo = () =>
					{
						cxzxc("Select region");
						clip.FrameStart = newStart;
						clip.FrameEnd = newEnd;
					},
					Undo = () =>
					{
						cxzxc("UNDO Select region");
						clip.FrameStart = oldStart;
						clip.FrameEnd = oldEnd;
					},
					PostAction = () =>
					{
					}
				});
				uiObjects.UiStateChanged();
			}
			// TODO: change here for KB mode
			IsDone = true;
			keyboardMode = false;
			uiObjects.ClearDraggy();
			uiObjects.UiStateChanged();
		}

		public override void KeyPressedArrow(Keys keyData)
		{
			//TODO: kb mode???
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

	}
}
