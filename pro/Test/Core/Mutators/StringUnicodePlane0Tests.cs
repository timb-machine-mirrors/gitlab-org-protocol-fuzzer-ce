using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core.Dom;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	class StringUnicodePlane0Tests : StringMutatorTester
	{
		public StringUnicodePlane0Tests()
			: base("StringUnicodePlane0")
		{
			// Verify fuzzed string lengths for sequential
			VerifyLength = true;
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
