//#define ONE_LINE_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

namespace Vidka.Core.Ops
{
	public class ThumbnailTest : OpBaseClass
	{
		public const int ThumbW = 64;
		public const int ThumbH = 36;
		public const double ThumbIntervalSec = 0.5;

		public ThumbnailTest(string filename, string outFilename)
			: base()
		{
			MakeSureTmpFolderExists();
			RunFfMpegThumbnail(filename);
			StitchIntoOneBmp(TmpFolder, outFilename);
		}

		private void StitchIntoOneBmp(string TmpFolder, string outFilename)
		{
			var imgsFiles = Directory.GetFiles(TmpFolder)
				.Where(fff => fff.Contains("out") && fff.Contains(".jpg"))
				.ToArray();
			if (imgsFiles.Length == 0) {
				ErrorMessage2 = "No thumbnails found in tmp directory";			
				return;
			}

			var nCol = (int)Math.Ceiling(Math.Sqrt(imgsFiles.Length));
			var nRow = (int)Math.Ceiling((double)imgsFiles.Length / nCol);

#if ONE_LINE_TEST
			// tmp 1 line test
			nCol = imgsFiles.Length;
			nRow = 1;
#endif

			Bitmap allThumbs = new Bitmap(nCol*ThumbW, nRow*ThumbH);
			Graphics ggg = Graphics.FromImage(allThumbs);
			int i = 1;
			for (int r = 0; r < nRow; r++) {
				for (int c = 0; c < nCol; c++) {
					Bitmap bmp = (Bitmap)Image.FromFile(String.Format("{0}/out{1}.jpg", TmpFolder, i));
					ggg.DrawImage(bmp, ThumbW * c, ThumbH * r);
					bmp.Dispose();
					i++;
					if (i > imgsFiles.Length)
						break;
				}
				if (i > imgsFiles.Length)
					break;
			}
			ggg.Flush();

			// save all to one jpg file
			allThumbs.Save(outFilename, ImageFormat.Jpeg);
			allThumbs.Dispose();


			// delete all tmp images
			foreach (var fff in imgsFiles)
				File.Delete(fff);
		}

		private void RunFfMpegThumbnail(string filename)
		{
			Process process = new Process();
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = FfmpegExecutable;
			// make img 64x36 every 0.5 sec
			// source: https://trac.ffmpeg.org/wiki/Create%20a%20thumbnail%20image%20every%20X%20seconds%20of%20the%20video
			process.StartInfo.Arguments = String.Format("-i \"{0}\" -y -f image2 -vf \"scale={2},fps=fps=1/{3}\" {1}/out%d.jpg", filename, TmpFolder, ThumbW + ":" + ThumbH, ThumbIntervalSec);
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;

			runProcessRememberError(process);
		}
	}
}
