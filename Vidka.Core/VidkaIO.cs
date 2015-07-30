using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vidka.Core.Model;

namespace Vidka.Core
{
	public class VidkaIO
	{

		internal VidkaProj LoadProjFromFile(string filename)
		{
			XmlSerializer x = new XmlSerializer(typeof(VidkaProj));
			var fs = new FileStream(filename, FileMode.Open);
			var proj = (VidkaProj)x.Deserialize(fs);
			fs.Close();
			return proj;
		}

		internal void SaveProjToFile(VidkaProj Proj, string filename)
		{
			XmlSerializer x = new XmlSerializer(typeof(VidkaProj));
			var fs = new FileStream(filename, FileMode.Create);
			x.Serialize(fs, Proj);
			fs.Close();
		}

		internal static void ExportToAvs(VidkaProj Proj, string fileOut)
		{
			var sbClips = new StringBuilder();
			var sbClipStats = new StringBuilder();
			var lastClip = Proj.ClipsVideo.LastOrDefault();
			foreach (var clip in Proj.ClipsVideo) {
				sbClips.Append(String.Format("\tNeutralClip(\"{0}\", {1}, {2}){3}\n",
					clip.FileName, clip.FrameStart, clip.FrameEnd,
					(clip != lastClip) ? ", \\" : " \\"));
				sbClipStats.Append(String.Format("collectpixeltypestat(\"{0}\", {1})\n",
					clip.FileName, clip.LengthFrameCalc));
			}

			// TODO: calc abs path based on exe
			var templateStr = File.ReadAllText("App_data/template.avs");
			// TODO: inject project properties
			var outputStr = templateStr
				.Replace("{video-clips}", sbClips.ToString())
				.Replace("{collectpixeltypestat-videos}", sbClipStats.ToString())
			;

			File.WriteAllText(fileOut, outputStr);
		}
	}
}
