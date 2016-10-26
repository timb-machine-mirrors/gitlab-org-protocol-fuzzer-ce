using System.Linq;
using System.Web;
using NUnit.Framework;
using Peach.Pro.Core.WebApi;
using Titanium.Web.Proxy;
using Assert = NUnit.Framework.Assert;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebApiPathTests
	{
		private WebApiEndPoint _endPoint;

		[OneTimeSetUp]
		public virtual void Init()
		{
			var swagger = BaseRunTester.GetValuesJson();
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

		[Test]
		public void TestQueryStringParse()
		{
			var uri = new Uri("http://host.com/foo?/bar&/baz");
			var query = uri.Query;
			var res = HttpUtility.ParseQueryString(query);
			Assert.AreEqual(1, res.Keys.Count);
			Assert.AreEqual(null, res.Keys[0]);
			Assert.AreEqual(new[] {"/bar", "/baz"}, res.GetValues(0));
		}
	}
}
