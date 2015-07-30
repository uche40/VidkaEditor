using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Vidka.Core.Ops
{
	/// <summary>
	/// Will place a folder .vidkadata into every folder with media files and will generate
	/// all meta and thumbs in there
	/// </summary>
	public class VidkaFileMapping_resource : VidkaFileMapping
	{
		private const string DATA_FOLDER = ".vidkadata";

		public VidkaFileMapping_resource()
		{
		}

		public override string AddGetMetaFilename(string filename)
		{
			var justName = Path.GetFileNameWithoutExtension(filename);
			var dirname = Path.GetDirectoryName(filename);
			return Path.Combine(dirname, DATA_FOLDER, justName + ".xml");
		}
		public override string AddGetThumbnailFilename(string filename)
		{
			var justName = Path.GetFileNameWithoutExtension(filename);
			var dirname = Path.GetDirectoryName(filename);
			return Path.Combine(dirname, DATA_FOLDER, justName + "_thumbs.jpg");
		}

		public override string AddGetWaveFilenameDat(string filename)
		{
			var justName = Path.GetFileNameWithoutExtension(filename);
			var dirname = Path.GetDirectoryName(filename);
			return Path.Combine(dirname, DATA_FOLDER, justName + "_wave.dat");
		}
		public override string AddGetWaveFilenameJpg(string filename)
		{
			var justName = Path.GetFileNameWithoutExtension(filename);
			var dirname = Path.GetDirectoryName(filename);
			return Path.Combine(dirname, DATA_FOLDER, justName + "_wave.jpg");
		}
		public override void MakeSureDataFolderExists(string filename) {
			var dataFolder = Path.GetDirectoryName(filename);
			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);
		}

	}
}
