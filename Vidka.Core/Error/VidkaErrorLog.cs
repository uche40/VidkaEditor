using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vidka.Core.Properties;

namespace Vidka.Core.Error
{
	public class VidkaErrorLog
	{
		/// <summary>
		/// Constructor private, because u should use the Logger singleton
		/// </summary>
		private VidkaErrorLog() {
			
		}

		public void Log(string logMessage)
		{
			// from: https://msdn.microsoft.com/en-us/library/3zc0w663(v=vs.110).aspx
			using (StreamWriter w = File.AppendText(Settings.Default.ErrorLogFilename))
			{
				w.Write("\r\nLog Entry : ");
				w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
					DateTime.Now.ToLongDateString());
				w.WriteLine("  :");
				w.WriteLine("  :{0}", logMessage);
				w.WriteLine("-------------------------------");
			}
		}

		//------------------------------------------------

		/// <summary>
		/// singleton
		/// </summary>
		public static VidkaErrorLog Logger
		{
			get
			{
				return _logger ?? (_logger = new VidkaErrorLog());
			}
		}
		private static VidkaErrorLog _logger;
	}
}
