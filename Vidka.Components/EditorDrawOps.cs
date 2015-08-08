using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vidka.Core;
using Vidka.Core.Model;
using Vidka.Core.Ops;

namespace Vidka.Components
{
	/// <summary>
	/// To allow to choose the different colors and styles of highlight
	/// </summary>
	public enum OutlineClipType {
		Hover = 1,
		Active = 2,
	}


	public class EditorDrawOps
	{
		// constants
		private const int THUMB_MARGIN = 20;
		private const int THUMB_MARGIN_Y = 50;
		private Pen penDefault = new Pen(Color.Black, 1); // new Pen(Color.FromArgb(255, 30, 30, 30), 1);
		private Pen penBorder = new Pen(Color.Black, 1);
		private Pen penMarker = new Pen(Color.Black, 2);
		private Pen penBorderDrag = new Pen(Color.Blue, 5);
		private Pen penHover = new Pen(Color.Blue, 4);
		private Pen penActiveClip = new Pen(Color.LightBlue, 6);
		private Pen penActiveBoundary = new Pen(Color.Red, 6);
		private Pen penActiveBoundaryPrev = new Pen(Color.Purple, 6);
		private Pen penGray = new Pen(Color.Gray, 1);
		private Brush brushDefault = new SolidBrush(Color.Black);
		private Brush brushLightGray = new SolidBrush(Color.FromArgb(unchecked((int)0xFFfbfbfb)));
		private Brush brushLightGray2 = new SolidBrush(Color.FromArgb(unchecked((int)0xFFf5f5f5)));
		private Brush brushLightGray3 = new SolidBrush(Color.FromArgb(unchecked((int)0xFFeeeeee)));
		private Brush brushActive = new SolidBrush(Color.LightBlue);
		private Brush brushWhite = new SolidBrush(Color.White);
		private Brush brushHazy = new SolidBrush(Color.FromArgb(200, 230, 230, 230));
		private Font fontDefault = SystemFonts.DefaultFont;

		// helpers
		private ImageCacheManager imgCache;
		private Rectangle destRect, srcRect;

		public EditorDrawOps(ImageCacheManager imgCache) {
			// init
			this.imgCache = imgCache;
			destRect = new Rectangle();
			srcRect = new Rectangle();
		}

		public void PrepareCanvas(Graphics g, ProjectDimensions dimdim, int w, int h, ProjectDimensionsTimelineType hover)
		{
			int yMain1 = dimdim.getY_main1(h);
			int yMain2 = dimdim.getY_main2(h);
			int yMainHalf = dimdim.getY_main_half(h);
			int yAudio1 = dimdim.getY_audio1(h);
			int yAudio2 = dimdim.getY_audio2(h);
			g.FillRectangle((hover == ProjectDimensionsTimelineType.Main) ? brushLightGray2 : brushLightGray, 0, yMain1, w, yMainHalf - yMain1);
			g.FillRectangle((hover == ProjectDimensionsTimelineType.Main) ? brushLightGray3 : brushLightGray2, 0, yMainHalf, w, yMain2 - yMainHalf);
			g.FillRectangle((hover == ProjectDimensionsTimelineType.Audios) ? brushLightGray3 : brushLightGray2, 0, yAudio1, w, yAudio2 - yAudio1);
		}

		public void DrawProjectVideoTimeline(
			Graphics g,
			int Width,
			int Height,
			VidkaProj proj,
			VidkaFileMapping projMapping,
			ProjectDimensions dimdim,
			VidkaClipVideo currentVideoClip,
			EditorDraggy draggy)
		{
			// draw video events
			long curFrame = 0;

			int y1 = dimdim.getY_main1(Height);
			int y2 = dimdim.getY_main2(Height);
			int yaudio = dimdim.getY_main_half(Height);
			int cliph = y2 - y1; // clip height (video and audio)
			int clipvh = yaudio - y1; // clip (only video) height (just the video part, no audio!)
			int index = 0;
			int draggyVideoShoveIndex = dimdim.GetVideoClipDraggyShoveIndex(draggy);

			foreach (var vclip in proj.ClipsVideo)
			{
				if (dimdim.isEvenOnTheScreen(curFrame, curFrame + vclip.LengthFrameCalc, Width))
				{
					if (draggy.Mode == EditorDraggyMode.VideoTimeline && draggyVideoShoveIndex == index)
					{
						drawDraggyVideo(g, curFrame, y1, cliph, clipvh, draggy, dimdim);
						curFrame += draggy.FrameLength;
					}

					if (draggy.VideoClip != vclip)
					{
						drawVideoClip(g, vclip,
							curFrame, y1, cliph, clipvh,
							(vclip == currentVideoClip) ? brushActive : brushWhite,
							proj, projMapping, dimdim
							);
					}
				}

				index++;
				if (draggy.VideoClip != vclip)
					curFrame += vclip.LengthFrameCalc;
			}

			if (draggy.Mode == EditorDraggyMode.VideoTimeline && draggyVideoShoveIndex == index)
				drawDraggyVideo(g, curFrame, y1, cliph, clipvh, draggy, dimdim);
		}

		private void drawVideoClip(Graphics g,
			VidkaClipVideo vclip,
			long curFrame, int y1, int cliph, int clipvh,
			Brush brushClip,
			VidkaProj proj,
			VidkaFileMapping projMapping,
			ProjectDimensions dimdim)
		{
			int x1 = dimdim.convert_Frame2ScreenX(curFrame);
			int x2 = dimdim.convert_Frame2ScreenX(curFrame + vclip.LengthFrameCalc);
			int clipw = x2 - x1;

			// active video clip deserves a special outline, fill white otherwise to hide gray background
			g.FillRectangle(brushClip, x1, y1, clipw, clipvh);
			DrawClipBitmaps(
				g: g,
				proj: proj,
				projMapping: projMapping,
				vclip: vclip,
				x1: x1,
				y1: y1,
				clipw: clipw,
				clipvh: clipvh,
				secStart: proj.FrameToSec(vclip.FrameStart),
				len: proj.FrameToSec(vclip.LengthFrameCalc));
			DrawWaveform(g, proj, projMapping, vclip, x1, y1 + clipvh, clipw, cliph - clipvh,
				proj.FrameToSec(vclip.FrameStart), proj.FrameToSec(vclip.FrameEnd));
			// waveform separator
			g.DrawLine(penGray, x1, y1 + clipvh, x2, y1 + clipvh);
			// outline rect
			g.DrawRectangle(penDefault, x1, y1, clipw, cliph);
			// still analyzing...
			if (vclip.IsNotYetAnalyzed)
				g.DrawString("Still analyzing...", fontDefault, brushDefault, x1+5, y1+5);
		}

		private void drawDraggyVideo(Graphics g, long curFrame, int y1, int cliph, int clipvh, EditorDraggy draggy, ProjectDimensions dimdim)
		{
			var draggyX = dimdim.convert_Frame2ScreenX(curFrame);
			var draggyW = dimdim.convert_FrameToAbsX(draggy.FrameLength); // hacky, i know
			if (draggy.VideoClip != null)
			{
				g.FillRectangle(brushWhite, draggyX, y1, draggyW, cliph);
				g.FillRectangle(brushActive, draggyX, y1, draggyW, clipvh);
			}
			g.DrawRectangle(penBorderDrag, draggyX, y1, draggyW, cliph);
			g.DrawString(draggy.Text, fontDefault, brushDefault, draggyX + 5, y1 + 5);
			
			// debug rect
			//g.DrawRectangle(penDefault, draggy.MouseX-draggy.MouseXOffset, y1-2, draggyW, cliph+5);
		}

		public void DrawProjectAudioTimeline(
			Graphics g,
			int Width,
			int Height,
			VidkaProj proj,
			ProjectDimensions dimdim,
			VidkaClipAudio currentAudioClip,
			EditorDraggy draggy)
		{
			// draw video events
			long curFrame = 0;

			int y1 = dimdim.getY_audio1(Height);
			int y2 = dimdim.getY_audio2(Height);
			int cliph = y2 - y1;

			foreach (var aclip in proj.ClipsAudio)
			{
				if (dimdim.isEvenOnTheScreen(curFrame, curFrame + aclip.LengthFrameCalc, Width))
				{
					int x1 = dimdim.convert_Frame2ScreenX(curFrame);
					int x2 = dimdim.convert_Frame2ScreenX(curFrame + aclip.LengthFrameCalc);
					int clipw = x2 - x1;

					// active video clip deserves a special outline
					//if (aclip == currentAudioClip)
					//	g.FillRectangle(brushActive, x1, y1, clipw, clipvh);
					//else
					//	g.FillRectangle(brushWhite, x1, y1, clipw, clipvh);

					throw new NotImplementedException("DrawWaveform that takes Audio clip!!!");
					//DrawWaveform(g, proj, aclip, x1, y1, clipw, cliph,
					//	proj.FrameToSec(aclip.FrameStart), proj.FrameToSec(aclip.FrameEnd));


					// outline rect
					g.DrawRectangle(penDefault, x1, y1, clipw, cliph);
				}

				curFrame += aclip.LengthFrameCalc;
			}
			if (draggy.Mode == EditorDraggyMode.AudioTimeline)
			{
				var draggyX = draggy.MouseX - draggy.MouseXOffset;
				var draggyW = dimdim.convert_FrameToAbsX(draggy.FrameLength); // hacky, i know
				g.DrawRectangle(penBorderDrag, draggyX, y1, draggyW, cliph);
			}
		}

		/// <param name="timeSec">needs to be in seconds to figure out which thumb</param>
		//private void DrawVideoThumbnail(Graphics g, Bitmap bmpAll, double timeSec, int xCenter, int yCenter, int preferredWidth, int maxWidth)
		//{
		//	var imageIndex = (int)(timeSec / ThumbnailTest.ThumbIntervalSec);
		//	var nRow = bmpAll.Width / ThumbnailTest.ThumbW;
		//	var nCol = bmpAll.Height / ThumbnailTest.ThumbH;
		//	srcRect.X = ThumbnailTest.ThumbW * (imageIndex % nCol);
		//	srcRect.Y = ThumbnailTest.ThumbH * (imageIndex / nRow);
		//	srcRect.Width = ThumbnailTest.ThumbW;
		//	srcRect.Height = ThumbnailTest.ThumbH;
		//	destRect.Width = preferredWidth;
		//	destRect.Height = preferredWidth * ThumbnailTest.ThumbH / ThumbnailTest.ThumbW;
		//	destRect.X = xCenter - destRect.Width / 2;
		//	destRect.Y = yCenter - destRect.Height / 2;
		//	g.DrawImage(bmpAll, destRect: destRect, srcRect: srcRect, srcUnit: GraphicsUnit.Pixel);
		//}
		private void DrawVideoThumbnail(Graphics g, string filenameAll, int index, int xCenter, int yCenter, int preferredWidth, int maxWidth)
		{
			var bmpThumb = imgCache.getThumb(filenameAll, index);
			srcRect.X = 0;
			srcRect.Y = 0;
			srcRect.Width = ThumbnailTest.ThumbW;
			srcRect.Height = ThumbnailTest.ThumbH;
			destRect.Width = preferredWidth;
			destRect.Height = preferredWidth * ThumbnailTest.ThumbH / ThumbnailTest.ThumbW;
			destRect.X = xCenter - destRect.Width / 2;
			destRect.Y = yCenter - destRect.Height / 2;
			g.DrawImage(bmpThumb, destRect: destRect, srcRect: srcRect, srcUnit: GraphicsUnit.Pixel);
		}

		public void DrawBorder(Graphics g, int Width, int Height)
		{
			g.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
		}

		public void OutlineClipVideoHover(
			Graphics g,
			VidkaClipVideo vclip,
			ProjectDimensions dimdim,
			int Height,
			TrimDirection trimDirection,
			int trimBracketLength,
			long framesActiveMouseTrim)
		{
			int y1 = dimdim.getY_main1(Height);
			int y2 = dimdim.getY_main2(Height);
			//int yaudio = dimdim.getY_main_half(Height);
			int x1 = dimdim.getScreenX1(vclip);
			int clipW = dimdim.convert_FrameToAbsX(vclip.LengthFrameCalc); // hacky, I know
			g.DrawRectangle(penHover, x1, y1, clipW, y2 - y1);
			if (trimDirection == TrimDirection.Left)
				drawTrimBracket(g, x1, y1, y2, TrimDirection.Left, trimBracketLength, dimdim.convert_FrameToAbsX(framesActiveMouseTrim), dimdim);
			if (trimDirection == TrimDirection.Right)
				drawTrimBracket(g, x1 + clipW, y1, y2, TrimDirection.Right, trimBracketLength, dimdim.convert_FrameToAbsX(framesActiveMouseTrim), dimdim);
		}

		public void OutlineClipAudioHover(Graphics g, VidkaClipAudio aclip, ProjectDimensions dimdim, int Height)
		{
			throw new NotImplementedException();
			// TODO: this was never used...
			// TODO: write a generic function to handle both outline of video and audio clips
			int y1 = dimdim.getY_audio1(Height);
			int y2 = dimdim.getY_audio2(Height);
			//var secStart = dimdim.FrameToSec(aclip.FrameStart);
			//var secEnd = dimdim.FrameToSec(aclip.FrameEnd);
			int x1 = dimdim.convert_Frame2ScreenX(aclip.FrameStart);
			int x2 = dimdim.convert_Frame2ScreenX(aclip.FrameEnd);
			g.DrawRectangle(penHover, x1, y1, x2 - x1, y2 - y1);
			// TODO: audio clip trim direction not implemented!!!
		}

		internal void DrawCurrentFrameMarker(
			Graphics g,
			long markerFrame,
			int h,
			ProjectDimensions dimdim)
		{
			var markerX = dimdim.convert_Frame2ScreenX(markerFrame);
			g.DrawLine(penMarker, markerX, 0, markerX, h);
			//g.DrawString("" + markerFrame, fontDefault, brushDefault, markerX, 0);
		}

		internal void DrawCurrentClipVideo(
			Graphics g,
			VidkaClipVideo vclip,
			ProjectDimensions dimdim,
			VidkaProj proj,
			VidkaFileMapping projMapping,
			int w, int h,
			OutlineClipType type,
			TrimDirection trimDirection,
			int trimBracketLength,
			long markerFrame,
			long selectedClipFrameOffset,
			long framesActiveMouseTrim)
		{
			int yMainTop = dimdim.getY_main1(h);
			int xMain1 = dimdim.getScreenX1(vclip);
			int xMain2 = xMain1 + dimdim.convert_FrameToAbsX(vclip.LengthFrameCalc); //hacky, I know
			int xMainDelta = dimdim.convert_FrameToAbsX(framesActiveMouseTrim); //hacky, I know
			int xOrig1 = (int)((float)w * vclip.FrameStart / vclip.FileLengthFrames);
			int xOrig2 = (int)((float)w * vclip.FrameEnd / vclip.FileLengthFrames);
			int xOrigDelta = (int)((float)w * framesActiveMouseTrim / vclip.FileLengthFrames); //hacky, I know
			int y1 = dimdim.getY_original1(h);
			int y2 = dimdim.getY_original2(h);
			int yaudio = dimdim.getY_original_half(h);

			// draw entire original clip (0 .. vclip.FileLength)
			g.FillRectangle(brushWhite, 0, y1, w, y2 - y1);
			g.FillRectangle(brushActive, xOrig1, y1, xOrig2 - xOrig1, y2 - y1);
			DrawClipBitmaps(g, proj, projMapping, vclip, 0, y1, w, yaudio - y1, 0, vclip.FileLengthSec ?? 0);
			DrawWaveform(g, proj, projMapping, vclip, 0, yaudio, w, y2 - yaudio, 0, vclip.FileLengthSec ?? 0);
			g.DrawLine(penGray, 0, yaudio, w, yaudio);
			g.DrawRectangle(penDefault, 0, y1, w, y2 - y1);

			//draw clip bounds
			g.DrawLine((type == OutlineClipType.Hover) ? penHover : penGray, xMain1, yMainTop, xOrig1, y2);
			g.DrawLine((type == OutlineClipType.Hover) ? penHover : penGray, xMain2, yMainTop, xOrig2, y2);
			g.DrawLine((type == OutlineClipType.Hover) ? penHover : penGray, xOrig1, y1, xOrig1, y2);
			g.DrawLine((type == OutlineClipType.Hover) ? penHover : penGray, xOrig2, y1, xOrig2, y2);
			if (type == OutlineClipType.Hover)
			{
				if (trimDirection == TrimDirection.Left) {
					g.DrawLine(penActiveBoundary, xMain1 + xMainDelta, yMainTop, xOrig1 + xOrigDelta, y2);
					drawTrimBracket(g, xOrig1, y1, y2, TrimDirection.Left, trimBracketLength, xOrigDelta, dimdim);
				}
				if (trimDirection == TrimDirection.Right) {
					g.DrawLine(penActiveBoundary, xMain2 + xMainDelta, yMainTop, xOrig2 + xOrigDelta, y2);
					drawTrimBracket(g, xOrig2, y1, y2, TrimDirection.Right, trimBracketLength, xOrigDelta, dimdim);
				}
			}

			// draw marker on 
			var frameOffset = markerFrame - selectedClipFrameOffset + vclip.FrameStart;
			int xMarker = (int)((float)w * frameOffset / vclip.FileLengthFrames);
			g.DrawLine(penMarker, xMarker, y1, xMarker, y2);
		}

		internal void DrawCurrentClipAudio(Graphics graphics, VidkaClipAudio vidkaAudioClip, int Width, int Height, ProjectDimensions projectDimensions)
		{
			throw new NotImplementedException();
		}

		#region ================================== helpers ===================================

		private void DrawWaveform(
			Graphics g,
			VidkaProj proj,
			VidkaFileMapping projMapping,
			VidkaClipVideo vclip,
			int x1, int y1, int clipw, int cliph,
			double secStart, double secEnd)
		{
			// TODO: tmp!!! please use a cache or something u idiot, dont read the fucking file on every paint
			string waveFile = projMapping.AddGetWaveFilenameJpg(vclip.FileName);
			if (File.Exists(waveFile))
			{
				Image origWave = System.Drawing.Image.FromFile(waveFile, true);
				var bmpWave = new Bitmap(origWave);
				var xSrc1 = (int)(bmpWave.Width * secStart / vclip.FileLengthSec); //TODO: this
				var xSrc2 = (int)(bmpWave.Width * secEnd / vclip.FileLengthSec);
				srcRect.X = xSrc1;
				srcRect.Width = xSrc2 - xSrc1;
				srcRect.Y = 0;
				srcRect.Height = bmpWave.Height; //TODO: use constant from Ops
				destRect.X = x1;
				destRect.Y = y1;
				destRect.Width = clipw;
				destRect.Height = cliph;
				g.DrawImage(bmpWave, destRect: destRect, srcRect: srcRect, srcUnit: GraphicsUnit.Pixel);
				origWave.Dispose();
			}
		}

		/// <param name="secStart">needs to be in seconds to figure out which thumb</param>
		/// <param name="len">needs to be in seconds to figure out which thumb</param>
		private void DrawClipBitmaps(
			Graphics g,
			VidkaProj proj,
			VidkaFileMapping projMapping,
			VidkaClipVideo vclip,
			int x1, int y1, int clipw, int clipvh,
			double secStart, double len)
		{
			string thumbsFile = projMapping.AddGetThumbnailFilename(vclip.FileName);
			//if (!File.Exists(thumbsFile))
			//	return;
			//Image origThumb = System.Drawing.Image.FromFile(thumbsFile, true);
			//var bmpThumb = new Bitmap(origThumb);
			var heightForThumbs = Math.Max(clipvh - 2 * THUMB_MARGIN_Y, ThumbnailTest.ThumbH);
			var thumbPrefWidth = heightForThumbs * ThumbnailTest.ThumbW / ThumbnailTest.ThumbH;
			var howManyThumbs = (clipw - THUMB_MARGIN) / (thumbPrefWidth + THUMB_MARGIN);
			if (howManyThumbs == 0)
				howManyThumbs = 1;
			var xCenteringOffset = (clipw - howManyThumbs * (thumbPrefWidth + THUMB_MARGIN)) / 2;
			for (int i = 0; i < howManyThumbs; i++)
			{
				//DrawVideoThumbnail(
				//	g: g,
				//	bmpAll: bmpThumb,
				//	timeSec: secStart + (i + 0.5) * len / howManyThumbs,
				//	xCenter: x1 + xCenteringOffset + i * (thumbPrefWidth + THUMB_MARGIN) + (thumbPrefWidth + THUMB_MARGIN) / 2,
				//	yCenter: y1 + clipvh / 2,
				//	preferredWidth: thumbPrefWidth,
				//	maxWidth: clipw);
				var timeSec = secStart + (i + 0.5) * len / howManyThumbs;
				var imageIndex = (int)(timeSec / ThumbnailTest.ThumbIntervalSec);
				DrawVideoThumbnail(
					g: g,
					filenameAll: thumbsFile,
					index: imageIndex,
					xCenter: x1 + xCenteringOffset + i * (thumbPrefWidth + THUMB_MARGIN) + (thumbPrefWidth + THUMB_MARGIN) / 2,
					yCenter: y1 + clipvh / 2,
					preferredWidth: thumbPrefWidth,
					maxWidth: clipw);
			}
			//bmpThumb.Dispose();
			//origThumb.Dispose();
		}

		/// <summary>
		/// Draws one red bracket if drag frames = 0. If there has been a drag > 0,
		/// draws 2 brackets: one purple for original edge, one red for active (under mouse)
		/// </summary>
		private void drawTrimBracket(Graphics g, int x, int y1, int y2, TrimDirection trimDirection, int bracketLength, int trimDeltaX, ProjectDimensions dimdim)
		{
			if (trimDeltaX == 0)
				drawTrimBracketSingle(g, penActiveBoundary, x, y1, y2, trimDirection, bracketLength);
			else
			{
				g.FillRectangle(brushHazy, Math.Min(x, x + trimDeltaX), y1, Math.Abs(trimDeltaX), y2-y1);
				drawTrimBracketSingle(g, penActiveBoundaryPrev, x, y1, y2, trimDirection, bracketLength);
				drawTrimBracketSingle(g, penActiveBoundary, x + trimDeltaX, y1, y2, trimDirection, bracketLength);
			}
		}

		/// <summary>
		/// Only used in drawTrimBracket()
		/// </summary>
		private void drawTrimBracketSingle(Graphics g, Pen pen, int x, int y1, int y2, TrimDirection direction, int bracketLength)
		{
			var bracketDx = (direction == TrimDirection.Left)
				? bracketLength
				: -bracketLength;
			g.DrawLine(pen, x, y1, x, y2);
			g.DrawLine(pen, x, y1, x + bracketDx, y1);
			g.DrawLine(pen, x, y2, x + bracketDx, y2);
		}

		#endregion
	}
}
