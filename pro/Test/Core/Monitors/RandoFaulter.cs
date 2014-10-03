using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Test.Monitors
{
	[Monitor("RandoFaulter", true, IsTest = true)]
	[Description("Generate random faults for metrics testing")]
	[Parameter("Fault", typeof(int), "How often to fault", "10")]
	[Parameter("NewMajor", typeof(int), "How often to generate a new major", "5")]
	[Parameter("NewMinor", typeof(int), "How often to generate a new minor", "5")]
	public class RandoFaulter : Monitor
	{
		System.Random rnd = new System.Random();

		public int Fault { get; set; }
		public int NewMajor { get; set; }
		public int NewMinor { get; set; }

		bool isControl = false;

		string fmt = "X8";

		List<string> majors = new List<string>();
		Dictionary<string, List<string>> minors = new Dictionary<string, List<string>>();
		//Dictionary<string, Dictionary<string, string>> severities = new Dictionary<string, Dictionary<string, string>>();

		string[] severity = { "EXPLOITABLE", "PROBABLY EXPLOITABLE", "PROBABLY NOT EXPLOITABLE", "UNKNWON" };

		public RandoFaulter(IAgent agent, string name, Dictionary<string, Variant> args)
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

		uint currentIteration = 0;
		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			currentIteration = iterationCount;
		}

		public override bool IterationFinished()
		{
			return true;
		}

		public override bool DetectedFault()
		{
			// Avoid faulting on first iteration
			if (currentIteration < 2)
				return false;
			 
			if (!isControl && rnd.Next() % Fault == 0)
				return true;

			return false;
		}

		public override Fault GetMonitorData()
		{
			try
			{
				Fault fault = new Fault();

				fault.type = FaultType.Fault;
				fault.monitorName = this.Name;
				fault.detectionSource = "RandoFaulter";

				var buckets = GetBuckets();

				fault.majorHash = buckets[0];
				fault.minorHash = buckets[1];
				fault.exploitability = severity[rnd.Next(severity.Length)];

				fault.title = "Rando Faulter Funbag";
				fault.description = @"CUPERTINO, CA—Ending weeks of anticipation and intense speculation, tech giant Apple unveiled a short and fleeting moment of excitement to the general public Tuesday during a media event at its corporate headquarters. “With this groundbreaking new release, Apple has completely revolutionized the way we experience an ephemeral sense of wonder lasting no longer than several moments,” said Wired writer Gary Turnham, who added that the company has once again proved why it’s the global leader in developing exhilarating sensations that only temporarily mask one’s underlying feelings before dissolving away. “Even before today’s announcement, people across the country were lining up to be among the first to get their hands on this new short-lived and non-renewable flash of satisfaction. And they won’t be disappointed; this already vanishing glimmer of pleasure is exactly what we’ve come to expect from Apple.” According to Turnham, rumors are already swirling that Apple engineers are working on a slimmer, briefer moment of excitement projected for release next fall.";

				fault.collectedData.Add(new Fault.Data(
					"NetworkCapture1.pcap", Peach.Pro.Test.Resource1.snmpv2c));
				fault.collectedData.Add(new Fault.Data(
					"NetworkCapture2.pcapng", Peach.Pro.Test.Resource1.snmpv2c));
				fault.collectedData.Add(new Fault.Data(
					"BinaryData.bin", Peach.Pro.Test.Resource1.snmpv2c));

				return fault;
			}
			catch
			{
				throw;
			}
		}

		public string[] GetBuckets()
		{
			string major;
			string minor;

			if (majors.Count == 0 || rnd.Next() % NewMajor == 0)
			{
				major = rnd.Next().ToString(fmt);
				minor = rnd.Next().ToString(fmt);

				majors.Add(major);
				minors[major] = new List<string>();
				minors[major].Add(minor);

				return new string[] { major, minor };
			}

			major = majors[rnd.Next(majors.Count)];

			if (rnd.Next() % NewMinor == 0)
			{
				do
				{
					minor = rnd.Next().ToString(fmt);
				}
				while (minors[major].Contains(minor));

				minors[major].Add(minor);

				return new string[] { major, minor };
			}

			minor = minors[major][rnd.Next(minors[major].Count)];

			return new string[] { major, minor };
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{

			switch(((string)data).ToLower())
			{
				case "true":
					isControl = true;
					break;
				case "false":
					isControl = false;
					break;
				default:
					break;
			}
			return null;
		}
	}
}
