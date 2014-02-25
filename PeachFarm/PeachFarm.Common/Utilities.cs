using System;
using System.Reflection;
using System.ServiceProcess;

namespace PeachFarm.Common
{
	public static class Utilities
	{
		static Utilities()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
			{
				// mono-service sets RunService to non-null when we are 
				var fi = typeof(ServiceBase).GetField("RunService", BindingFlags.Static | BindingFlags.NonPublic);
				IsService = fi != null && fi.GetValue(null) != null;
			}
			else
			{
				IsService = !Environment.UserInteractive;
			}
		}

		public static bool IsService { get; private set; }
	}
}
