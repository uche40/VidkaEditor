using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Vidka.Core.Ops
{
	public class VidkaFileMapping_proj : VidkaFileMapping
	{
		private string projFilename;
		private string projDataFolder;

		public VidkaFileMapping_proj()
		{
			throw new NotImplementedException();
			projDataFolder = NewProjDataFolder;
		}

		public void setProjFilename(string filename) {
			throw new NotImplementedException();
			projFilename = filename;
			projDataFolder = String.Format(ProjDataFolderTemplate, Path.GetFileNameWithoutExtension(filename));
		}
		public override string AddGetMetaFilename(string filename) {
			throw new NotImplementedException();
			var justName = Path.GetFileNameWithoutExtension(filename);
			return Path.Combine(projDataFolder, justName + ".xml");
		}
		public override string AddGetThumbnailFilename(string filename) {
			throw new NotImplementedException();
			var justName = Path.GetFileNameWithoutExtension(filename);
			//return Path.Combine(projDataFolder, justName + "_thumbs.jpg");
			var dirname = Path.GetDirectoryName(filename);
			return Path.Combine(dirname, ".vidkadata", justName + "_thumbs.jpg");
		}

		public override string AddGetWaveFilenameDat(string filename) {
			throw new NotImplementedException();
			var justName = Path.GetFileNameWithoutExtension(filename);
			return Path.Combine(projDataFolder, justName + "_wave.dat");
		}
		public override string AddGetWaveFilenameJpg(string filename)
		{
			throw new NotImplementedException();
			var justName = Path.GetFileNameWithoutExtension(filename);
			return Path.Combine(projDataFolder, justName + "_wave.jpg");
		}
		public override void MakeSureDataFolderExists(string filename)
		{
			throw new NotImplementedException();
			if (!Directory.Exists(NewProjDataFolder))
				Directory.CreateDirectory(NewProjDataFolder);
		}

		//---------------------------------------------------
		public const string NewProjDataFolder = "NewProjDataFolder";
		public const string ProjDataFolderTemplate = "{0}_data";
		/// <summary>
		/// This is called from within logic class
		/// </summary>
		public static void MakeSureTmpFolderExists() {
			if (!Directory.Exists(NewProjDataFolder))
				Directory.CreateDirectory(NewProjDataFolder);
		}

	}
}
