//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using NUnit.Framework;
//using NUnit.Framework.Constraints;
//using Peach.Core;
//using Peach.Core.Dom;
//using Peach.Core.Analyzers;
//using Peach.Core.IO;

//namespace Godel.Tests
//{
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
