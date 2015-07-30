using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidka.Core.VideoMeta;

namespace Vidka.Core
{
	public interface IVidkaConsole
	{
		void AppendToConsole(VidkaConsoleLogLevel level, string text);		
	}

	public static class IVidkaConsole_extensions
	{
		public static void cxzxc(this IVidkaConsole console, string text) {
			console.AppendToConsole(VidkaConsoleLogLevel.Debug, text);
		}
		public static void iiii(this IVidkaConsole console, string text) {
			console.AppendToConsole(VidkaConsoleLogLevel.Info, text);
		}
		public static void errr(this IVidkaConsole console, string text) {
			console.AppendToConsole(VidkaConsoleLogLevel.Error, text);
		}
	}
}
