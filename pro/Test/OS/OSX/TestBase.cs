using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.Test.OS.OSX
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();

			// Peach.Core.dll
			ClassLoader.LoadAssembly(typeof(ClassLoader).Assembly);

			// Peach.Pro.dll
			ClassLoader.LoadAssembly(typeof(BaseProgram).Assembly);

			// Peach.Pro.Test.OS.OSX.dll
			ClassLoader.LoadAssembly(Assembly.GetExecutingAssembly());
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
