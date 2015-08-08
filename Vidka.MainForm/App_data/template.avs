global proj_frameRate = {proj-fps}
global proj_width = {proj-width}
global proj_height = {proj-height}
global proj_audiorate = 44100
global proj_audiochannels = 2
audiorate = proj_audiorate

# NOTE: we collect YUV (for which no conversion exists) and RGB, but the actual pixel type is more specific
# a video that is YV12 or YUV2 is gonna also be YUV, like wise RGB32 will also be RGB
global stat_nRGB = 0
global stat_nRGB24 = 0
global stat_nRGB32 = 0
global stat_nY8 = 0
global stat_nYUV = 0
global stat_nYUY2 = 0
global stat_nYV12 = 0
global stat_nYV16 = 0
global stat_nYV24 = 0
global stat_nYV411 = 0
global mostFrequentPixelType = "???"

#################### determine the most frequently used pixel type ############################
{collectpixeltypestat-videos}

global mostFrequentPixelType = "RGB32"
curStatMax = stat_nRGB32
global mostFrequentPixelType = (stat_nRGB24 > curStatMax) ? "RGB24" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nRGB24)
global mostFrequentPixelType = (stat_nYV12 > curStatMax) ? "YV12" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nRGB32)
global mostFrequentPixelType = (stat_nYV16 > curStatMax) ? "YV16" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nYV16)
global mostFrequentPixelType = (stat_nY8 > curStatMax) ? "Y8" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nY8)
#~ global mostFrequentPixelType = (stat_nYUV > curStatMax) ? "YUV" : mostFrequentPixelType
#~ curStatMax = Max(curStatMax, stat_nYUV)
global mostFrequentPixelType = (stat_nYUY2 > curStatMax) ? "YUY2" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nYUY2)
global mostFrequentPixelType = (stat_nYV24 > curStatMax) ? "YV24" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nYV24)
global mostFrequentPixelType = (stat_nYV411 > curStatMax) ? "YV411" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nYV411)
global mostFrequentPixelType = (stat_nRGB > curStatMax) ? "RGB" : mostFrequentPixelType
curStatMax = Max(curStatMax, stat_nRGB)

#################### render ###########################

{video-clips}

#All these things we can chain outside on the NeutralClip function...
# - FadeIn2 adds 2 frames, FadeIn adds 1 frame, FadeIn0 adds no frames (so use that one I guess).
#   The frames added are solid black, otherwise fade is not complete. See "Fade" documentation.
# - Reverse does not work
# .FadeIn0(10).FlipHorizontal().FlipVertical().Reverse()


#debug log file sample:
#~ nctest1 = NeutralClip("vid2.mp4", 500, 530)#.FadeIn2(10)
#~ Version().WriteFileStart("test.avs.log", String(nctest1.FrameCount))


#################### functions! ###########################

function NeutralClip(string filename, int fstart, int fend,\
		float "slowmo")
{
	# load clip
	#~ vclip = (filename.RightStr(4).lcase() == ".avi") \
		#~ ? AviSource(filename) \
		#~ : FFmpegSource2(filename) # fps=proj_frameRate, convertfps=true)
	vclip = DirectShowSource(filename, audio=True) # fps=proj_frameRate, convertfps=true)
	# if need be convert to color space of the most frequent one

	vclip = (mostFrequentPixelType == "RGB" && !vclip.IsRGB()) ? vclip.ConvertToRGB() : vclip
	vclip = (mostFrequentPixelType == "RGB24" && !vclip.IsRGB24()) ? vclip.ConvertToRGB24() : vclip
	vclip = (mostFrequentPixelType == "RGB32" && !vclip.IsRGB32()) ? vclip.ConvertToRGB32() : vclip
	vclip = (mostFrequentPixelType == "Y8" && !vclip.IsY8()) ? vclip.ConvertToY8() : vclip
	###### vclip = (mostFrequentPixelType == "YUV" && !vclip.IsYUV()) ? vclip.ConvertToYV12(matrix="Rec601", interlaced=false) : vclip
	vclip = (mostFrequentPixelType == "YUY2" && !vclip.IsYUY2()) ? vclip.ConvertToYUY2(matrix="Rec601", interlaced=false) : vclip
	vclip = (mostFrequentPixelType == "YV12" && !vclip.IsYV12()) ? vclip.ConvertToYV12(matrix="Rec601", interlaced=false) : vclip
	vclip = (mostFrequentPixelType == "YV16" && !vclip.IsYV16()) ? vclip.ConvertToYV16(interlaced=false) : vclip
	vclip = (mostFrequentPixelType == "YV24" && !vclip.IsYV24()) ? vclip.ConvertToYV24(interlaced=false) : vclip
	vclip = (mostFrequentPixelType == "YV411" && !vclip.IsYV411()) ? vclip.ConvertToYV411(interlaced=false) : vclip
	
	vclip = vclip.ConvertToYV12(matrix="Rec601", interlaced=false)
	
	# convert clip so that they are all same video size, fps, audio format, etc...
	vclip = (vclip.framerate != proj_frameRate) ? vclip.ChangeFPS(proj_frameRate) : vclip
	vclip = (vclip.width != proj_width || vclip.height != proj_height) ? vclip.BilinearResize(proj_width, proj_height) : vclip
	audioSilence = Tone(length=(vclip.FrameCount/vclip.FrameRate), samplerate=proj_audiorate, channels=proj_audiochannels, type="silence", level=0.0)
	vclip = !vclip.HasAudio ? AudioDub(vclip, audioSilence) : vclip
	vclip = (vclip.HasAudio && vclip.AudioChannels == 1 && proj_audiochannels == 2) \
		? AudioDub(vclip, vclip.GetChannel(1,1)) \
		: vclip
	vclip = (vclip.AudioRate != proj_audiorate) ? vclip.ResampleAudio(proj_audiorate) : vclip
	# trim, of course!
	vclip = vclip.Trim(fstart, fend, true)
	# misc manipulations
	#vclip = Defined(slowmo) ? ConvertFPS(slowmo * proj_frameRate).AssumeFPS(proj_frameRate, sync_audio=true).ResampleAudio(44100) : vclip
	
	# other samples...
	# vclip = fadein ? vclip.Reverse.FadeOut(fend-fstart).Reverse : vclip #fade in (doesnt work cause reverse doesnt work)
	
	#debug
	#pixType = vclip.PixelType()
	#~ vclip = vclip.subtitle("pixType:" + mostFrequentPixelType)
	#~ vclip = vclip.subtitle("stats:" + String(stat_nRGB) + "," + String(stat_nRGB24) + "," + String(stat_nRGB32) + "," + String(stat_nY8) + "," + String(stat_nYUV) + "," + String(stat_nYUY2) + "," + String(stat_nYV12) + "," + String(stat_nYV16) + "," + String(stat_nYV24) + "," + String(stat_nYV411))
	#vclip = vclip.Subtitle(String(vclip.framerate), align=2) #DEBUG
	return vclip
}

function CollectPixelTypeStat(filename, weight)
{
	vclip = DirectShowSource(filename, audio=True) # fps=proj_frameRate, convertfps=true)
	global stat_nRGB = stat_nRGB + (vclip.IsRGB() ? weight : 0)
	global stat_nRGB24 = stat_nRGB24 + (vclip.IsRGB24() ? weight : 0)
	global stat_nRGB32 = stat_nRGB32 + (vclip.IsRGB32() ? weight : 0)
	global stat_nY8 = stat_nY8 + (vclip.IsY8() ? weight : 0)
	global stat_nYUV = stat_nYUV + (vclip.IsYUV() ? weight : 0)
	global stat_nYUY2 = stat_nYUY2 + (vclip.IsYUY2() ? weight : 0)
	global stat_nYV12 = stat_nYV12 + (vclip.IsYV12() ? weight : 0)
	global stat_nYV16 = stat_nYV16 + (vclip.IsYV16() ? weight : 0)
	global stat_nYV24 = stat_nYV24 + (vclip.IsYV24() ? weight : 0)
	global stat_nYV411 = stat_nYV411 + (vclip.IsYV411() ? weight : 0)
}

