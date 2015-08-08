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

	}
}
