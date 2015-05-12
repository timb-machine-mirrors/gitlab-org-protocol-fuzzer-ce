using System.Reflection;
using NUnit.Framework;
using Peach.Core.Test;

namespace Godel.Tests
{
	[SetUpFixture]
	class TestBase : SetUpFixture
	{
	}

	[TestFixture]
	[Quick]
	class CommonTests : TestFixture
	{
		public CommonTests() : base(Assembly.GetExecutingAssembly()) { }
	}
}
