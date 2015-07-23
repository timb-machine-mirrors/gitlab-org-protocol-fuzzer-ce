using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;

namespace Peach.Pro.Test.Core
{
	#region Helper Classes

	/// <summary>
	/// All child elements occur many times
	/// </summary>
	[XmlRoot("Root")]
	public class ManyChildren
	{
		public abstract class ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class ChildOne : ChildElement
		{
		}

		public class ChildTwo : ChildElement
		{
		}

		public class ChildThree : ChildElement
		{
		}

		public class ChildFour : ChildElement
		{
		}

		[XmlElement("ChildOne", Type = typeof(ChildOne))]
		[XmlElement("ChildTwo", Type = typeof(ChildTwo))]
		[XmlElement("ChildThree", Type = typeof(ChildThree))]
		public List<ChildElement> Children { get; set; }

		[XmlElement("ChildFour")]
		public List<ChildFour> Fours { get; set; }
	}

	/// <summary>
	/// All children occur a single time
	/// </summary>
	[XmlRoot("Root")]
	public class SingleChildren
	{
		public class Child
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		[XmlElement("ChildOne")]
		public Child ChildOne { get; set; }

		[XmlElement("ChildTwo")]
		public Child ChildTwo { get; set; }
	}

	/// <summary>
	/// ChildOne and ChildTwo occur many times
	/// ChildThree and ChildFour occur a single time
	/// </summary>
	[XmlRoot("Root")]
	public class MixedChildren
	{
		public abstract class ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class ChildOne : ChildElement
		{
		}

		public class ChildTwo : ChildElement
		{
		}

		public class ChildThree : ChildElement
		{
		}

		public class ChildFour : ChildElement
		{
		}

		[XmlElement("ChildOne", Type = typeof(ChildOne))]
		[XmlElement("ChildTwo", Type = typeof(ChildTwo))]
		public List<ChildElement> Children { get; set; }

		[XmlElement("ChildThree")]
		public ChildThree Three { get; set; }

		[XmlElement("ChildFour")]
		public ChildFour Four { get; set; }
	}

	#endregion

	[TestFixture]
	[Quick]
	class XmlToolsTests
	{
		static T Deserialize<T>(string xml)
		{
			var rdr = new StringReader(xml);
			var ret = XmlTools.Deserialize<T>(rdr);

			return ret;
		}

		[Test]
		public void TestSingle()
		{
			var obj = Deserialize<SingleChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.Null(obj.ChildOne);
			Assert.Null(obj.ChildTwo);

			obj = Deserialize<SingleChildren>(@"
<Root>
	<ChildOne name='one'/>
	<ChildTwo name='two'/>
</Root>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.ChildOne);
			Assert.AreEqual("one", obj.ChildOne.Name);
			Assert.NotNull(obj.ChildTwo);
			Assert.AreEqual("two", obj.ChildTwo.Name);

			obj = Deserialize<SingleChildren>(@"
<Root>
	<ChildTwo name='two'/>
	<ChildOne name='one'/>
</Root>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.ChildOne);
			Assert.AreEqual("one", obj.ChildOne.Name);
			Assert.NotNull(obj.ChildTwo);
			Assert.AreEqual("two", obj.ChildTwo.Name);

			var ex = Assert.Throws<PeachException>(() =>
				Deserialize<SingleChildren>(@"
<Root>
	<ChildTwo name='aaa'/>
	<ChildTwo name='ccc'/>
	<ChildOne name='one'/>
</Root>"));

			Assert.That(ex.Message, Is.StringContaining("failed to validate"));
		}

		[Test]
		public void TestMany()
		{
			var obj = Deserialize<ManyChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(0, obj.Children.Count);
			Assert.NotNull(obj.Fours);
			Assert.AreEqual(0, obj.Fours.Count);

			obj = Deserialize<ManyChildren>(@"
<Root>
	<ChildFour name='four1' />
	<ChildThree name='three1'/>
	<ChildTwo name='two1'/>
	<ChildOne name='one1'/>
	<ChildOne name='one2'/>
	<ChildTwo name='two2'/>
	<ChildThree name='three2'/>
	<ChildFour name='four2' />
</Root>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(6, obj.Children.Count);
			Assert.NotNull(obj.Fours);
			Assert.AreEqual(2, obj.Fours.Count);

			Assert.AreEqual("three1", obj.Children[0].Name);
			Assert.AreEqual("two1", obj.Children[1].Name);
			Assert.AreEqual("one1", obj.Children[2].Name);
			Assert.AreEqual("one2", obj.Children[3].Name);
			Assert.AreEqual("two2", obj.Children[4].Name);
			Assert.AreEqual("three2", obj.Children[5].Name);

			Assert.AreEqual("four1", obj.Fours[0].Name);
			Assert.AreEqual("four2", obj.Fours[1].Name);
		}

		[Test]
		public void TestMixed()
		{
			var obj = Deserialize<MixedChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(0, obj.Children.Count);
			Assert.Null(obj.Three);
			Assert.Null(obj.Four);

			obj = Deserialize<MixedChildren>(@"
<Root>
	<ChildFour name='four1' />
	<ChildThree name='three1'/>
	<ChildTwo name='two1'/>
	<ChildOne name='one1'/>
	<ChildOne name='one2'/>
	<ChildTwo name='two2'/>
	<ChildThree name='three2'/>
	<ChildFour name='four2' />
</Root>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(4, obj.Children.Count);
			Assert.NotNull(obj.Three);
			Assert.NotNull(obj.Four);

			Assert.AreEqual("two1", obj.Children[0].Name);
			Assert.AreEqual("one1", obj.Children[1].Name);
			Assert.AreEqual("one2", obj.Children[2].Name);
			Assert.AreEqual("two2", obj.Children[3].Name);

			Assert.AreEqual("three1", obj.Three.Name);
			Assert.AreEqual("four1", obj.Four.Name);
		}
	}
}