using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Vidka.Core.Ops {
	public class WaveformExtraction : OpBaseClass
	{
		const int SampleRate = 8000;
		const int ImgWidth = 1638;
		const int ImgHeight = 200;
		
		/// <summary>
		/// Generates 2 files: outFilename and outFilename.jpg
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="outFilename">should end with .dat; Since there are 2 the .jpg is added automatically to the "image" file</param>
		public WaveformExtraction(string filename, string outFilenameDat, string outFilenameJpg, bool deleteDat)
			: base()
		{
			MakeSureTmpFolderExists();
			RunFfMpegWaveform(filename, outFilenameDat);
			RenderTestWaveformJpeg(outFilenameDat, outFilenameJpg);
			if (deleteDat)
				File.Delete(outFilenameDat);
		}

		private void RunFfMpegWaveform(string filename, string outFilename)
		{
			Process process = new Process();
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = FfmpegExecutable;
			//process.StartInfo.Arguments = String.Format("-i \"{0}\" -y -ac 1 -filter:a aresample={1} -map 0:a -c:a pcm_s16le -f data {2}", filename, SampleRate, outFilename);
			process.StartInfo.Arguments = String.Format("-i \"{0}\" -y -ac 1 -filter:a aresample={1} -map 0:a -c:a pcm_s8 -f data {2}", filename, SampleRate, outFilename);
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			runProcessRememberError(process);			
		}

		// it is assumed length is much < than rawsamples2Byte.Length (see countThresh)
		private byte[] squishWaveform16le(byte[] rawsamples2Byte, int length)
		{
			var result = new byte[length];
			byte[] byte2 = new byte[2];
			int total = 0;
			int count = 0;
			int max = 0;
			int countThresh = (rawsamples2Byte.Length / 2) / length;
			int index = 0;
			for (int i = 0; i < rawsamples2Byte.Length / 2; i++)
			{
				byte2[0] = rawsamples2Byte[2 * i];
				byte2[1] = rawsamples2Byte[2 * i + 1];
				int sample = BitConverter.ToUInt16(byte2, 0);
				total += sample;
				count++;
				max = Math.Max(max, sample);
				if (count >= countThresh)
				{
					var avg = total / count;
					result[index] = (byte)(256 * avg / short.MaxValue);
					// reset!!!
					total = 0;
					count = 0;
					index++;
				}
				if (index >= result.Length)
					break;
			}
			if (count > 0 && index < result.Length)
			{
				var avg = total / count;
				result[index] = (byte)(256 * avg / short.MaxValue);
			}
			return result;
		}

		private byte[] squishWaveform(byte[] rawsamples2Byte, int length)
		{
			float[] result = new float[length];
			float maxResult = 0;
			int countThresh = (rawsamples2Byte.Length) / length;
			int ws = countThresh / 2;

			for (int i = 0; i < length; i++)
			{
				float sum = 0;
				var n = 0;
				
				for (int j = -ws; j < ws; j++)
				{
					var index = j + (i * countThresh + countThresh / 2);
					if (index < 0 || index >= rawsamples2Byte.Length)
						continue;
					sbyte sample = (sbyte)rawsamples2Byte[index];
					sum += Math.Abs((short)sample);
					n++;
				}
				result[i] = sum / n;
				maxResult = Math.Max(maxResult, result[i]);
			}

			//var result2 = new byte[length];
			//for (int i = 0; i < length; i++)
			//	result2[i] = (byte)Math.Floor(result[i] * 255 / maxResult);
			//return result2;

			var result3 = new byte[length];
			for (int i = 1; i < length - 1; i++)
			{
				var xi = Math.Floor(result[i-1] * 255 / maxResult);
				var xii = Math.Floor(result[i] * 255 / maxResult);
				var xiii = Math.Floor(result[i+1] * 255 / maxResult);
				result3[i] = (byte)((xi + 2*xii + xiii) / 4);
			}
			result3[0] = (byte)Math.Floor(result[0] * 255 / maxResult);
			result3[length-1] = (byte)Math.Floor(result[length-1] * 255 / maxResult);

			return result3;
		}

		//private byte[] squishWaveform1(byte[] rawsamples1Byte, int length) {
		//	var result = new byte[length];
		//	byte[] byte2 = new byte[2];
		//	int total = 0;
		//	int count = 0;
		//	int countThresh = rawsamples1Byte.Length / length;
		//	int index = 0;
		//	for (int i = 0; i < rawsamples1Byte.Length; i++) {
		//		int sample = rawsamples1Byte[i];
		//		total += sample;
		//		count++;
		//		if (count >= countThresh) {
		//			var avg = total / count;
		//			result[index] = (byte)(256 * avg / short.MaxValue);
		//			// reset!!!
		//			total = 0;
		//			count = 0;
		//			index++;
		//		}
		//		if (index >= result.Length)
		//			break;
		//	}
		//	if (count > 0 && index < result.Length) {
		//		var avg = total / count;
		//		result[index] = (byte)(256 * avg / short.MaxValue);
		//	}
		//	return result;
		//}

		private void RenderTestWaveformJpeg(string fileData, string outFile)
		{
			if (!File.Exists(fileData)) {
				ErrorMessage2 = String.Format("Waveform extraction error: failed to generate {0} and thus cannot generate {1}",
					Path.GetFileName(fileData),
					Path.GetFileName(outFile));
				return;
			}

			byte[] data = System.IO.File.ReadAllBytes(fileData);
			byte[] drawableData = squishWaveform(data, ImgWidth);

			Bitmap waveBmp = new Bitmap(drawableData.Length, ImgHeight);
			Graphics ggg = Graphics.FromImage(waveBmp);
			var pen = new Pen(Color.DarkGray);
			ggg.FillRectangle(new SolidBrush(Color.White), 0, 0, data.Length/2, ImgHeight);
			for (int i = 0; i < drawableData.Length; i++) {
				int sampleHeight = ImgHeight * drawableData[i] / 255;
				ggg.DrawLine(pen, i, ImgHeight / 2 - sampleHeight / 2, i, ImgHeight / 2 + sampleHeight/2);
			}
			ggg.Flush();

			waveBmp.Save(outFile, ImageFormat.Jpeg);
			waveBmp.Dispose();
		}


	}
}
