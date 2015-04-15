using System.IO;
using NLog;
using Peach.Core;

namespace Peach.Pro.Core
{
	public class Configuration
	{
		public static string LogRoot { get; set; }

		public static LogLevel LogLevel { get; set; }

		static Configuration()
		{
			var config = Utilities.GetUserConfig();
			var path = 
				config.AppSettings.Settings.Get("LogRoot") ?? 
				Utilities.GetAppResourcePath("Logs");
			LogRoot = Path.GetFullPath(path);
		}
	}
}
