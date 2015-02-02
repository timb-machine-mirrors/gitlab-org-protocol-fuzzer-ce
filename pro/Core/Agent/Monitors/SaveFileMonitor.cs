using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors
{
	/// <summary>
	/// Save a file when a fault occurs.
	/// </summary>
	[Monitor("SaveFile", true)]
	[Description("Saves the specified file as part of the logged data when a fault occurs")]
	[Parameter("Filename", typeof(string), "File to save on fault")]
	public class SaveFileMonitor : Monitor
	{
		public string Filename { get; set; }

		public SaveFileMonitor(string name)
			: base(name)
		{
		}

		public override MonitorData GetNewMonitorData()
		{
			if (!File.Exists(Filename))
				return null;

			var ret = new MonitorData
			{
				Title = "Save File \"{0}\".".Fmt(Filename),
				Data = new Dictionary<string,byte[]>
				{
					{ Path.GetFileName(Filename), File.ReadAllBytes(Filename) }
				}
			};

			return ret;
		}
	}
}
