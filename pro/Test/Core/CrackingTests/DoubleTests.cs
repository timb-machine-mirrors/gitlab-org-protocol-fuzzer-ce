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
using Peach.Core.Cracker;
using Peach.Core.IO;

namespace Peach.Core.Test.CrackingTests
{
	[TestFixture]
	[Category("Peach")]
	class DoubleTests
	{
		[Test]
		public void CrackDouble1()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' />
                	</DataModel>
                </Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var bs = new BitStream(BitConverter.GetBytes(1.0));
			bs.Seek(0, SeekOrigin.Begin);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1.0, (double)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void CrackDouble2()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' endian='big'/>
                	</DataModel>
                </Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var bs = new BitStream(BitConverter.GetBytes(3.0386519416174186E-319d));
			bs.Seek(0, SeekOrigin.Begin);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1.0, (double)dom.dataModels[0][0].DefaultValue);
		}
	}
}
