using System;
using Peach.Core;
using Peach.Core.Mutators;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class ArrayEdgeCaseTests : DataModelCollector
	{
		// TODO: Test we hit +/- N around each edge case
		// TODO: Test the hint works as well
		// TODO: Ensure if data element remove fires that it doesn't screw up expansion
		// TODO: Test the point of insertion/removal moves around with each mutation
		// TODO: Test count relation overflow

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ArrayEdgeCase");

			var array = new Dom.Array("Array");
			array.OriginalElement = new Dom.String("Str");
			array.ExpandTo(0);

			// Empty array can be expanded
			Assert.True(runner.IsSupported(array));

			// Single element array can be expanded
			array.ExpandTo(1);
			Assert.True(runner.IsSupported(array));

			// Anything > 1 element is expandable
			array.ExpandTo(2);
			Assert.True(runner.IsSupported(array));

			array.ExpandTo(10);
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}
	}
}
