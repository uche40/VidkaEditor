using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.CreateFileAssociation
{
	class Program
	{
		static void Main(string[] args)
		{
			var exePath = args.FirstOrDefault();
			if (exePath == null) {
				Console.WriteLine("Must specify the exe path as the first argument!");
				return;
			}

			Utils.SetAssociation(".vidka", "VidkaEditor.vidka", exePath, "Vidka Project");
		}
	}
}
