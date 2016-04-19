using System.Reflection;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.OS.OSX
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();
		}

		[OneTimeTearDown]
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
