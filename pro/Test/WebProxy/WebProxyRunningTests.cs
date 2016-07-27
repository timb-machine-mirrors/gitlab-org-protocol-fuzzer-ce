using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Titanium.Web.Proxy.EventArguments;
using ASCIIEncoding = Peach.Core.ASCIIEncoding;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRunningTests : BaseRunTester
	{
		[Test]
		public void TestOnRequest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Test name=""Default"">
		<WebProxy>
			<Route url='*' />
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy' />
	</Test>
</Peach>";

			//RunEngine(xml);
			Task.Run(() =>
			{
				try
				{
					RunEngine(xml);
				}
				catch (Exception)
				{
					System.Diagnostics.Debugger.Break();
				}
			});

			Thread.Sleep(2000);

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}
	}
}
