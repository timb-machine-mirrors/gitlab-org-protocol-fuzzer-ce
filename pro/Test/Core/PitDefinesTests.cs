using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;

namespace Peach.Pro.Test.Core
{
	[TestFixture] [Category("Peach")]
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
		<String   key='key0' value='value0' name='name0' description='description0'/>
		<Ipv4     key='key1' value='value1' name='name1' description='description1'/>
		<Ipv6     key='key2' value='value2' name='name2' description='description2'/>
		<Iface    key='key3' value='value3' name='name3' description='description3'/>
		<Strategy key='key4' value='value4' name='name4' description='description4'/>
		<Hwaddr   key='key5' value='value5' name='name5' description='description5'/>
		<Range    key='key6' value='value6' name='name6' description='description6' min='0' max='100'/>
		<Enum     key='key7' value='value7' name='name7' description='description7' enumType='System.IO.FileAccess'/>
		<Define   key='key8' value='value8' name='name8' description='description8'/>
	</All>
</PitDefines>
";

			var defs = PitDefines.Parse(new StringReader(xml));
			Assert.NotNull(defs);
			Assert.AreEqual(defs[0].ConfigType, ParameterType.String);
			Assert.AreEqual(defs[1].ConfigType, ParameterType.Ipv4);
			Assert.AreEqual(defs[2].ConfigType, ParameterType.Ipv6);
			Assert.AreEqual(defs[3].ConfigType, ParameterType.Iface);
			Assert.AreEqual(defs[4].ConfigType, ParameterType.Enum);
			Assert.AreEqual(defs[5].ConfigType, ParameterType.Hwaddr);
			Assert.AreEqual(defs[6].ConfigType, ParameterType.Range);
			Assert.AreEqual(defs[7].ConfigType, ParameterType.Enum);
			Assert.AreEqual(defs[8].ConfigType, ParameterType.User);
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
		public void TestEvaluate()
		{
			var src = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("SamplePath", "##PitLibraryPath##/Samples"),
				new KeyValuePair<string, string>("PitLibraryPath", "Peach/Pits")
			};

			var dst = PitDefines.Evaluate(src);

			Assert.AreEqual(2, dst.Count);
			Assert.AreEqual("SamplePath", dst[0].Key);
			Assert.AreEqual("Peach/Pits/Samples", dst[0].Value);
			Assert.AreEqual("PitLibraryPath", dst[1].Key);
			Assert.AreEqual("Peach/Pits", dst[1].Value);

			src.Clear();
			src.Add(new KeyValuePair<string, string>("k1", "##k2##"));
			src.Add(new KeyValuePair<string, string>("k2", "##k3##-2"));
			src.Add(new KeyValuePair<string, string>("k3", "##k4##-3"));
			src.Add(new KeyValuePair<string, string>("k4", "##k5##/##k5##"));
			src.Add(new KeyValuePair<string, string>("k5", "foo"));
			src.Add(new KeyValuePair<string, string>("k6", "##k2##-##k3##"));

			dst = PitDefines.Evaluate(src);

			Assert.AreEqual(6, dst.Count);
			Assert.AreEqual("k1", dst[0].Key);
			Assert.AreEqual("foo/foo-3-2", dst[0].Value);
			Assert.AreEqual("k2", dst[1].Key);
			Assert.AreEqual("foo/foo-3-2", dst[1].Value);
			Assert.AreEqual("k3", dst[2].Key);
			Assert.AreEqual("foo/foo-3", dst[2].Value);
			Assert.AreEqual("k4", dst[3].Key);
			Assert.AreEqual("foo/foo", dst[3].Value);
			Assert.AreEqual("k5", dst[4].Key);
			Assert.AreEqual("foo", dst[4].Value);
			Assert.AreEqual("k6", dst[5].Key);
			Assert.AreEqual("foo/foo-3-2-foo/foo-3", dst[5].Value);

			src.Clear();
			src.Add(new KeyValuePair<string, string>("k1", "##missing##"));
			dst = PitDefines.Evaluate(src);

			Assert.AreEqual(src, dst);
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
