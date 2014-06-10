using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Peach.Core;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;

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

			var defs1 = XmlTools.Deserialize<PitDefines>(new StringReader("<PitDefines/>"));
			Assert.AreEqual(0, defs1.Platforms.Count);

			var defs2 = XmlTools.Deserialize<PitDefines>(new StringReader("<PitDefines><All/></PitDefines>"));
			Assert.AreEqual(1, defs2.Platforms.Count);
			Assert.AreEqual(Platform.OS.All, defs2.Platforms[0].Platform);
			Assert.AreEqual(0, defs2.Platforms[0].Defines.Count);

			var defs = XmlTools.Deserialize<PitDefines>(new StringReader(xml));
			Assert.NotNull(defs);
		}

		[Test]
		public void TestTypes()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<String key='key0' value='value0' name='name0' description='description0'/>
		<Ipv4 key='key1' value='value1' name='name1' description='description1'/>
		<Ipv6 key='key2' value='value2' name='name2' description='description2'/>
		<Iface key='key3' value='value3' name='name3' description='description3'/>
		<Strategy key='key4' value='value4' name='name4' description='description4'/>
		<Hwaddr key='key5' value='value5' name='name5' description='description5'/>
		<Range key='key6' value='value6' name='name6' description='description5' min='0' max='100'/>
		<Enum key='key6' value='value6' name='name6' description='description5' enumType='System.IO.FileAccess'/>
	</All>
</PitDefines>
";

			var defs = PitDefines.Parse(new StringReader(xml));
			Assert.NotNull(defs);
		}

		[Test]
		public void Schema()
		{
			var schema = XmlTools.GetSchema(typeof(PitDefines));
			var sb = new StringBuilder();
			var wr = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true });
			schema.Write(wr);
			var asStr = sb.ToString();
			Assert.NotNull(asStr);
		}

		[Test]
		public void InvalidXmlUri()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines></Bad>";

			var tmp = Path.GetTempFileName();
			File.WriteAllText(tmp, xml);

			try
			{
				XmlTools.Deserialize<PitDefines>(tmp);
				Assert.Fail("Should throw");
			}
			catch (PeachException ex)
			{
				Assert.NotNull(ex);
			}
			finally
			{
				File.Delete(tmp);
			}
		}

		[Test]
		public void InvalidXmlString()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines></Bad>";

			try
			{
				using (var rdr = new StringReader(xml))
				{
					XmlTools.Deserialize<PitDefines>(rdr);
					Assert.Fail("Should throw");
				}
			}
			catch (PeachException ex)
			{
				Assert.NotNull(ex);
			}
		}

		[Test]
		public void IncompleteXmlUri()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines></Bad>";

			var tmp = Path.GetTempFileName();
			File.WriteAllText(tmp, xml);

			try
			{
				XmlTools.Deserialize<PitDefines>(tmp);
				Assert.Fail("Should throw");
			}
			catch (PeachException ex)
			{
				Assert.NotNull(ex);
			}
			finally
			{
				File.Delete(tmp);
			}
		}

		[Test]
		public void IncompleteXmlString()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<Enum key='key' value='value' name='name' description='description'/>
	</All>
</PitDefines>";

			try
			{
				using (var rdr = new StringReader(xml))
				{
					XmlTools.Deserialize<PitDefines>(rdr);
					Assert.Fail("Should throw");
				}
			}
			catch (PeachException ex)
			{
				Assert.NotNull(ex);
			}
		}
	}


}
