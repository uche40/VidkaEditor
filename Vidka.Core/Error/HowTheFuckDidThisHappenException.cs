using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Vidka.Core.Model;
using Vidka.Core.Properties;

namespace Vidka.Core.Error
{
	/// <summary>
	/// this exception when thrown, saves a backup file, also logs to a file.
	/// </summary>
	class HowTheFuckDidThisHappenException : Exception
	{
		public HowTheFuckDidThisHappenException(VidkaProj proj, string message)
			: base(message
			+ " IMPORTANT: A backup of the project has been saved to "
			+ Settings.Default.ProjBackupFile
			+ " relative to editor executable or something.")
		{
			// write to file
			XmlSerializer x = new XmlSerializer(typeof(VidkaProj));
			var fs = new FileStream(Settings.Default.ProjBackupFile, FileMode.Create);
			x.Serialize(fs, proj);
			fs.Close();

			// write to error log
			VidkaErrorLog.Logger.Log(message);
		}
	}
}
