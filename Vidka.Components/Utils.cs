using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vidka.Components
{
	public static class Utils
	{
		public static bool IsLRShiftKey(this Keys key) {
			return key == (Keys.LButton | Keys.ShiftKey)
				|| key == (Keys.RButton | Keys.ShiftKey);
		}

		private static int[] PowersOf2ForTimeAxis = new [] { 1, 2, 5, 10, 20, 30, 60, 120, 300, 600, 1200, 1800, 3600, 7200 };
		internal static int GetClosestSnapToSecondsForTimeAxis(int seconds) {
			foreach (var snap in PowersOf2ForTimeAxis) {
				if (seconds <= snap)
					return snap;
			}
			return 1;
		}
	}
}
