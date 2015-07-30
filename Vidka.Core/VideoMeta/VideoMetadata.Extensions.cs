using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.Core.VideoMeta {
	public partial class ffprobeFormat {
		public double ParsedDuration {
			get {
				double ddd = 0;
				double.TryParse(this.duration, out ddd);
				return ddd;
			}
		}
	}

	public partial class ffprobeStreamsStream {
		public double ParsedDuration {
			get {
				double ddd = 0;
				double.TryParse(this.duration, out ddd);
				return ddd;
			}
		}

		public int ParsedDurationFrames {
			get
			{
				int ddd = 0;
				int.TryParse(this.duration_ts, out ddd);
				return ddd;
			}
		}
	}

	/// <summary>
	/// a sensible wrapper class for stupid XML
	/// </summary>
	public class VideoMetadataUseful
	{
		private ffprobe xml;
		private ffprobeStreamsStream videoStream;
		private ffprobeStreamsStream audioStream;
		private ffprobeFormat format;

		public VideoMetadataUseful(ffprobe xml) {
			this.xml = xml;
			var streams = (ffprobeStreams)xml.Items[0];
			if (streams != null) {
				videoStream = streams.stream.FirstOrDefault(x => x.codec_type == "video");
				audioStream = streams.stream.FirstOrDefault(x => x.codec_type == "audio");
			}
			format = (ffprobeFormat)xml.Items[1];
			
		}

		public double VideoDurationSec {
			get {
				return (videoStream == null) ? 0 : videoStream.ParsedDuration;
			}
		}

		public int VideoDurationFrames
		{
			get
			{
				return (videoStream == null) ? 0 : videoStream.ParsedDurationFrames;
			}
		}

		public double AudioDurationSec {
			get {
				return (audioStream == null) ? 0 : audioStream.ParsedDuration;
			}
		}

		public string Filename {
			get {
				return (format == null) ? "" : format.filename;
			}
		}
	}

	
}
