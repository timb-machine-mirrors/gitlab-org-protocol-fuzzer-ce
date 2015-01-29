using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("RandoFaulter", true, IsTest = true)]
	[Description("Generate random faults for metrics testing")]
	[Parameter("Fault", typeof(int), "How often to fault", "10")]
	[Parameter("NewMajor", typeof(int), "How often to generate a new major", "5")]
	[Parameter("NewMinor", typeof(int), "How often to generate a new minor", "5")]
	[Parameter("Boolean", typeof(bool), "A boolean parameter", "true")]
	[Parameter("String", typeof(string), "A string parameter", "some string")]
	[Parameter("When", typeof(MonitorWhen), "An enum parameter", "OnCall")]
	public class RandoFaulter : Monitor
	{
		readonly System.Random _rnd = new System.Random();

		public int Fault { get; set; }
		public int NewMajor { get; set; }
		public int NewMinor { get; set; }
		public bool Boolean { get; set; }
		public string String { get; set; }
		public MonitorWhen When { get; set; }

		private const string Fmt = "X8";
		private static readonly byte[] Snmpv2CPacket = LoadResource("snmpv2c.pcap").ToArray();
		private static readonly string[] Severity = { "EXPLOITABLE", "PROBABLY EXPLOITABLE", "PROBABLY NOT EXPLOITABLE", "UNKNOWN" };

		uint _startCount;
		bool _isControl;
		readonly List<string> _majors = new List<string>();
		readonly Dictionary<string, List<string>> _minors = new Dictionary<string, List<string>>();

		public RandoFaulter(string name)
			: base(name)
		{
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			++_startCount;
		}

		public override bool DetectedFault()
		{
			// Avoid faulting on first run of the monitor
			if (_startCount < 2)
				return false;
			 
			if (!_isControl && _rnd.Next() % Fault == 0)
				return true;

			return false;
		}

		public override Fault GetMonitorData()
		{
			var fault = new Fault
			{
				type = FaultType.Fault,
				monitorName = Name,
				detectionSource = "RandoFaulter"
			};

			var buckets = GetBuckets();

			fault.majorHash = buckets[0];
			fault.minorHash = buckets[1];
			fault.exploitability = Severity[_rnd.Next(Severity.Length)];

			fault.title = "Rando Faulter Funbag";
			fault.description = @"CUPERTINO, CA—Ending weeks of anticipation and intense speculation, tech giant Apple unveiled a short and fleeting moment of excitement to the general public Tuesday during a media event at its corporate headquarters. “With this groundbreaking new release, Apple has completely revolutionized the way we experience an ephemeral sense of wonder lasting no longer than several moments,” said Wired writer Gary Turnham, who added that the company has once again proved why it’s the global leader in developing exhilarating sensations that only temporarily mask one’s underlying feelings before dissolving away. “Even before today’s announcement, people across the country were lining up to be among the first to get their hands on this new short-lived and non-renewable flash of satisfaction. And they won’t be disappointed; this already vanishing glimmer of pleasure is exactly what we’ve come to expect from Apple.” According to Turnham, rumors are already swirling that Apple engineers are working on a slimmer, briefer moment of excitement projected for release next fall.";

			fault.collectedData.Add(new Fault.Data("NetworkCapture1.pcap", Snmpv2CPacket));
			fault.collectedData.Add(new Fault.Data("NetworkCapture2.pcapng", Snmpv2CPacket));
			fault.collectedData.Add(new Fault.Data("BinaryData.bin", Snmpv2CPacket));

			return fault;
		}

		public override void Message(string msg)
		{
			switch (msg.ToLower())
			{
				case "true":
					_isControl = true;
					break;
				case "false":
					_isControl = false;
					break;
			}
		}

		private string[] GetBuckets()
		{
			string major;
			string minor;

			if (_majors.Count == 0 || _rnd.Next() % NewMajor == 0)
			{
				major = _rnd.Next().ToString(Fmt);
				minor = _rnd.Next().ToString(Fmt);

				_majors.Add(major);
				_minors[major] = new List<string> { minor };

				return new[] { major, minor };
			}

			major = _majors[_rnd.Next(_majors.Count)];

			if (_rnd.Next() % NewMinor == 0)
			{
				do
				{
					minor = _rnd.Next().ToString(Fmt);
				}
				while (_minors[major].Contains(minor));

				_minors[major].Add(minor);

				return new[] { major, minor };
			}

			minor = _minors[major][_rnd.Next(_minors[major].Count)];

			return new[] { major, minor };
		}

		private static MemoryStream LoadResource(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			var fullName = "Peach.Pro.Core.Resources." + name;
			using (var stream = asm.GetManifestResourceStream(fullName))
			{
				Debug.Assert(stream != null);
				var ms = new MemoryStream();
				stream.CopyTo(ms);
				return ms;
			}
		}
	}
}
