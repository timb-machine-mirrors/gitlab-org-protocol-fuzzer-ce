using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.OS.OSX
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[SetUp]
		public void SetUp()
		{
			// NUnit [Platform] attribute doesn't differentiate MacOSX/Linux
			if (Platform.GetOS() != Platform.OS.OSX)
				Assert.Ignore("Only supported on MacOSX");

			DoSetUp();
		}

		[TearDown]
		public void TearDown()
		{
			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}
}
