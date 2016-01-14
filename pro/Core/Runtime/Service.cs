using System.Collections.Generic;
using Peach.Core.Runtime;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.Core.Runtime
{
	public class Service : Program
	{
		string _pitLibraryPath;

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"pits=",
				"Pit Library Path",
				v => _pitLibraryPath = v
			);
		}

		protected override void OnRun(List<string> args)
		{
			// Ensure pit library exists
			var pits = FindPitLibrary(_pitLibraryPath);

			if (CreateWeb != null)
				RunWeb(pits, false, new ExternalJobMonitor());
		}
	}
}
