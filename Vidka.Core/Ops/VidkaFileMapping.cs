using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Vidka.Core.Ops
{
	public abstract class VidkaFileMapping
	{
		public VidkaFileMapping() {
		}

		public abstract string AddGetMetaFilename(string filename);
		public abstract string AddGetThumbnailFilename(string filename);
		public abstract string AddGetWaveFilenameDat(string filename);
		public abstract string AddGetWaveFilenameJpg(string filename);
		public abstract void MakeSureDataFolderExists(string filename);

	}
}
