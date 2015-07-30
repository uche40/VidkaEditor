using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidka.Core.VideoMeta;

namespace Vidka.Core
{
	public interface IVideoPlayer
	{
		//TODO: test
		void PlayFileTest(string filename);
		// ... still frame
		void SetStillFrameNone();
		void SetStillFrame(string filename, double offsetSeconds);
		// ... playback
		void PlayVideoClip(string filename, double clipSecStart, double clipSecEnd);
		void StopWhateverYouArePlaying();
		// ... misc
		double GetPositionSec();
	}
}
