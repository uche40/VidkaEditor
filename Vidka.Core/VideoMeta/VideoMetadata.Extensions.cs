using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.Core.VideoMeta
{

	/// <summary>
	/// a sensible wrapper class for stupid XML
	/// </summary>
	public class VideoMetadataUseful
	{
		private ffprobe xml;
		private ffprobeStream videoStream;
		private ffprobeStream audioStream;
		private ffprobeFormat format;

		public VideoMetadataUseful(ffprobe xml) {
			this.xml = xml;
			if (xml.streams != null) {
				videoStream = xml.streams.FirstOrDefault(x => x.codec_type == "video");
				audioStream = xml.streams.FirstOrDefault(x => x.codec_type == "audio");
			}
			format = (ffprobeFormat)xml.format;
			
		}

		//public double VideoDurationSec {
		//	get {
		//		return (videoStream == null) ? 0 : (double)videoStream.duration;
		//	}
		//}

		//public uint VideoDurationFrames
		//{
		//	get
		//	{
		//		return (videoStream == null) ? 0 : videoStream.duration_ts;
		//	}
		//}

		public double GetVideoDurationSec(double projFps) {
			if (videoStream == null)
				return 0;
			// hack to use 
			if (projFps == 30
				&& videoStream.r_frame_rate == "1000000/33333"
				&& videoStream.avg_frame_rate == "1000000/33333")
				return (double)videoStream.nb_read_frames / projFps;
			return (double)videoStream.duration;
		}

		//public int GetVideoDurationFrames(double projFps) {
		//	if (videoStream == null)
		//		return 0;
		//	// hack to use 
		//	if (projFps == 30
		//		&& videoStream.r_frame_rate == "1000000/33333"
		//		&& videoStream.avg_frame_rate == "1000000/33333")
		//		return (int)videoStream.nb_read_frames;
		//	return (int)((double)videoStream.duration * projFps);
		//}

		public double AudioDurationSec {
			get {
				return (audioStream == null) ? 0 : (double)audioStream.duration;
			}
		}

		public string Filename {
			get {
				return (format == null) ? "" : format.filename;
			}
		}
	}


	public partial class ffprobeStream
	{
		/// <summary>
		/// Not sure why we need this
		/// </summary>
		public double avg_frame_rate_Parsed {
			get {
				double fps = 0;
				if (!avg_frame_rate.Contains("/"))
					return double.TryParse(avg_frame_rate, out fps) ? fps : 0;
				double top = 0, bot = 0;
				var splits = avg_frame_rate.Split('/');
				double.TryParse(splits.FirstOrDefault() ?? "", out top);
				double.TryParse(splits.Skip(1).FirstOrDefault() ?? "", out top);
				if (bot == 0)
					return 0;
				return top / bot;
			}
		}
	}
	
}
