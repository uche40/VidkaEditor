using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Vidka.Core;
using Vidka.Core.Ops;

namespace Vidka.Components
{
	public partial class VidkaFastPreviewPlayer : UserControl
	{
		private Font fontDefault = SystemFonts.DefaultFont;
		private Brush brushDefault = new SolidBrush(Color.Black);

		private VidkaFileMapping fileMapping;
		private double offsetSeconds;
		private string filenameVideo;
		private Bitmap bmpThumbs;
		private Rectangle rectCrop, rectMe;
		private int bmpThumbs_nRow;
		private int bmpThumbs_nCol;

		public VidkaFastPreviewPlayer()
		{
			InitializeComponent();
			rectCrop = new Rectangle();
			rectMe = new Rectangle() { X = 0, Y = 0 };
		}

		private void VidkaFastPreviewPlayer_Load(object sender, EventArgs e)
		{
			this.DoubleBuffered = true;
		}

		public void SetFileMapping(VidkaFileMapping fileMapping)
		{
			this.fileMapping = fileMapping;
			Invalidate();
		}
		public void SetStillFrameNone()
		{
			disposeOfOldBmpThumbs();
			Invalidate();
		}
		public void SetStillFrame(string filename, double offsetSeconds)
		{
			if (this.filenameVideo != filename && fileMapping != null)
			{
				disposeOfOldBmpThumbs();
				this.filenameVideo = filename;
				var filenameThumbs = fileMapping.AddGetThumbnailFilename(filename);
				bmpThumbs = System.Drawing.Image.FromFile(filenameThumbs, true) as Bitmap;
				bmpThumbs_nRow = bmpThumbs.Width / ThumbnailTest.ThumbW;
				bmpThumbs_nCol = bmpThumbs.Height / ThumbnailTest.ThumbH;
			}
			this.offsetSeconds = offsetSeconds;
			Invalidate();
		}

		private void disposeOfOldBmpThumbs()
		{
			if (bmpThumbs == null)
				return;
			bmpThumbs.Dispose();
			bmpThumbs = null;
		}

		private void VidkaFastPreviewPlayer_Paint(object sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			if (bmpThumbs != null) {
				var imageIndex = (int)(offsetSeconds / ThumbnailTest.ThumbIntervalSec);
				rectMe.Width = Width;
				rectMe.Height = Height;
				rectCrop.X = ThumbnailTest.ThumbW * (imageIndex % bmpThumbs_nCol);
				rectCrop.Y = ThumbnailTest.ThumbH * (imageIndex / bmpThumbs_nRow);
				rectCrop.Width = ThumbnailTest.ThumbW;
				rectCrop.Height = ThumbnailTest.ThumbH;
				g.DrawImage(bmpThumbs, rectMe, rectCrop, GraphicsUnit.Pixel);
			}
			//g.DrawString(Path.GetFileName(filenameVideo) + ":" + imageIndex, fontDefault, brushDefault, 10, 20);
		}

	}
}
