using System.Reflection;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebApi
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[SetUp]
		public void SetUp()
		{
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
