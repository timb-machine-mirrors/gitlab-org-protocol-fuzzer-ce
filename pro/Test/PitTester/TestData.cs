using Peach.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Peach.Pro.Core;

namespace PitTester
{
	[XmlRoot("TestData", IsNullable = false, Namespace = "http://peachfuzzer.com/2012/TestData")]
	public class TestData
	{
		public TestData()
		{
			Defines = new List<Define>();
			Ignores = new List<Ignore>();
			Slurps = new List<Slurp>();
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

		public class Slurp
		{
			[XmlAttribute("setXpath")]
			public string SetXpath { get; set; }

			[XmlAttribute("valueType")]
			[DefaultValue("string")]
			public string ValueType { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}

		public class Ignore
		{
			[XmlAttribute("xpath")]
			public string Xpath { get; set; }
		}

		public class Test
		{
			public Test()
			{
				Actions = new List<Action>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("verifyDataSets")]
			[DefaultValue(true)]
			public bool VerifyDataSets { get; set; }

			[XmlAttribute("singleIteration")]
			[DefaultValue(true)]
			public bool SingleIteration { get; set; }

			[XmlAttribute("seed")]
			[DefaultValue("")]
			public string Seed { get; set; }

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

		public abstract class DataAction : Action
		{
			protected DataAction()
			{
				Payload = new byte[0];
			}

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

		public class GetProperty : DataAction
		{
			public override string ActionType { get { return "getProperty"; } }
		}

		public class Input : DataAction
		{
			public override string ActionType { get { return "input"; } }

			[XmlAttribute("datagram")]
			[DefaultValue(false)]
			public bool IsDatagram { get; set; }
		}

		public class Output : DataAction
		{
			public override string ActionType { get { return "output"; } }

			[XmlAttribute("ignore")]
			[DefaultValue(false)]
			public bool Ignore { get; set; }
		}

		[XmlElement("Define")]
		public List<Define> Defines { get; set; }

		[XmlElement("Ignore")]
		public List<Ignore> Ignores { get; set; }

		[XmlElement("Slurp")]
		public List<Slurp> Slurps { get; set; }

		[XmlElement("Test")]
		public List<Test> Tests { get; set; }
	}
}
