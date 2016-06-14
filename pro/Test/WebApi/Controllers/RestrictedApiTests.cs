using System.Net;
using System.Net.Http;
using System.Threading;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebApi.Controllers
{
	[TestFixture]
	[Quick]
	class RestrictedApiTests : ControllerTestsBase
	{
		[Test]
		public void NoEula()
		{
			_license.Setup(x => x.IsValid).Returns(true);

			using (var request = new HttpRequestMessage(HttpMethod.Get, "/p/jobs"))
			using (var response = _server.HttpClient.SendAsync(request, CancellationToken.None).Result)
			{
				Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
			}
		}
	}
	
}