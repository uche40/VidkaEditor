using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.CreateFileAssociation
{
	public static class Utils
	{
		// http://stackoverflow.com/questions/2681878/associate-file-extension-with-application
		//[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
		public static void SetAssociation(string Extension, string KeyName, string OpenWith, string FileDescription)
		{
			// The stuff that was above here is basically the same
			RegistryKey BaseKey;
			RegistryKey OpenMethod;
			RegistryKey Shell;
			RegistryKey CurrentUser;

			BaseKey = Registry.CurrentUser.OpenSubKey("Software\\Classes", true).CreateSubKey(Extension);
			BaseKey.SetValue("", KeyName);

			OpenMethod = Registry.CurrentUser.OpenSubKey("Software\\Classes", true).CreateSubKey(KeyName);
			OpenMethod.SetValue("", FileDescription);
			OpenMethod.CreateSubKey("DefaultIcon").SetValue("", "\"" + OpenWith + "\",0");
			Shell = OpenMethod.CreateSubKey("Shell");
			Shell.CreateSubKey("edit").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
			Shell.CreateSubKey("open").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
			BaseKey.Close();
			OpenMethod.Close();
			Shell.Close();

			// Delete the key instead of trying to change it
			CurrentUser = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + Extension, true);
			CurrentUser.DeleteSubKey("UserChoice", false);
			CurrentUser.Close();

			// Tell explorer the file association has been changed
			SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
	}
}
