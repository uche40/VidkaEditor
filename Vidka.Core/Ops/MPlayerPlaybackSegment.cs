using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidka.Core.Model;

namespace Vidka.Core.Ops
{
	public class MPlayerPlaybackSegment : OpBaseClass
	{
		private const string TMP_FILENAME = "MPlayerPlaybackSegment-temp.avs";

		public MPlayerPlaybackSegment(VidkaProj proj, long frameStart, long framesLength, bool leaveOpen)
		{
			var projCropped = CropProject(proj, frameStart, framesLength);
			VidkaIO.ExportToAvs(projCropped, TMP_FILENAME);
			RunMPlayer(TMP_FILENAME, proj, leaveOpen);
		}

		private VidkaProj CropProject(VidkaProj proj, long frameStart, long framesLength) {
			if (frameStart == 0)
				return proj;
			return proj.Crop(frameStart, framesLength, proj.Width / 4, proj.Height / 4);
		}

		private void RunMPlayer(string filenameAvs, VidkaProj proj, bool leaveOpen)
		{
			Process process = new Process();
			process.StartInfo.FileName = MplayerExecutable;
			process.StartInfo.Arguments = String.Format("{0} -vo gl -noautosub -geometry {1}x{2} {3}",
				filenameAvs,
				proj.Width,
				proj.Height,
				leaveOpen ? "-idle -fixed-vo -loop 1000" : "");
			process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//process.StartInfo.CreateNoWindow = true;

			runProcessRememberError(process);
		}
	}
}
