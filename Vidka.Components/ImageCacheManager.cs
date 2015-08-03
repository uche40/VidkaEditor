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

		private Dictionary<string, Bitmap> thumbsCache;
		private Dictionary<string, Bitmap> wavesCache;
		private List<string> thumbsNotUsed;
		private Dictionary<string, List<int>> requests;
		private TaskQueueInOtherThread taskThread;
		private Bitmap nullThumb;
		private Bitmap nullWave;
		private Rectangle rectThumb;
		private Rectangle rectCrop;
		private bool removeUnusedOnNextRepaint;

		public ImageCacheManager()
		{
			thumbsCache = new Dictionary<string, Bitmap>();
			wavesCache = new Dictionary<string, Bitmap>();
			thumbsNotUsed = new List<string>();
			requests = new Dictionary<string, List<int>>();
			taskThread = new TaskQueueInOtherThread();
			nullThumb = makeSolidColorBitmap(ThumbnailTest.ThumbW, ThumbnailTest.ThumbH, Color.Gray);
			nullWave = makeSolidColorBitmap(5, 1, Color.White);
			rectThumb = new Rectangle(0, 0, ThumbnailTest.ThumbW, ThumbnailTest.ThumbH);
			rectCrop = new Rectangle();
			removeUnusedOnNextRepaint = false;
		}

		public Bitmap getThumb(string filename, int index)
		{
			var url = getUrl(filename, index);
			if (thumbsCache.ContainsKey(url))
			{
				thumbsNotUsed.Remove(url);
				return thumbsCache[url];
			}
			// otherwise we need to queue it to the search
			addRequest(filename, index);
			return nullThumb;
		}

		public Bitmap getWaveImg(string filename)
		{
			return nullWave;
		}

		public void ___paintBegin()
		{
			cxzxc("___paintBegin");
			if (removeUnusedOnNextRepaint)
			{
				thumbsNotUsed.AddRange(thumbsCache.Keys);
				removeUnusedOnNextRepaint = false;
				cxzxc("to-remove:" + thumbsNotUsed.Select(x => debug_urlIndex(x)).StringJoin(","));
			}
		}

		public void ___paintEnd()
		{
			// remove images we are no longer using
			foreach (var notUsed in thumbsNotUsed)
			{
				cxzxc("removing " + debug_url(notUsed));
				//if (!thumbsCache.ContainsKey(notUsed))
				//	continue;
				var img = thumbsCache[notUsed];
				thumbsCache.Remove(notUsed);
				img.Dispose();
			}
			cxzxc("to-add:" + requests.SelectMany(x => x.Value.Select(y => ""+y)).StringJoin(","));
			// start loading any new images which we require
			foreach (var filename in requests.Keys)
			{
				var indices = requests[filename];
				taskThread.QueueThisUpPlease(() =>
				{
					if (!File.Exists(filename))
						return;
					Bitmap thumbsAll = System.Drawing.Image.FromFile(filename, true) as Bitmap;
					var nRow = thumbsAll.Width / ThumbnailTest.ThumbW;
					var nCol = thumbsAll.Height / ThumbnailTest.ThumbH;
					foreach (var index in indices)
					{
						var url = getUrl(filename, index);
						Bitmap target = new Bitmap(ThumbnailTest.ThumbW, ThumbnailTest.ThumbH);
						rectCrop.X = ThumbnailTest.ThumbW * (index % nCol);
						rectCrop.Y = ThumbnailTest.ThumbH * (index / nRow);
						rectCrop.Width = ThumbnailTest.ThumbW;
						rectCrop.Height = ThumbnailTest.ThumbH;
						using (Graphics g = Graphics.FromImage(target))
							g.DrawImage(thumbsAll, rectThumb, rectCrop, GraphicsUnit.Pixel);
						cxzxc("adding " + debug_url(url));
						thumbsCache.Add(url, target);
						if (thumbsCache.Count > MAX_ThumbsBeforeCleanseUnused)
							removeUnusedOnNextRepaint = true;
					}
				});
			}
			if (requests.Count > 0)
			{
				requests.Clear();
				taskThread.QueueThisUpPlease(() =>
				{
					cxzxc("triggering ImagesReady");
					if (ImagesReady != null)
						ImagesReady();
				});
			}
		}

		#region ----------------------- helpers --------------------------

		private string getUrl(string filename, int index)
		{
			return filename + ":" + index;
		}

		private void addRequest(string filename, int index)
		{
			if (requests.ContainsKey(filename))
				requests[filename].AddUnique(index);
			else
				requests.Add(filename, new List<int> { index });
		}

		private Bitmap makeSolidColorBitmap(int w, int h, Color color)
		{
			var img = new Bitmap(w, h, PixelFormat.Format32bppRgb);
			Graphics g = Graphics.FromImage(img);
			g.FillRectangle(new SolidBrush(color), 0, 0, w, h);
			return img;
		}

		private void cxzxc(string message)
		{
			Trace.WriteLine(message);
			if (VideoShitbox.ConsoleSingleton == null)
				return;
			VideoShitbox.ConsoleSingleton.AppendToConsole(VidkaConsoleLogLevel.Debug, message);
		}

		private string debug_urlIndex(string url)
		{
			var splits = url.Split(':');
			var index = splits.LastOrDefault();
			return index;
		}

		private string debug_url(string url) {
			var index = debug_urlIndex(url);
			return Path.GetFileName(url.Replace(":" + index, "")) + ':' + index;
		}

		#endregion

	}
}
