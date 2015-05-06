using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.WebApi;

namespace Peach.Pro.Test.WebApi.Controllers
{
	[TestFixture]
	[Quick]
	class EulaServiceTests
	{
		[Test]
		public void NoEula()
		{
			var bootstrapper = new Bootstrapper(null);
			var browser = new Browser(bootstrapper);

			var result = browser.Get("/", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.SeeOther, result.StatusCode);
		}
	}
	
}