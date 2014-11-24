using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core.Dom;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	class StringUnicodeFormatCharactersTests : StringMutatorTester
	{
		public StringUnicodeFormatCharactersTests()
			: base("StringUnicodeFormatCharacters")
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
