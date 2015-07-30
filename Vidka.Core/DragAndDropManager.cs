using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vidka.Core.Model;
using Vidka.Core.Ops;
using Vidka.Core.Properties;
using Vidka.Core.VideoMeta;

namespace Vidka.Core
{
	public class DragAndDropManager
	{
		#region events
		public delegate void MetaReadyForDraggyH(string filename, VideoMetadataUseful meta);
		public event MetaReadyForDraggyH MetaReadyForDraggy;

		public delegate void MetaReadyForOutstandingVideoH(VidkaClipVideo vclip, VideoMetadataUseful meta);
		public event MetaReadyForOutstandingVideoH MetaReadyForOutstandingVideo;

		public delegate void MetaReadyForOutstandingAudioH(VidkaClipAudio aclip, VideoMetadataUseful meta);
		public event MetaReadyForOutstandingAudioH MetaReadyForOutstandingAudio;

		public delegate void ThumbOrWaveReadyH();
		public event ThumbOrWaveReadyH ThumbOrWaveReady;
		#endregion

		private IVideoEditor editor;
		private readonly string[] EXT_video, EXT_audio;
		private MetaGeneratorInOtherThread metaGenerator;
		private List<DragAndDropMediaFile> _draggies;
		private List<VidkaClipVideo> outstandingVideo;
		private List<VidkaClipAudio> outstandingAudio;

		// public
		public VidkaProj Proj { get; private set; }
		public DragAndDropManagerMode Mode { get; private set; }
		public IEnumerable<DragAndDropMediaFile> Draggies { get { return _draggies; } }

		public DragAndDropManager(IVideoEditor editor, VidkaProj proj, VidkaFileMapping fileMapping)
		{
			this.editor = editor;
			Proj = proj;
			Mode = DragAndDropManagerMode.None;
			_draggies = new List<DragAndDropMediaFile>();
			outstandingVideo = new List<VidkaClipVideo>();
			outstandingAudio = new List<VidkaClipAudio>();
			EXT_video = Settings.Default.FileExtensionsVideo.Split('|');
			EXT_audio = Settings.Default.FileExtensionsAudio.Split('|');
			metaGenerator = new MetaGeneratorInOtherThread(fileMapping);
			//metaGenerator.OneItemFinished += metaGenerator_OneItemFinished;
			//metaGenerator.MetaGeneratorDone += metaGenerator_MetaGeneratorDone;
			metaGenerator.HereIsSomeTextForConsole += genericListener_AppendToConsole;
			metaGenerator.MetaReady += metaGenerator_MetaReady;
			metaGenerator.ThumbnailsReady += metaGenerator_ThumbReady;
			metaGenerator.WaveformReady += metaGenerator_WaveReady;
		}

		public void SetProj(VidkaProj proj) {
			Proj = proj;
		}

		public void NewFilesDragged(string[] filenames, long nFakeFrames)
		{
			var relevantFiles = GetRelevantFilenames(filenames);
			var sampleFirst = relevantFiles.FirstOrDefault();
			if (IsFilenameVideo(sampleFirst))
			{
				Mode = DragAndDropManagerMode.DraggingVideo;
				foreach (var filename in relevantFiles)
				{
					_draggies.Add(new DragAndDropMediaFile(Proj) {
						Filename = filename,
						NFakeFrames = nFakeFrames,
					});
					metaGenerator.RequestMeta(filename);
					metaGenerator.RequestThumbsAndWave(filename);
				}
			}
			else if (IsFilenameAudio(sampleFirst))
			{
				Mode = DragAndDropManagerMode.DraggingAudio;
				// TODO: Q audio...
			}
			else
			{
				
			}
			// TODO: the mapped filenames (xml, thumbs, etc) should be done inside metaGenerator
			//var filenameXml = metaGenerator. Proj.Mapping.AddGetMetaFilename(filename);
			// runs the XML generations
			//new Ops.MetadataExtraction(filename, filenameXml);
		}

		public VidkaClipVideo[] FinalizeDragAndMakeVideoClips()
		{
			if (Mode != DragAndDropManagerMode.DraggingVideo)
				return null;
			lock (this)
			{
				//TODO: Take(1) is to be removed when we support multiple draggies
				var clips = _draggies.Take(1).Select(x => new VidkaClipVideo
				{
					FileName = x.Filename,
					FileLengthSec = Proj.FrameToSec(x.LengthInFrames),
					FrameStart = 0,
					FrameEnd = x.LengthInFrames, //Proj.SecToFrame(dragMeta.VideoDurationSec) // its ok because SecToFrame floors it
					IsNotYetAnalyzed = (x.Meta == null)
				}).ToList();
				outstandingVideo.AddRange(clips.Where(x => x.IsNotYetAnalyzed));
				_draggies.Clear();
				Mode = DragAndDropManagerMode.None;
				return clips.ToArray();
			}
		}
		public VidkaClipAudio[] FinalizeDragAndMakeAudioClips()
		{
			if (Mode != DragAndDropManagerMode.DraggingAudio)
				return null;
			lock (this)
			{
				//TODO: Take(1) is to be removed when we support multiple draggies
				var clips = _draggies.Take(1).Select(x => new VidkaClipAudio {
					FileName = x.Filename,
					FileLengthSec = Proj.FrameToSec(x.LengthInFrames),
					FrameStart = 0,
					FrameEnd = x.LengthInFrames, //Proj.SecToFrame(dragMeta.VideoDurationSec) // its ok because SecToFrame floors it
					IsNotYetAnalyzed = (x.Meta == null)
				}).ToList();
				outstandingAudio.AddRange(clips.Where(x => x.IsNotYetAnalyzed));
				_draggies.Clear();
				Mode = DragAndDropManagerMode.None;
				throw new NotImplementedException();
				return clips.ToArray();
			}
		}

		internal void CancelDragDrop()
		{
			_draggies.Clear();
			Mode = DragAndDropManagerMode.None;
		}

		#region event handlers

		private void genericListener_AppendToConsole(VidkaConsoleLogLevel level, string text)
		{
			editor.AppendToConsole(level, text);
		}
		private void metaGenerator_MetaReady(string filename, VideoMetadataUseful meta)
		{
			lock (this)
			{
				// 2 cases: meta is ready for one of the draggies, or for one of the outstanding media
				var draggyMaybe = _draggies.FirstOrDefault(x => x.Filename == filename);
				if (draggyMaybe != null)
				{
					draggyMaybe.Meta = meta;
					if (MetaReadyForDraggy != null)
						MetaReadyForDraggy(filename, meta);
					return;
				}
				// at this point it could be one of outstanding media (video or audio)
				var outstandingMaybeVid = outstandingVideo.FirstOrDefault(x => x.FileName == filename);
				if (outstandingMaybeVid != null)
				{
					// TODO: handle variable fps, fps == proj.fps and counted frames for PENTAX avis
					outstandingMaybeVid.FileLengthSec = meta.VideoDurationSec;
					var projFramesThisOne = Proj.SecToFrame(outstandingMaybeVid.FileLengthSec ?? 0); // remember, this clip could be different fps, we need proj's fps
					outstandingMaybeVid.FileLengthFrames = projFramesThisOne;
					outstandingMaybeVid.FrameEnd = projFramesThisOne;
					outstandingMaybeVid.IsNotYetAnalyzed = false;
					outstandingVideo.Remove(outstandingMaybeVid);
					if (MetaReadyForOutstandingVideo != null)
						MetaReadyForOutstandingVideo(outstandingMaybeVid, meta);
					return;
				}
				var outstandingMaybeAud = outstandingAudio.FirstOrDefault(x => x.FileName == filename);
				if (outstandingMaybeAud != null)
				{
					outstandingMaybeAud.FileLengthSec = Proj.FrameToSec(meta.VideoDurationFrames);
					outstandingMaybeAud.FrameEnd = meta.VideoDurationFrames;
					outstandingAudio.Remove(outstandingMaybeAud);
					if (MetaReadyForOutstandingAudio != null)
						MetaReadyForOutstandingAudio(outstandingMaybeAud, meta);
					return;
				}
			}
		}
		private void metaGenerator_WaveReady(string filename, string fileWave, string fileWaveJpg)
		{
			if (ThumbOrWaveReady != null)
				ThumbOrWaveReady();
		}
		private void metaGenerator_ThumbReady(string filename, string fileThumbs)
		{
			if (ThumbOrWaveReady != null)
				ThumbOrWaveReady();
		}

		//private void metaGenerator_OneItemFinished(MetaGeneratorInOtherThread_QueueRequest item)
		//{
		//	// just repaint the damn thing
		//	editor.PleaseRepaint();
		//}
		//private void metaGenerator_MetaGeneratorDone(MetaGeneratorInOtherThread_QueueRequest item, VideoMetadataUseful meta)
		//{
		//	UiObjects.SetDraggyCoordinates(frameLength: meta.VideoDurationFrames);
		//	editor.PleaseRepaint();
		//}

		#endregion

		
		#region helpers

		private bool IsFilenameVideo(string filename)
		{
			if (String.IsNullOrEmpty(filename))
				return false;
			var ext = Path.GetExtension(filename).ToLower();
			return EXT_video.Any(e => String.Equals(e, ext));
		}

		private bool IsFilenameAudio(string filename)
		{
			if (String.IsNullOrEmpty(filename))
				return false;
			var ext = Path.GetExtension(filename).ToLower();
			return EXT_audio.Any(e => String.Equals(e, ext));
		}

		/// <summary>
		/// Removes all non-video and non-audio filenames (by extension)
		/// Returns only video if given a mix of video and audio files.
		/// </summary>
		private string[] GetRelevantFilenames(string[] filenames)
		{
			// TODO: implement this filter please, otherwise our draggy will be fucked
			return filenames;
		}

		#endregion

	}

	public enum DragAndDropManagerMode
	{
		None = 0,
		DraggingVideo = 1,
		DraggingAudio = 2,
	}
	public class DragAndDropMediaFile
	{
		private VidkaProj proj;
		public DragAndDropMediaFile(VidkaProj proj)
		{
			this.proj = proj;
		}
		public string Filename { get; set; }
		public long NFakeFrames { get; set; }
		public VideoMetadataUseful Meta { get; set; }
		public long LengthInFrames { get {
			return (Meta != null) ? proj.SecToFrame(Meta.VideoDurationSec) : NFakeFrames;
		} }
		//public double LengthInSec { get {
		//	return (Meta != null) ? Meta.VideoDurationSec : proj.FrameToSec(NFakeFrames);
		//} }
	}

}
