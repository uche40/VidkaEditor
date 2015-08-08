using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidka.Core.Model;
using Vidka.Core.Properties;

namespace Vidka.Core.Ops
{
	public class MEncoderMaveVideoFile : OpBaseClass
	{
		private string args;

		public MEncoderMaveVideoFile(string filenameAvs, string filenameVideo)
		{
			//mencoder -ovc x264 -x264encopts preset=slow:tune=film:crf=22 -of lavf -o "{file-video}" -forcedsubsonly -oac mp3lame "{file-avs}" -vf scale,format=i420
			//mencoder -ovc xvid -xvidencopts bitrate=1500:me_quality=6:rc_reaction_delay_factor=100:rc_averaging_period=16:rc_buffer=100:quant_type=h263:min_iquant=1:max_iquant=31:min_pquant=1:max_pquant=31:min_bquant=1:max_bquant=31:max_key_interval=250:quant_type=h263:max_bframes=2:bquant_ratio=150:bquant_offset=100:bf_threshold=0:vhq=2:bvhq=1:curve_compression_high=0:curve_compression_low=0:overflow_control_strength=10:max_overflow_improvement=10:max_overflow_degradation=10:trellis:noqpel:nogmc:nocartoon:chroma_opt:chroma_me:nointerlacing:par=ext:par_width=1:par_height=1:closed_gop:nopacked:threads=8 -vf scale,format=i420 -forcedsubsonly -nosub -oac mp3lame -mc 0 "{file-avs}" -force-avi-aspect 1.81818 -of avi -o "{file-video}"
			
			args = Settings.Default.mencoderArguments
				.Replace("{file-video}", filenameVideo)
				.Replace("{file-avs}", filenameAvs);
		}

		public string FullCommand {
			get { return MencoderExecutable + " " + args; }
		}

		public void RunMEncoder() {
			Process process = new Process();
			process.StartInfo.FileName = MencoderExecutable;
			process.StartInfo.Arguments = args;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//process.StartInfo.CreateNoWindow = true;

			runProcessRememberError(process);
		}
	}
}
