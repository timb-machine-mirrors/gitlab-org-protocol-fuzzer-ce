using Peach.Core;

namespace Peach.Pro.Core
{
	public class Configuration
	{
		public static string LogRoot { get; set; }

		static Configuration()
		{
			var config = Utilities.GetUserConfig();
			LogRoot = config.AppSettings.Settings.Get("LogRoot");
			if (LogRoot == null)
				LogRoot = Utilities.GetAppResourcePath("db");
		}
	}
}
