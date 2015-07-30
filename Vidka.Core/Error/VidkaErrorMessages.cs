using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vidka.Core.Error
{
	public static class VidkaErrorMessages
	{
		public const string TrimDragCurVideoNull = @"
EditOperationTrimVideo.MouseDragged, for some stupid reason CurrentVideoClip is null.
I mean... WTF? U can only hit this part of the code if u have been hovering over a video,
then the hover video is assigned to CurrentVideoClip and then its not null.
I personally think writing this error message is a waste of time because it will NEVER happen!
Trim-direction: {0}";
		public const string MoveDragCurVideoNull = @"
EditOperationMoveVideo.MouseDragged, for some stupid reason CurrentVideoClip is null.
I mean... WTF? U can only hit this part of the code if u have been hovering over a video,
then the hover video is assigned to CurrentVideoClip and then its not null.
I personally think writing this error message is a waste of time because it will NEVER happen!";
	}
}
