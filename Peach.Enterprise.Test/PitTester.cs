using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;

using NUnit.Framework;
using System.ComponentModel;

namespace Peach.Enterprise.Test
{
	[TestFixture]
	public class PitTester
	{
		#region TestData

		[XmlRoot("TestData", IsNullable = false, Namespace = "http://peachfuzzer.com/2012/TestData")]
		public class TestData
		{
			public TestData()
			{
				Defines = new List<Define>();
				Tests = new List<Test>();
			}

			public static TestData Parse(string fileName)
			{
				return XmlTools.Deserialize<TestData>(fileName);
			}

			public class Define
			{
				[XmlAttribute("key")]
				public string Key { get; set; }

				[XmlAttribute("value")]
				public string Value { get; set; }
			}

			public class Test
			{
				public Test()
				{
					Actions = new List<Action>();
				}

				[XmlAttribute("name")]
				public string Name { get; set; }

				[XmlElement("Start", Type = typeof(Start))]
				[XmlElement("Stop", Type = typeof(Stop))]
				[XmlElement("Open", Type = typeof(Open))]
				[XmlElement("Close", Type = typeof(Close))]
				[XmlElement("Accept", Type = typeof(Accept))]
				[XmlElement("Call", Type = typeof(Call))]
				[XmlElement("SetProperty", Type = typeof(SetProperty))]
				[XmlElement("GetProperty", Type = typeof(GetProperty))]
				[XmlElement("Input", Type = typeof(Input))]
				[XmlElement("Output", Type = typeof(Output))]
				public List<Action> Actions { get; set; }
			}

			public abstract class Action
			{
				public abstract string ActionType { get; }

				[XmlAttribute("action")]
				public string ActionName { get; set; }

				[XmlAttribute("publisher")]
				public string PublisherName { get; set; }

				protected static byte[] FromCData(string payload)
				{
					var sb = new StringBuilder();
					var rdr = new StringReader(payload);

					var line = "";
					while ((line = rdr.ReadLine()) != null)
					{
						// Expect 16 byte hex dump
						// some chars chars, whitespace, the bytes, 16 chars

						var space = line.IndexOf(' ') + 1;

						if (line.Length < (space + 16))
							continue;

						var subst = line.Substring(space, line.Length - 16 - space);
						subst = subst.Replace(" ", "");
						sb.Append(subst);
					}

					var ret = HexString.Parse(sb.ToString()).Value;
					return ret;
				}
			}

			public class Start : Action
			{
				public override string ActionType { get { return "start"; } }
			}

			public class Stop : Action
			{
				public override string ActionType { get { return "stop"; } }
			}

			public class Open : Action
			{
				public override string ActionType { get { return "open"; } }
			}

			public class Close : Action
			{
				public override string ActionType { get { return "close"; } }
			}

			public class Accept : Action
			{
				public override string ActionType { get { return "accept"; } }
			}

			public class Call : Action
			{
				public override string ActionType { get { return "call"; } }
			}

			public class SetProperty : Action
			{
				public override string ActionType { get { return "setProperty"; } }
			}

			public class GetProperty : Action
			{
				public override string ActionType { get { return "getProperty"; } }
			}

			public class Input : Action
			{
				public Input()
				{
					Payload = new byte[0];
				}

				public override string ActionType { get { return "input"; } }

				[XmlAttribute("datagram")]
				[DefaultValue(false)]
				public bool IsDatagram { get; set; }

				[XmlIgnore]
				public byte[] Payload { get; private set; }

				[XmlText]
				public XmlNode[] CDataSection
				{
					get
					{
						var msg = Utilities.HexDump(Payload, 0, Payload.Length);
						return new XmlNode[] { new XmlDocument().CreateCDataSection(msg) };
					}
					set
					{
						if (value == null)
						{
							Payload = new byte[0];
							return;
						}

						if (value.Length != 1)
							throw new InvalidOperationException();

						Payload = FromCData(value[0].Value);
					}
				}
			}

			public class Output : Action
			{
				public Output()
				{
					Payload = new byte[0];
				}

				public override string ActionType { get { return "output"; } }

				[XmlIgnore]
				public byte[] Payload { get; private set; }

				[XmlText]
				public XmlNode[] CDataSection
				{
					get
					{
						var msg = Utilities.HexDump(Payload, 0, Payload.Length);
						return new XmlNode[] { new XmlDocument().CreateCDataSection(msg) };
					}
					set
					{
						if (value == null)
						{
							Payload = new byte[0];
							return;
						}

						if (value.Length != 1)
							throw new InvalidOperationException();

						Payload = FromCData(value[0].Value);
					}
				}
			}

			[XmlElement("Define")]
			public List<Define> Defines { get; set; }

			[XmlElement("Test")]
			public List<Test> Tests { get; set; }
		}

		#endregion

		#region Logger

		private class PitTesterLogger : Logger
		{
			TestData.Test testData;
			int index;
			Core.Dom.Action action;
			bool verify;

			public string ActionName { get; private set; }

			public PitTesterLogger(TestData.Test testData)
			{
				this.testData = testData;
			}

			public T Verify<T>(string publisherName) where T : TestData.Action
			{
				if (!verify)
					return null;

				//Ignore implicit closes that are called at end of state model
				if (action == null)
					return null;

				try
				{
					if (index >= testData.Actions.Count)
						throw new PeachException("Missing record in test data");

					var d = testData.Actions[index++];
					

					if (typeof(T) != d.GetType())
					{
						var msg = "Encountered unexpected action type.\nAction Name: {0}\nExpected: {1}\nGot: {2}".Fmt(ActionName, typeof(T).Name, d.GetType().Name);
						throw new PeachException(msg);
					}

					if (d.PublisherName != publisherName)
						throw new PeachException("Publisher names didn't match. Expected {0} but got {1}".Fmt(publisherName, d.PublisherName));

					if (d.ActionName != ActionName)
						throw new PeachException("Action names didn't match.\n\tExpected: {0}\n\tBut got: {1}\n".Fmt(ActionName, d.ActionName));

					return (T)d;
				}
				catch
				{
					// don't perform anymore verification
					verify = false;

					throw;
				}
			}

			protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
			{
				verify = true;
				index = 0;
			}

			protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
			{
				// TODO: Assert we made it all the way through TestData.Actions
				//if (verify && index != testData.Actions.Count)
				//	throw new PeachException("Didn't make it all the way through the expected data");

				// Don't perform anymore verification
				// This prevents publisher stopping that happens
				// after the iteration from causing problems
				verify = false;
			}

			protected override void ActionStarting(RunContext context, Core.Dom.Action action)
			{
				this.action = action;

				ActionName = string.Join(".", new[] { action.parent.parent.name, action.parent.name, action.name });
			}

			protected override void ActionFinished(RunContext context, Core.Dom.Action action)
			{
				// If the action errored, don't do anymore verification
				if (action.error)
					verify = false;

				this.action = null;

				ActionName = null;
			}
		}

		#endregion

		#region Publisher

		private class PitTesterPublisher : Peach.Core.Publishers.StreamPublisher
		{
			private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
			protected override NLog.Logger Logger { get { return logger; } }

			string name;
			PitTesterLogger testLogger;

			public PitTesterPublisher(string name, PitTesterLogger testLogger)
				: base(new Dictionary<string, Variant>())
			{
				this.name = name;
				this.testLogger = testLogger;
				this.stream = new MemoryStream();
			}

			protected override void OnStart()
			{
				//testLogger.Verify<TestData.Start>(name);
			}

			protected override void OnStop()
			{
				//testLogger.Verify<TestData.Stop>(name);
			}

			protected override void OnOpen()
			{
				testLogger.Verify<TestData.Open>(name);
			}

			protected override void OnClose()
			{
				testLogger.Verify<TestData.Close>(name);
			}

			protected override void OnAccept()
			{
				testLogger.Verify<TestData.Accept>(name);
			}

			protected override Variant OnCall(string method, List<ActionParameter> args)
			{
				testLogger.Verify<TestData.Call>(name);
				throw new NotSupportedException();
			}

			protected override void OnSetProperty(string property, Variant value)
			{
				testLogger.Verify<TestData.SetProperty>(name);
				throw new NotSupportedException();
			}

			protected override Variant OnGetProperty(string property)
			{
				testLogger.Verify<TestData.GetProperty>(name);
				throw new NotSupportedException();
			}

			protected override void OnInput()
			{
				var data = testLogger.Verify<TestData.Input>(name);

				if (data.IsDatagram)
				{
					// This is the 'Datagram' publisher behavior
					stream.Seek(0, SeekOrigin.Begin);
					stream.Write(data.Payload, 0, data.Payload.Length);
					stream.SetLength(data.Payload.Length);
					stream.Seek(0, SeekOrigin.Begin);
				}
				else
				{
					// This is the 'Stream' publisher behavior
					var pos = stream.Position;
					stream.Seek(0, SeekOrigin.End);
					stream.Write(data.Payload, 0, data.Payload.Length);
					stream.Seek(pos, SeekOrigin.Begin);

					// TODO: For stream publishers, defer putting all of the
					// payload into this.stream and use 'WantBytes' to
					// deliver more bytes
				}
			}

			public override void output(DataModel dataModel)
			{
				var data = testLogger.Verify<TestData.Output>(name);
				var expected = data.Payload;

				// Only check outputs on non-fuzzing iterations
				if (!this.Test.parent.context.controlIteration)
					return;

				// Ensure we end on a byte boundary
				var bs = dataModel.Value.PadBits();
				bs.Seek(0, SeekOrigin.Begin);
				var actual = new BitReader(bs).ReadBytes((int)bs.Length);

				// If this data model has a file data set, compare to that
				var dataSet = dataModel.actionData.selectedData as Peach.Core.Dom.DataFile;
				if (dataSet != null)
					expected = File.ReadAllBytes(dataSet.FileName);

				if (expected.Length != actual.Length)
					throw new PeachException("Length mismatch in action {0}. Expected {1} bytes but got {2} bytes.".Fmt(testLogger.ActionName, expected.Length, actual.Length));

				for (int i = 0; i < actual.Length; ++i)
					if (expected[i] != actual[i])
						throw new PeachException("\nTest failed on action: {0}\n\tValues differ at offset 0x{3:x8}\n\tExpected: 0x{1:x2}\n\tBut was: 0x{2:x2}\n".Fmt(testLogger.ActionName, expected[i], actual[i], i));
			}

			protected override void OnOutput(BitwiseStream data)
			{
				// Handled with the override for output()
				throw new NotSupportedException();
			}
		}

		#endregion

		public static void TestPit(string pitLibrary, string pitName, string testName = null)
		{
			var fileName = Path.Combine(pitLibrary, pitName);

			//var defines = PitDefines.Parse(fileName + ".config");
			var testData = TestData.Parse(fileName + ".test");

			var defs = new List<KeyValuePair<string, string>>();
			foreach (var item in testData.Defines)
				if (item.Key != "PitLibraryPath")
					defs.Add(new KeyValuePair<string, string>(item.Key, item.Value));
			defs.Add(new KeyValuePair<string, string>("PitLibraryPath", pitLibrary));

			var args = new Dictionary<string, object>();
			args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

			var parser = new Peach.Core.Analyzers.PitParser();

			var dom = parser.asParser(args, fileName);

			foreach (var test in dom.tests)
			{
				test.agents.Clear();

				var data = testData.Tests.Where(t => t.Name == test.name).First();

				var logger = new PitTesterLogger(data);

				test.loggers.Clear();
				test.loggers.Add(logger);

				foreach (var key in test.publishers.Keys)
					test.publishers[key] = new PitTesterPublisher(key, logger);
			}

			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 0;
			config.rangeStop = 1;
			config.pitFile = Path.GetFileName(pitName);
			config.runName = testName ?? "Default";

			var e = new Engine(null);

			try
			{
				e.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				var msg = "Encountered an unhandled exception on iteration {0}, seed {1}.\n{2}".Fmt(
					e.context.currentIteration,
					config.randomSeed,
					ex.Message);
				throw new PeachException(msg, ex);
			}
		}

#if DISABLED
		/*
		 * Image Tests
		 */

		[Test]
		public void TestBmp()
		{
			TestPit("../../../../pits/pro", "Image/BMP.xml", "Default");
		}

		[Test]
		public void TestGif()
		{
			TestPit("../../../../pits/pro", "Image/GIF.xml", "Default");
		}

		[Test]
		public void TestICO()
		{
			TestPit("../../../../pits/pro", "Image/ICO.xml", "Default");
		}

		[Test]
		public void TestJPEG2000()
		{
			TestPit("../../../../pits/pro", "Image/JPEG2000.xml", "Default");
		}

		[Test]
		public void Testjpgjfif()
		{
			TestPit("../../../../pits/pro", "Image/jpg-jfif.xml", "Default");
		}


		[Test]
		public void TestPng()
		{
			TestPit("../../../../pits/pro", "Image/PNG.xml", "Default");
		}

		/*
		 * Video Tests
		 */

		[Test]
		public void TestAviDivx()
		{
			TestPit("../../../../pits/pro", "Video/avi_divx.xml", "Default");
		}

		/*
		 *  Network Tests
		 */

		[Test]
		public void TestArp()
		{
			TestPit("../../../../pits/pro", "Net/ARP.xml", "Default");
			TestPit("../../../../pits/pro", "Net/ARP.xml", "Reply");
		}


		[Test]
		public void TestCdp()
		{
			TestPit("../../../../pits/pro", "Net/CDP.xml", "Default");
		}

		[Test]
		public void TestDhcpv4()
		{
			TestPit("../../../../pits/pro", "Net/DHCPv4.xml", "Default");
		}

		[Test]
		public void TestDhcpv6Client()
		{
			TestPit("../../../../pits/pro", "Net/DHCPv6_Client.xml", "Default");
		}

		[Test]
		public void TestDhcpv6Server()
		{
			TestPit("../../../../pits/pro", "Net/DHCPv6_Server.xml", "Default");
		}

		[Test]
		public void TestEthernet()
		{
			TestPit("../../../../pits/pro", "Net/Ethernet.xml", "Default");
		}

		[Test]
		public void TestFtpClient()
		{
			TestPit("../../../../pits/pro", "Net/FTP_Client.xml", "Default");
			TestPit("../../../../pits/pro", "Net/FTP_Client.xml", "Passive");
		}

		[Test]
		public void TestFtpServer()
		{
			TestPit("../../../../pits/pro", "Net/FTP_Server.xml", "Default");
			TestPit("../../../../pits/pro", "Net/FTP_Server.xml", "Passive");
		}

		[Test]
		public void TestHttp()
		{
			TestPit("../../../../pits/pro", "Net/HTTP.xml", "Default");
		}

		[Test]
		public void TestIcmpv4()
		{
			TestPit("../../../../pits/pro", "Net/ICMPv4.xml", "Echo");
			//Need Real data for other icmp messages
		}

		[Test]
		public void TestIcmpv6()
		{
			TestPit("../../../../pits/pro", "Net/ICMPv6.xml", "Echo");
			TestPit("../../../../pits/pro", "Net/ICMPv6.xml", "NeighborSolicitation");
			//Need tests for the rest of the ICMPv6 types
		}

		[Test]
		public void TestIgmp()
		{
			//We just blast packets. Need actual server/client states to test
			TestPit("../../../../pits/pro", "Net/IGMP.xml", "Default");
		}

		[Test]
		public void TestIPsec()
		{
			//We just blast packets. Need actual states to test
			TestPit("../../../../pits/pro", "Net/IPSECv6.xml", "Default");
		}

		[Test]
		public void TestIPv4()
		{
			TestPit("../../../../pits/pro", "Net/IPv4.xml", "Default");
		}

		[Test]
		public void TestIPv6()
		{
			//We just blast packets. Need actual states to test
			TestPit("../../../../pits/pro", "Net/IPv6.xml", "Default");
		}

		[Test]
		public void TestLacp()
		{
			TestPit("../../../../pits/pro", "Net/LACP.xml", "Default");
		}

		[Test]
		public void TestLldp()
		{
			//We just blast packets. Need actual server/client states to test
			TestPit("../../../../pits/pro", "Net/LLDP.xml", "Default");
		}

		[Test]
		public void TestMld()
		{
			//We just blast packets. Need actual server/client states to test
			TestPit("../../../../pits/pro", "Net/MLD.xml", "Default");
		}

		[Test]
		public void TestModbus()
		{
			TestPit("../../../../pits/pro", "Net/Modbus.xml", "Default");
		}

		[Test]
		public void TestNtp()
		{
			TestPit("../../../../pits/pro", "Net/NTP.xml", "Default");
		}

		[Test]
		public void TestSnmpClient()
		{
			TestPit("../../../../pits/pro", "Net/SNMP_Client.xml", "Default");
		}

		[Test]
		public void TestSnmpServer()
		{
			TestPit("../../../../pits/pro", "Net/SNMP_Server.xml", "Default");
		}

		[Test]
		public void TestTcpv4()
		{
			TestPit("../../../../pits/pro", "Net/TCPv4.xml", "Default");
		}

		[Test]
		public void TestTcpv6()
		{
			TestPit("../../../../pits/pro", "Net/TCPv6.xml", "Default");
		}

		[Test]
		public void TestTelnetClient()
		{
			TestPit("../../../../pits/pro", "Net/TELNET_Client.xml", "Default");
		}

		[Test]
		public void TestTelnetServer()
		{
			TestPit("../../../../pits/pro", "Net/TELNET_Server.xml", "Default");
		}

		[Test]
		public void TestUdpv4()
		{
			TestPit("../../../../pits/pro", "Net/UDPv4.xml", "Default");
		}

		[Test]
		public void TestUdpv6()
		{
			TestPit("../../../../pits/pro", "Net/UDPv6.xml", "Default");
		}

		[Test]
		public void TestVlan()
		{
			TestPit("../../../../pits/pro", "Net/VLAN.xml", "Default");
		}

		[Test]
		public void TestVxlan()
		{
			TestPit("../../../../pits/pro", "Net/VXLAN.xml", "Default");
		}
#endif
	}
}
