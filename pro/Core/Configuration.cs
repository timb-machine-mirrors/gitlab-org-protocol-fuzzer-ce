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
			LogRoot = config.AppSettings.Settings.Get("LogRoot");
			if (LogRoot == null)
				LogRoot = Utilities.GetAppResourcePath("Logs");
		}
	}
}
