using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidka.Core.Ops;

namespace ConsoleTests {
	class Program {
		static void Main(string[] args)
		{
			const string vvvPath = @"C:\Users\Mikhail\Desktop\vvvvvvvvvv\";
			const string outPath = @"..\..\..\FormTest\bin\Debug\NewProjDataFolder\";

			//20140830_180449.mp4 - bells
			//20141206_170817.mp4 - ass is wet
			//IMGP3367.AVI - bridge
			//new ThumbnailTest(vvvPath + "20141206_170817.mp4", outPath + "testThumbs2.jpg");
			
			// TODO constructor changed... see usage in FormTest
			//new WaveformExtraction(vvvPath + "20141206_170817.mp4", outPath + "wavetest2.dat");

			//var extract = new MetadataExtraction(vvvPath + "20141206_170817.mp4", outPath + "20141206_170817.xml");
			//var test = extract.test(outPath + "20141206_170817.xml");
			//var durr = ((Vidka.Core.VideoMeta.ffprobeStreams)test.Items[0]).stream[0].ParsedDuration;
			//Console.ReadKey();

			//new FFVideoFrameByFrameTest();
		}
	}
}
