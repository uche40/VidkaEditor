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

		public EditOperationMoveVideo(ISomeCommonEditorOperations iEditor,
			VidkaUiStateObjects uiObjects,
			ProjectDimensions dimdim,
			IVideoEditor editor,
			IVideoPlayer videoPlayer,
			bool copyMode = false)
			: base(iEditor, uiObjects, dimdim, editor, videoPlayer)
		{
			this.copyMode = copyMode;
			keyboardMode = false;
		}

		public override string Description { get {
			return copyMode ? "Copy clip" : "Move cip";
		} }

		public override void MouseDragStart(int x, int y, int w, int h)
		{
			IsDone = false;
			// I assume its not null, otherwise how do u have CurrentAudioClipTrimHover?
			var clip = uiObjects.CurrentVideoClipHover;
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
			if (!copyMode)
				uiObjects.SetDraggyVideo(clip);
			uiObjects.SetHoverVideo(null);

			keyboardMode = false;
		}

		public override void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			uiObjects.SetDraggyCoordinates(mouseX: x);
		}

		public override void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h)
		{
			performDefensiveProgrammingCheck();
			var clip = uiObjects.CurrentVideoClip;
			int draggyVideoShoveIndex = dimdim.GetVideoClipDraggyShoveIndex(uiObjects.Draggy);
			int curIndex = proj.ClipsVideo.IndexOf(clip);
			if (copyMode)
			{
				cxzxc("copy: " + curIndex + "->" + draggyVideoShoveIndex);
				var newClip = clip.MakeCopy();
				proj.ClipsVideo.Insert(draggyVideoShoveIndex, newClip);
			}
			else
			{
				if (curIndex != draggyVideoShoveIndex) {
					//TODO: kb mode???
					cxzxc("swap: " + curIndex + "->" + draggyVideoShoveIndex);
					proj.SwitchVideoClips(curIndex, draggyVideoShoveIndex);
				}
			}
			IsDone = true;
			uiObjects.ClearDraggy();
			uiObjects.SomethingDidChangeITellYou();
		}

		public override void KeyPressedArrow(Keys keyData)
		{

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
