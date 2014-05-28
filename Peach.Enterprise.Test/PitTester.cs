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

namespace Peach.Enterprise.Test
{
	public class PitTester
	{
		#region TestData

		[XmlRoot("TestData")]
		public class TestData
		{
			public TestData()
			{
				Tests = new List<Test>();
			}

			public static TestData Parse(string fileName)
			{
				var s = new XmlSerializer(typeof(TestData));
				var o = s.Deserialize(XmlReader.Create(fileName));
				var r = (TestData)o;
				return r;
			}

			public class Test
			{
				public Test()
				{
					Actions = new List<Action>();
				}

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

				[XmlAttribute("name")]
				public string Name { get; set; }
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
						// 8 chars, whitespace, some stuff, 16 chars
						if (line.Length < (9 + 16))
							continue;

						var subst = line.Substring(8, line.Length - (8 + 16));
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
				public override string ActionType { get { return "output"; } }
			}

			[XmlElement("Test")]
			public List<Test> Tests { get; set; }
		}

		#endregion

		#region Logger

		private class PitTesterLogger : Logger
		{
			public TestData.Test TestData { get; private set; }

			public int CurrentActionNumber { get; private set; }

			public Core.Dom.Action CurrentAction { get; private set; }

			public string CurrentActionName
			{
				get
				{
					return string.Join(".", new[] { CurrentAction.parent.parent.name, CurrentAction.parent.name, CurrentAction.name });
				}
			}

			public TestData.Action CurrentTestData
			{
				get
				{
					return TestData.Actions[CurrentActionNumber];
				}
			}

			public PitTesterLogger(TestData.Test testData)
			{
				TestData = testData;
			}

			protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
			{
				CurrentActionNumber = 0;
			}

			protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
			{
				// TODO: Assert we made it all the way through TestData.Actions
			}

			protected override void ActionStarting(RunContext context, Core.Dom.Action action)
			{
				CurrentAction = action;
			}

			protected override void ActionFinished(RunContext context, Core.Dom.Action action)
			{
				++CurrentActionNumber;
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
				VerifyAction<TestData.Start>();
			}

			protected override void OnStop()
			{
				VerifyAction<TestData.Stop>();
			}

			protected override void OnOpen()
			{
				VerifyAction<TestData.Open>();
			}

			protected override void OnClose()
			{
				VerifyAction<TestData.Close>();
			}

			protected override void OnAccept()
			{
				VerifyAction<TestData.Accept>();
			}

			protected override Variant OnCall(string method, List<ActionParameter> args)
			{
				VerifyAction<TestData.Call>();
				throw new NotSupportedException();
			}

			protected override void OnSetProperty(string property, Variant value)
			{
				VerifyAction<TestData.SetProperty>();
				throw new NotSupportedException();
			}

			protected override Variant OnGetProperty(string property)
			{
				VerifyAction<TestData.GetProperty>();
				throw new NotSupportedException();
			}

			protected override void OnInput()
			{
				var data = VerifyAction<TestData.Input>();

				// TODO: Figure out if this is a datagram publisher

				// This is the 'Datagram' publisher behavior
				//stream.Seek(0, SeekOrigin.Begin);
				//stream.Write(data.Payload, 0, data.Payload.Length);
				//stream.SetLength(data.Payload.Length);
				//stream.Seek(0, SeekOrigin.Begin);

				// This is the 'Stream' publisher behavior
				var pos = stream.Position;
				stream.Seek(0, SeekOrigin.End);
				stream.Write(data.Payload, 0, data.Payload.Length);
				stream.Seek(pos, SeekOrigin.Begin);

				// TODO: For stream publishers, defer putting all of the
				// payload into this.stream and use 'WantBytes' to
				// deliver more bytes

			}

			public override void output(DataModel dataModel)
			{
				VerifyAction<TestData.Output>();
				throw new NotSupportedException();
			}

			protected override void OnOutput(BitwiseStream data)
			{
				// Handled with the override for output()
				throw new NotSupportedException();
			}

			T VerifyAction<T>() where T: TestData.Action
			{
				var d = testLogger.CurrentTestData;

				if (typeof(T) != d.GetType())
					throw new PeachException("Bad action type");

				if (d.PublisherName != name)
					throw new PeachException("Bad publisher name");

				if (d.ActionName != testLogger.CurrentActionName)
					throw new PeachException("Mismatch on test");

				return (T)testLogger.CurrentTestData;
			}
		}

		#endregion

		public static void TestPit(string pitLibrary, string pitName)
		{
			var fileName = Path.Combine(pitLibrary, pitName);


			var defines = PitDefines.Parse(fileName + ".config");
			var testData = TestData.Parse(fileName + ".test");

			var defs = new Dictionary<string, string>();
			foreach (var item in defines)
				defs[item.Key] = item.Value;
			defs["PitLibraryPath"] = pitLibrary;

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
			config.singleIteration = true;
			config.pitFile = Path.GetFileName(pitName);

			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}

		public void TestFtpClient()
		{
			TestPit("../../../../pits/pro", "Net/FTP_Client.xml");
		}
	}
}
