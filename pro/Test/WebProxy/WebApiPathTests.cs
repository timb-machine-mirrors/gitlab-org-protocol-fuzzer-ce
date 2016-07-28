using System.Linq;
using NUnit.Framework;
using Peach.Pro.Core.WebApi;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebApiPathTests
	{
		private WebApiEndPoint _endPoint;

		[OneTimeSetUp]
		public virtual void Init()
		{
			var swagger = BaseTester.GetValuesJson();
			_endPoint = SwaggerToWebApi.Convert(swagger);
		}

		[Test]
		public void TestSwaggerPath()
		{
			var path = _endPoint.Paths.First(p => p.Path == "/api/values/{id}");
			var match = path.PathRegex().Match("/api/values/5?foo=bar");
			Assert.IsTrue(match.Success);
			Assert.AreEqual("5", match.Groups[1].Value);
			Assert.AreEqual("5", match.Groups["id"].Value);

			match = path.PathRegex().Match("/unknown/api/values/5?foo=bar");
			Assert.IsFalse(match.Success);
		}
	}
}
