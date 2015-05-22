using System.Reflection;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebApi
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
