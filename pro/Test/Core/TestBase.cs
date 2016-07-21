using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Runtime;

// This assembly contains Peach plugins
[assembly: PluginAssembly]

namespace Peach.Pro.Test.Core
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		TempDirectory _tmpDir;

		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();

			BaseProgram.Initialize();

			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();

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
