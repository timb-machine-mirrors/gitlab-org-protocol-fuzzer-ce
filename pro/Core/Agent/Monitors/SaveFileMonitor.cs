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
	[Parameter("Filename", typeof(string), "File to save on fault")]
	public class SaveFileMonitor : Monitor
	{
		public string Filename { get; private set; }

		public SaveFileMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
		}

		public override void StopMonitor()
		{
		}

		public override void SessionStarting()
		{
		}

		public override void SessionFinished()
		{
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
		}

		public override bool IterationFinished()
		{
			return false;
		}

		public override bool DetectedFault()
		{
			return false;
		}

		public override Fault GetMonitorData()
		{
			Fault fault = new Fault();
			fault.type = FaultType.Data;
			fault.title = "Save File \"" + Filename + "\"";
			fault.detectionSource = "SaveFileMonitor";
			fault.collectedData.Add(new Fault.Data(
				Path.GetFileName(Filename),
				File.ReadAllBytes(Filename)
			));

			return fault;
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
	}
}
