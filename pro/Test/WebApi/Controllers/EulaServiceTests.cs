using System.Net;
using System.Net.Http;
using System.Threading;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebApi.Controllers
{
	[TestFixture]
	[Quick]
	class EulaServiceTests : ControllerTestsBase
	{
		[SetUp]
		public void SetUp()
		{
			_license.Setup(x => x.IsValid).Returns(true);

			DoSetUp();
		}

		[TearDown]
		public void TearDown()
		{
			DoTearDown();
		}

		[Test]
		public void NoEula()
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, "/p/jobs"))
			using (var response = _client.SendAsync(request, CancellationToken.None).Result)
			{
				Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
			}
		}
	}
	
}