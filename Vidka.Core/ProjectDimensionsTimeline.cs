using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidka.Core.Model;

namespace Vidka.Core
{
	/// <summary>
	/// in all variable names 100 means the value is "out of 100", 100 being egde of the panel
	/// </summary>
	internal class ProjectDimensionsTimeline
	{
		public ProjectDimensionsTimeline(int y1100, int y2100, ProjectDimensionsTimelineType type) {
			this.y1100 = y1100;
			this.y2100 = y2100;
			this.Type = type;
		}

		public int y1100 { get; private set; }
		public int y2100 { get; private set; }
		public int yHalfway { get; set; }
		public ProjectDimensionsTimelineType Type { get; private set; }

		public bool testCollision(int y100) {
			return y100 >= y1100 && y100 < y2100;
		}

	}

	public enum ProjectDimensionsTimelineType {
		None = 0,
		Original = 1,
		Main = 2,
		Audios = 3,
	}
}
