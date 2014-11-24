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

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture] [Category("Peach")]
	class RelationCountTests
	{
		[Test]
		public void BasicTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.NotNull(dom.dataModels[0].Value);

			Number num = dom.dataModels[0][0] as Number;

			Assert.AreEqual(1, (int)num.InternalValue);

			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			array.OriginalElement = array[0];

			array.Add(new Peach.Core.Dom.String("Child2") { DefaultValue = new Variant("2") });
			array.Add(new Peach.Core.Dom.String("Child3") { DefaultValue = new Variant("3") });

			Assert.AreEqual(3, (int)num.InternalValue);
			Assert.AreEqual("123", ASCIIEncoding.ASCII.GetString(array.Value.ToArray()));
		}

		[Test]
		public void ExpressionSetTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" expressionSet=\"count + 1\" />" +
				"		</Number>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);

			Assert.AreEqual(new byte[] { 2, (byte)'1' }, val.ToArray());

			Number num = dom.dataModels[0][0] as Number;

			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			array.OriginalElement = array[0];

			array.Add(new Peach.Core.Dom.String("Child2") { DefaultValue = new Variant("2") });
			array.Add(new Peach.Core.Dom.String("Child3") { DefaultValue = new Variant("3") });

			Assert.AreEqual(4, (int)num.InternalValue);
			Assert.AreEqual("123", ASCIIEncoding.ASCII.GetString(array.Value.ToArray()));
		}
	}
}

// end
