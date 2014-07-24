using System;
using Peach.Core;
using Peach.Core.Dom;
using NUnit.Framework;

namespace Peach.Core.Test
{
	class Parent : INamed
	{
		public string name { get; set; }
	}

	class Child : INamed, IOwned<Parent>
	{
		public string name { get; set; }
		public Parent parent { get; set; }
	}

	[TestFixture]
	public class CollectionTests
	{
		[Test]
		public void TestAdd()
		{
			var p = new Parent() { name = "parent" };
			var c1 = new Child() { name = "child1" };
			var c2 = new Child() { name = "child2" };

			var items = new OwnedCollection<Parent, Child>(p);

			Assert.Null(c1.parent);
			Assert.Null(c2.parent);

			items.Add(c1);
			Assert.AreEqual(p, c1.parent);

			items[0] = c2;
			Assert.Null(c1.parent);
			Assert.AreEqual(p, c2.parent);

			items.RemoveAt(0);
			Assert.Null(c2.parent);
		}
	}
}
