using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vidka.Core
{
	interface ISomeCommonEditorOperations
	{
		void ShowFrameInVideoPlayer(long frame);

		/// <summary>
		/// Not used now.
		/// At first it was to notify user of KB mode, but then I decided to remove it,
		/// But then I thought it might be useful for multi-drag operations
		/// </summary>
		void LockMouseMovements();
	}
}
