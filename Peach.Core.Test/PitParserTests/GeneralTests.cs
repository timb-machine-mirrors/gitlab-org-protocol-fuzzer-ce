﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Core.Test.PitParserTests
{
	[TestFixture]
	class GeneralTests
	{
		//[Test]
		//public void NumberDefaults()
		//{
		//    string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
		//        "	<Defaults>" +
		//        "		<Number size=\"8\" endian=\"big\" signed=\"true\"/>" +
		//        "	</Defaults>" +
		//        "	<DataModel name=\"TheDataModel\">" +
		//        "		<Number name=\"TheNumber\" size=\"8\"/>" +
		//        "	</DataModel>" +
		//        "</Peach>";

		//    PitParser parser = new PitParser();
		//    Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
		//    Number num = dom.dataModels[0][0] as Number;

		//    Assert.IsTrue(num.Signed);
		//    Assert.IsFalse(num.LittleEndian);
		//}

		[Test]
		public void DeepOverride()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"       <Block name=\"TheBlock\">"+
				"		      <String name=\"TheString\" value=\"Hello\"/>" +
				"       </Block>"+
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel\" ref=\"TheDataModel1\">" +
				"		<String name=\"TheBlock.TheString\" value=\"World\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels["TheDataModel"].Count);
			Assert.AreEqual(1, ((DataElementContainer)dom.dataModels["TheDataModel"][0]).Count);
			
			Assert.AreEqual("TheString", ((DataElementContainer)dom.dataModels["TheDataModel"][0])[0].name);
			Assert.AreEqual("World", (string) ((DataElementContainer)dom.dataModels["TheDataModel"][0])[0].DefaultValue);
		}
	}
}
