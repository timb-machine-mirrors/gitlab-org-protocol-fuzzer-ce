using System;
using System.Collections.Generic;
using Peach.Core.Dom;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
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
