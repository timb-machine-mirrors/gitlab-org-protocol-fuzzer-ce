using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core.Dom;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	class StringUnicodePlane1Tests : StringMutatorTester
	{
		public StringUnicodePlane1Tests()
			: base("StringUnicodePlane1")
		{
		}

		protected override IEnumerable<StringType> InvalidEncodings
		{
			get
			{
				yield return StringType.ascii;
			}
		}

		[Test]
		public void TestSupported()
		{
			RunSupported();
		}

		[Test]
		public void TestSequential()
		{
			RunSequential();
		}

		[Test]
		public void TestRandom()
		{
			RunRandom();
		}
	}
}
