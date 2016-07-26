using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Titanium.Web.Proxy.EventArguments;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebApiPathTests
	{
		private WebApiEndPoint _endPoint = null;

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
