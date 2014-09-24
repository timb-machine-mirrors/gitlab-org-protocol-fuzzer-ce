using System;

using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class StringAsciiRandomTests : StringMutatorTester
	{
		public StringAsciiRandomTests()
			: base("StringAsciiRandom")
		{
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
