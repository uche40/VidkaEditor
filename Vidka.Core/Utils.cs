using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Core.Model;
using Vidka.Core.Properties;

namespace Vidka.Core
{
	public static class Utils
	{
		#region =============== native extensions ===================

		public static void AddUnique<T>(this List<T> list, T obj) {
			if (list.Contains(obj))
				return;
			list.Add(obj);
		}

		public static string StringJoin(this IEnumerable<string> list, string separator) {
			return string.Join(separator, list);
		}

		public static string ToString_MinuteOrHour(this TimeSpan ts) {
			return ts.ToString((ts.TotalHours >= 1) ? @"hh\:mm\:ss" : @"mm\:ss");
		}

		#endregion

		#region =============== editing helpers ===================

		public static void SetFrameMarker_LeftOfVClip(this ISomeCommonEditorOperations iEditor, VidkaClipVideo vclip, VidkaProj proj)
		{
			long frameMarker = proj.GetVideoClipAbsFramePositionLeft(vclip);
			iEditor.SetFrameMarker_ShowFrameInPlayer(frameMarker);
		}

		public static void SetFrameMarker_RightOfVClipJustBefore(this ISomeCommonEditorOperations iEditor, VidkaClipVideo vclip, VidkaProj proj)
		{
			long frameMarker = proj.GetVideoClipAbsFramePositionLeft(vclip);
			var rightThreshFrames = proj.SecToFrame(Settings.Default.RightTrimMarkerOffsetSeconds);
			// if clip is longer than RightTrimMarkerOffsetSeconds, we can skip to end-RightTrimMarkerOffsetSeconds
			if (vclip.LengthFrameCalc > rightThreshFrames)
				frameMarker += vclip.LengthFrameCalc - rightThreshFrames;
			iEditor.SetFrameMarker_ShowFrameInPlayer(frameMarker);
		}

		#endregion
	}
}
