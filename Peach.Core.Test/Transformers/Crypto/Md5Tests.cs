﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Core.Test.Transformers.Crypto
{
    [TestFixture]
    class Md5Tests
    {
        byte[] testValue = null;

        [Test]
        public void Test1()
        {
            // standard test

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <Block name=\"TheBlock\">" +
                "           <Transformer class=\"Md5\"/>" +
                "           <Blob name=\"Data\" value=\"Hello\"/>" +
                "       </Block>" +
                "   </DataModel>" +

                "   <StateModel name=\"TheState\" initialState=\"Initial\">" +
                "       <State name=\"Initial\">" +
                "           <Action type=\"output\">" +
                "               <DataModel ref=\"TheDataModel\"/>" +
                "           </Action>" +
                "       </State>" +
                "   </StateModel>" +

                "   <Test name=\"TheTest\">" +
                "       <StateModel ref=\"TheState\"/>" +
                "       <Publisher class=\"Stdout\"/>" +
                "   </Test>" +

                "   <Run name=\"DefaultRun\">" +
                "       <Test ref=\"TheTest\"/>" +
                "   </Run>" +
                "</Peach>";

            PitParser parser = new PitParser();

            Dom.Dom dom = parser.asParser(new Dictionary<string, string>(), new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            RunConfiguration config = new RunConfiguration();
            config.singleIteration = true;

            Dom.Action.Finished += new ActionFinishedEventHandler(Action_FinishedTest);

            Engine e = new Engine(null);
            e.config = config;
            e.startFuzzing(dom, config);

            // verify values
            // -- this is the pre-calculated result from Peach2.3 on the blob: "Hello"
            byte[] precalcResult = new byte[] { 0x8B, 0x1A, 0x99, 0x53, 0xC4, 0x61, 0x12, 0x96, 0xA8, 0x27, 0xAB, 0xF8, 0xC4, 0x78, 0x04, 0xD7 };
            Assert.AreEqual(testValue, precalcResult);

            // reset
            testValue = null;
        }

        void Action_FinishedTest(Dom.Action action)
        {
            testValue = action.dataModel[0].Value.Value;
        }
    }
}

// end
