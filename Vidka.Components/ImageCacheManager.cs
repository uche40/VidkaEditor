//#define OUTPUT_DEBUG_CXZXC

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Vidka.Core;
using Vidka.Core.Ops;

namespace Vidka.Components
{
	public class ImageCacheManager
	{
		#region events
		public delegate void ImagesReadyHandler();
		public event ImagesReadyHandler ImagesReady;
		#endregion

		private const int MAX_ThumbsBeforeCleanseUnused = 1; //200;

		private Dictionary<string, Bitmap> imgCache;
		private List<string> imgNotUsed;
		private Dictionary<string, List<int>> requests_thumb;
		private Dictionary<string, bool> requests_other;
		private TaskQueueInOtherThread taskThread;
		private Bitmap nullThumb;
		private Bitmap nullWave;
		private Rectangle rectThumb;
		private Rectangle rectCrop;
		private bool removeUnusedOnNextRepaint;

		public ImageCacheManager()
		{
			imgCache = new Dictionary<string, Bitmap>();
			imgNotUsed = new List<string>();
			requests_thumb = new Dictionary<string, List<int>>();
			requests_other = new Dictionary<string, bool>();
			taskThread = new TaskQueueInOtherThread();
			nullThumb = makeSolidColorBitmap(ThumbnailTest.ThumbW, ThumbnailTest.ThumbH, Color.Gray);
			nullWave = makeSolidColorBitmap(5, 1, Color.White);
			rectThumb = new Rectangle(0, 0, ThumbnailTest.ThumbW, ThumbnailTest.ThumbH);
			rectCrop = new Rectangle();
			removeUnusedOnNextRepaint = false;

			taskThread.CurrentQueueFinished += () => {
				cxzxc("triggering ImagesReady");
				if (ImagesReady != null)
					ImagesReady();
			};
		}

		public Bitmap getThumb(string filename, int index)
		{
			var url = getUrl_thumb(filename, index);
			if (imgCache.ContainsKey(url))
			{
				imgNotUsed.Remove(url);
				return imgCache[url];
			}
			// otherwise we need to queue it to the search
			if (requests_thumb.ContainsKey(filename))
				requests_thumb[filename].AddUnique(index);
			else
				requests_thumb.Add(filename, new List<int> { index });
			return nullThumb;
		}

		public Bitmap getWaveImg(string filename)
		{
			var url = filename;
			if (imgCache.ContainsKey(url))
			{
				imgNotUsed.Remove(url);
				return imgCache[url];
			}
			// otherwise we need to queue it to the search
			if (!requests_other.ContainsKey(url))
				requests_other.Add(url, true);
			return nullWave;
		}

		public void ___paintBegin()
		{
			cxzxc("___paintBegin");
			if (removeUnusedOnNextRepaint)
			{
				imgNotUsed.AddRange(imgCache.Keys);
				removeUnusedOnNextRepaint = false;
			}
		}

		public void ___paintEnd()
		{
			// remove images we are no longer using
			cxzxc("___paintEnd");
			cxzxc("to-remove:" + imgNotUsed.Select(x => debug_urlIndex(x)).StringJoin(","));
			foreach (var notUsed in imgNotUsed)
			{
				cxzxc("removing " + debug_url_thumb(notUsed));
				//if (!thumbsCache.ContainsKey(notUsed))
				//	continue;
				var img = imgCache[notUsed];
				imgCache.Remove(notUsed);
				img.Dispose();
			}
			imgNotUsed.Clear();
			cxzxc("to-add:" + requests_thumb.SelectMany(x => x.Value.Select(y => ""+y)).StringJoin(","));
			// start loading any new images which we require
			foreach (var filename in requests_thumb.Keys)
			{
				var indices = requests_thumb[filename];
				taskThread.QueueThisUpPlease(() =>
				{
					if (!File.Exists(filename))
						return;
					Bitmap thumbsAll = System.Drawing.Image.FromFile(filename, true) as Bitmap;
					var nRow = thumbsAll.Width / ThumbnailTest.ThumbW;
					var nCol = thumbsAll.Height / ThumbnailTest.ThumbH;
					foreach (var index in indices)
					{
						var url = getUrl_thumb(filename, index);
						if (imgCache.ContainsKey(url))
							continue;
						Bitmap target = new Bitmap(ThumbnailTest.ThumbW, ThumbnailTest.ThumbH);
						rectCrop.X = ThumbnailTest.ThumbW * (index % nCol);
						rectCrop.Y = ThumbnailTest.ThumbH * (index / nRow);
						rectCrop.Width = ThumbnailTest.ThumbW;
						rectCrop.Height = ThumbnailTest.ThumbH;
						using (Graphics g = Graphics.FromImage(target))
							g.DrawImage(thumbsAll, rectThumb, rectCrop, GraphicsUnit.Pixel);
						cxzxc("adding " + debug_url_thumb(url));
						imgCache.Add(url, target);
						if (imgCache.Count > MAX_ThumbsBeforeCleanseUnused)
							removeUnusedOnNextRepaint = true;
						// remove from requests
						//requests[filename].Remove(index);
						//if (requests[filename].Count == 0)
						//	requests.Remove(filename);
					}
				});
			}
			requests_thumb.Clear();
			foreach (var filename in requests_other.Keys)
			{
				var url = filename;
				taskThread.QueueThisUpPlease(() =>
				{
					if (imgCache.ContainsKey(url))
						return;
					if (!File.Exists(filename))
						return;
					Bitmap bmp = System.Drawing.Image.FromFile(filename, true) as Bitmap;
					cxzxc("adding " + debug_url_other(url));
					imgCache.Add(url, bmp);
					if (imgCache.Count > MAX_ThumbsBeforeCleanseUnused)
						removeUnusedOnNextRepaint = true;
				});
			}
			requests_other.Clear();
		}

		//#region ----------------------- concurrent ops --------------------------
		//private void addThumb() {}
		//#endregion

		#region ----------------------- helpers --------------------------

		private string getUrl_thumb(string filename, int index)
		{
			return filename + ":" + index;
		}

		private Bitmap makeSolidColorBitmap(int w, int h, Color color)
		{
			var img = new Bitmap(w, h, PixelFormat.Format32bppRgb);
			Graphics g = Graphics.FromImage(img);
			g.FillRectangle(new SolidBrush(color), 0, 0, w, h);
			return img;
		}

		private string debug_urlIndex(string url)
		{
			var splits = url.Split(':');
			var index = splits.LastOrDefault();
			return index;
		}

		private string debug_url_thumb(string url) {
			var index = debug_urlIndex(url);
			return Path.GetFileName(url.Replace(":" + index, "")) + ':' + index;
		}

		private string debug_url_other(string url) {
			return Path.GetFileName(url);
		}

		private void cxzxc(string message) {
#if OUTPUT_DEBUG_CXZXC
			Trace.WriteLine(message);
			if (VideoShitbox.ConsoleSingleton != null)
				VideoShitbox.ConsoleSingleton.AppendToConsole(VidkaConsoleLogLevel.Debug, message);
#endif
		}

		#endregion

	}
}
