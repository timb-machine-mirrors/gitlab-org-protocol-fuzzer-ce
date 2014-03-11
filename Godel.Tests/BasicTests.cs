using System;
using System.IO;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

using NUnit.Framework;
//using NUnit.Framework.Constraints;

using Peach.Core;
//using Peach.Core.Dom;
//using Peach.Core.Analyzers;
//using Peach.Core.IO;

namespace Godel.Tests
{
	[TestFixture]
	class BasicTests
	{
		[Test]
		public void ParserTest()
		{
			string xml = @"
<Peach>

	<DataModel name='DM'>
		<String value='Hello World'/>
	</DataModel>

	<Godel name='BasicContext' inv='1 == 1' pre='2 == 2' post='3 == 3'/>

	<Godel name='DerivedContext' ref='BasicContext' post='4 == 4'/>

	<Godel name='ControlOnly' ref='DerivedContext' controlOnly='true'/>

	<StateModel name='SM' initialState='Initial'>
		<Godel ref='DerivedContext' pre='5 == 5'/>

		<State name='Initial'>
			<Godel ref='BasicContext'/>

			<Action type='output'>
				<Godel ref='ControlOnly'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel inv='True == True'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel ref='DerivedContext' post='6 == 6'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel ref='ControlOnly' controlOnly='false'/>
				<DataModel ref='DM'/>
			</Action>
		</State>

	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>

</Peach>
";
			var e = new Engine(null);

			var parser = new Godel.Core.GodelPitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;

			Assert.NotNull(dom);

			Assert.AreEqual(6, dom.godel.Count);

			Assert.AreEqual(dom.godel[0].name, "SM.Initial.Action");
			Assert.AreEqual(dom.godel[0].controlOnly, true);
			Assert.AreEqual(dom.godel[0].inv, "1 == 1");
			Assert.AreEqual(dom.godel[0].pre, "2 == 2");
			Assert.AreEqual(dom.godel[0].post, "4 == 4");

			Assert.AreEqual(dom.godel[1].name, "SM.Initial.Action_1");
			Assert.AreEqual(dom.godel[1].controlOnly, false);
			Assert.AreEqual(dom.godel[1].inv, "True == True");
			Assert.AreEqual(dom.godel[1].pre, null);
			Assert.AreEqual(dom.godel[1].post, null);

			Assert.AreEqual(dom.godel[2].name, "SM.Initial.Action_2");
			Assert.AreEqual(dom.godel[2].controlOnly, false);
			Assert.AreEqual(dom.godel[2].inv, "1 == 1");
			Assert.AreEqual(dom.godel[2].pre, "2 == 2");
			Assert.AreEqual(dom.godel[2].post, "6 == 6");

			Assert.AreEqual(dom.godel[3].name, "SM.Initial.Action_3");
			Assert.AreEqual(dom.godel[3].controlOnly, false);
			Assert.AreEqual(dom.godel[3].inv, "1 == 1");
			Assert.AreEqual(dom.godel[3].pre, "2 == 2");
			Assert.AreEqual(dom.godel[3].post, "4 == 4");

			Assert.AreEqual(dom.godel[4].name, "SM.Initial");
			Assert.AreEqual(dom.godel[4].controlOnly, false);
			Assert.AreEqual(dom.godel[4].inv, "1 == 1");
			Assert.AreEqual(dom.godel[4].pre, "2 == 2");
			Assert.AreEqual(dom.godel[4].post, "3 == 3");

			Assert.AreEqual(dom.godel[5].name, "SM");
			Assert.AreEqual(dom.godel[5].controlOnly, false);
			Assert.AreEqual(dom.godel[5].inv, "1 == 1");
			Assert.AreEqual(dom.godel[5].pre, "5 == 5");
			Assert.AreEqual(dom.godel[5].post, "4 == 4");
		}

		[Test]
		public void SimpleInvariant()
		{
			string xml = @"
<Peach>

	<DataModel name='DM'>
		<String value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel inv='self != None'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			var e = new Engine(null);
			var epeach = new Godel.Core.ExtendPeach(e.context);
			var parser = new Godel.Core.GodelPitParser() { ExtendPeach = epeach };
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.singleIteration = true;
			e.startFuzzing(dom, config);
		}
	}
}

//    [TestFixture]
//    class BasicTests : DataModelCollector
//    {
//        [Test]
//        public void Test1()
//        {
//            // standard test generating odd unicode strings for each <String> element

//            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
//                "<Peach>" +
//                "   <DataModel name=\"TheDataModel\">" +
//                "       <String name=\"data\"/>" +
//                "   </DataModel>" +

//                "   <StateModel name=\"TheState\" initialState=\"Initial\">" +
//                "       <State name=\"Initial\">" +
//                "           <Action type=\"call\" method=\"Init\"/>" +

//                "           <Action type=\"call\" method=\"Login\">" +
//                "				<Param name=\"user\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"user\"/></Data>" +
//                "				</Param>" +
//                "				<Param name=\"pass\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"pass\"/></Data>" +
//                "				</Param>" +
//                "				<Param name=\"token\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"Godel\"/></Data>" +
//                "				</Param>" +
//                "           </Action>" +

//                "           <Action type=\"call\" method=\"PerformAuthedWork\">" +
//                "				<Param name=\"token\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"Godel\"/></Data>" +
//                "				</Param>" +
//                "           </Action>" +

//                "           <Action type=\"call\" method=\"Logout\">" +
//                "				<Param name=\"token\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"Godel\"/></Data>" +
//                "				</Param>" +
//                "           </Action>" +

//                "           <Action type=\"call\" method=\"PerformAuthedWork\">" +
//                "				<Param name=\"token\">" +
//                "					<DataModel ref=\"TheDataModel\"/>" +
//                "					<Data><Field name=\"data\" value=\"Godel\"/></Data>" +
//                "				</Param>" +
//                "           </Action>" +

//                "       </State>" +
//                "   </StateModel>" +

//                "   <Test name=\"Default\">" +
//                "       <StateModel ref=\"TheState\"/>" +
//                "       <Publisher class=\"WebService\">" +
//                "			<Param name=\"Url\" value=\"http://localhost:5903/GodelTestService.svc\"/>" +
//                "			<Param name=\"Service\" value=\"GodelTestService\"/>" +
//                "       <Strategy class=\"Sequential\"/>" +
//                "   </Test>" +
//                "</Peach>";

//            PitParser parser = new PitParser();

//            Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
//            dom.tests[0].includedMutators = new List<string>();
//            dom.tests[0].includedMutators.Add("StringMutator");

//            RunConfiguration config = new RunConfiguration();

//            Engine e = new Engine(null);
//            e.config = config;
//            e.startFuzzing(dom, config);

//            // verify first two values, last two values, and count (= 2379)
//            string val1 = "Peach";
//            string val2 = "abcdefghijklmnopqrstuvwxyz";
//            string val3 = "18446744073709551664";
//            string val4 = "10";

//            Assert.AreEqual(2379, mutations.Count);
//            Assert.AreEqual(val1, (string)mutations[0]);
//            Assert.AreEqual(val2, (string)mutations[1]);
//            Assert.AreEqual(val3, (string)mutations[mutations.Count - 2]);
//            Assert.AreEqual(val4, (string)mutations[mutations.Count - 1]);
//        }
//    }
//}
