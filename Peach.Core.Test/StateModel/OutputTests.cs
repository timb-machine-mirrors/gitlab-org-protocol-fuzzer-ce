﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using NLog;
using NLog.Targets;
using NLog.Config;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Publishers;

namespace Peach.Core.Test.StateModel
{
	[TestFixture]
	class OutputTests
	{
		[Test]
		public void Test1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String value=\"Hello World!\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheStateModel\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(new Dictionary<string, string>(), new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			MemoryStream stream = new MemoryStream();
			dom.tests[0].publishers[0] = new MemoryStreamPublisher(stream);

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.config = config;
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			byte [] buff = new byte[stream.Length];

			stream.Position = 0;
			stream.Read(buff, 0, buff.Length);

			Assert.AreEqual(ASCIIEncoding.ASCII.GetBytes("Hello World!"), buff);
		}
	}
}
