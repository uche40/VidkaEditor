using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Core.Model;

namespace Vidka.Core
{
	abstract class EditOperationAbstract
	{
		protected ISomeCommonEditorOperations iEditor;
		protected VidkaUiStateObjects uiObjects;
		protected ProjectDimensions dimdim;
		protected IVideoEditor editor;
		protected IVideoPlayer videoPlayer;
		protected VidkaProj proj;

		public EditOperationAbstract(
			ISomeCommonEditorOperations iEditor,
			VidkaUiStateObjects uiObjects,
			ProjectDimensions dimdim,
			IVideoEditor editor,
			IVideoPlayer videoPlayer)
		{
			this.iEditor = iEditor;
			this.uiObjects = uiObjects;
			this.dimdim = dimdim;
			this.editor = editor;
			this.videoPlayer = videoPlayer;
		}

		public void setProj(VidkaProj proj)
		{
			this.proj = proj;
		}

		public void SetVideoPlayer(IVideoPlayer videoPlayer)
		{
			this.videoPlayer = videoPlayer;
		}

		#region ---------------- abstract and vertual methods -------------------

		public abstract string Description { get; }

		public abstract void MouseDragStart(int x, int y, int w, int h);

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public abstract void MouseDragged(int x, int y, int deltaX, int deltaY, int w, int h);

		/// <param name="deltaX">relative to where the mouse was pressed down</param>
		/// <param name="deltaY">relative to where the mouse was pressed down</param>
		public abstract void MouseDragEnd(int x, int y, int deltaX, int deltaY, int w, int h);

		/// <summary>
		/// Override to accept keyboard adjustments
		/// See EditorLogic.LeftRightArrowKeys for call order and usage
		/// </summary>
		public virtual void ApplyFrameDelta(long deltaFrame) { }
	
		/// <summary>
		/// Override to accept keyboard adjustments
		/// See EditorLogic.LeftRightArrowKeys for call order and usage
		/// </summary>
		public virtual void KeyPressedArrow(Keys keyData) { }

		/// <summary>
		/// Override to accept keyboard adjustments
		/// See EditorLogic.LeftRightArrowKeys for call order and usage
		/// </summary>
		public virtual void KeyPressedOther(Keys keyData) { }

		/// <summary>
		/// For those ops who care
		/// </summary>
		public virtual void EnterPressed() { }

		/// <summary>
		/// Called when we begin a fresh new op
		/// </summary>
		public virtual void Init() {}

		/// <summary>
		/// At the start of op set this to false (you set it!), once this is true, this operation will be capitulated :)
		/// </summary>
		public bool IsDone { get; protected set; }

		/// <summary>
		/// Called from EditorLogic.MouseDragEnd (if DragEndIsTheEndOfThisOperation is true)
		/// or EditorLogic.EscapePressed, both cases, right before setting CurEditOp to null.
		/// (e.g. if the user decided to hit Escape and cancel this operation ...or to get out of keyboard adjustments)
		/// Override to clear any uiObjects in case a repaint is needed
		/// </summary>
		public virtual void EndOperation() { }

		/// <summary>
		/// Override and return false for those ops that support operation consisting of multiple mouse drags. AKA Blender3D style, baby :)
		/// This is used in EditorLogic.MouseDragStart to see if we need to begin a new operation (true)
		/// or continue with the prev operation (false), ure returning false so u dont wanna be cancelled!
		/// </summary>
		public virtual bool DoesNewMouseDragCancelMe { get { return true; } }

		/// <summary>
		/// For those ops that might have a different behavior based on whether it is click or control click
		/// </summary>
		public virtual void ControlPressed() { }

		/// <summary>
		/// For those ops that might have a different behavior based on whether it is click or shift click
		/// </summary>
		public virtual void ShiftPressed() { }

		#endregion

		#region ---------------- helpers -------------------

		/// <summary>
		/// Debug print to UI console
		/// </summary>
		protected void cxzxc(string text)
		{
			editor.AppendToConsole(VidkaConsoleLogLevel.Debug, text);
		}

		#endregion
	}
}
