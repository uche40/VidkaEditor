using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidka.Core.VideoMeta;

namespace Vidka.Core
{
	public enum VidkaConsoleLogLevel
	{
		Info = 1,
		Debug = 2,
		Error = 3,
	}

	public enum PreviewPlayerAbsoluteLocation
	{
		TopRight = 1,
		BottomRight = 2,
	}

	public interface IVideoEditor : IVidkaConsole
	{
		int Width { get; }
		int Height { get; }

		//void SetDraggy(VideoMetadataUseful meta);
		void PleaseRepaint();
		void UpdateCanvasWidth(int w);
		void UpdateCanvasHorizontalScroll(int scrollX);
		string OpenProjectSaveDialog();
		string OpenProjectOpenDialog();
		void AskToAbsoluteRepositionPreviewPlayer(PreviewPlayerAbsoluteLocation location);

		//TODO: change
		//void PlayTest(string filename);


	}
}
