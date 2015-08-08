using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vidka.Core
{
	public class UndoableAction
	{
		public Action Undo { get; set; }
		public Action Redo { get; set; }
		public Action PostAction { get; set; }
	}
}
