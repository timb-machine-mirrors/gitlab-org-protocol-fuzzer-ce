using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.OS.OSX
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		SingleInstance _si;

		[SetUp]
		public void SetUp()
		{
			// NUnit [Platform] attribute doesn't differentiate MacOSX/Linux
			if (Platform.GetOS() != Platform.OS.OSX)
				Assert.Ignore("Only supported on MacOSX");

			DoSetUp();

			// Ensure only 1 instance of the platform tests runs at a time
			_si = SingleInstance.CreateInstance(Assembly.GetExecutingAssembly().FullName);
			_si.Lock();
		}

		[TearDown]
		public void TearDown()
		{
			if (_si != null)
			{
				_si.Dispose();
				_si = null;
			}

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
