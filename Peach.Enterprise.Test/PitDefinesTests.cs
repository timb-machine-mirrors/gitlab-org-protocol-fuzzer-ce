using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Peach.Core;

namespace Peach.Enterprise.Test
{
	[TestFixture]
	public class PitDefinesTests
	{
		[Test]
		public void TestParse()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
	</All>

	<None>
	</None>

	<Windows>
	</Windows>

	<Linux>
	</Linux>

	<OSX>
	</OSX>

	<Unix>
	</Unix>
</PitDefines>
";

			var defs1 = PitDefines.Deserialize(new StringReader("<PitDefines/>"));
			Assert.AreEqual(0, defs1.Platforms.Count);

			var defs2 = PitDefines.Deserialize(new StringReader("<PitDefines><All/></PitDefines>"));
			Assert.AreEqual(1, defs2.Platforms.Count);
			Assert.AreEqual(Platform.OS.All, defs2.Platforms[0].Platform);
			Assert.AreEqual(0, defs2.Platforms[0].Defines.Count);

			var defs = PitDefines.Deserialize(new StringReader(xml));
			Assert.NotNull(defs);
		}

		[Test]
		public void TestTypes()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<Define key='key0' value='value0' name='name0' description='description0' type='string'/>
		<Define key='key1' value='value1' name='name1' description='description1' type='ipv4'/>
		<Define key='key2' value='value2' name='name2' description='description2' type='ipv6'/>
		<Define key='key3' value='value3' name='name3' description='description3' type='iface'/>
		<Define key='key4' value='value4' name='name4' description='description4' type='strategy'/>
		<Define key='key5' value='value5' name='name5' description='description5' type='hwaddr'/>
		<Define key='key6' value='value6' name='name6' description='description5' type='port'/>
	</All>
</PitDefines>
";

			var defs = PitDefines.Parse(new StringReader(xml));
			Assert.NotNull(defs);

			//var cfgs = new WebServices.PitDatabase().MakeConfig(defs);
			//Assert.NotNull(cfgs);
		}
	}
}
