using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.WebApi;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	class WebApiData : ActionData
	{
		
	}

	[TestFixture]
	[Peach]
	[Quick]
	internal class WebProxyTests
	{
		[Test]
		public void TestCreate()
		{
			var ctx = new RunContext
			{
				config = new RunConfiguration(),
				test = new Peach.Core.Dom.Test()
			};

			var strategy = new WebProxyStrategy(null);

			strategy.Initialize(ctx, null);

			strategy.Finalize(ctx, null);
		}

		[Test]
		public void TestDeserialize()
		{
			const string xml = @"
<WebProxy>
	<Route url='*' />
	<Route url='/foo/bar' mutate='false' swagger='/tmp/swagger/json' baseUrl='google.com' faultOnStatusCodes='500,501' />
</WebProxy>
";

			var obj = XmlTools.Deserialize<WebProxyModel>(new StringReader(xml));

			Assert.NotNull(obj);
			Assert.NotNull(obj.Routes);
			Assert.AreEqual(2, obj.Routes.Count);
			Assert.NotNull(obj.Routes[0]);
			Assert.AreEqual("*", obj.Routes[0].Url);
			Assert.AreEqual(true, obj.Routes[0].Mutate);
			Assert.NotNull(obj.Routes[1]);
			Assert.AreEqual("/foo/bar", obj.Routes[1].Url);
			Assert.AreEqual(false, obj.Routes[1].Mutate);
			Assert.AreEqual("/tmp/swagger/json", obj.Routes[1].Swagger);
			Assert.AreEqual("google.com", obj.Routes[1].BaseUrl);
			Assert.AreEqual(2, obj.Routes[1].FaultOnStatusCodes.Count);
			Assert.AreEqual(500, obj.Routes[1].FaultOnStatusCodes[0]);
			Assert.AreEqual(501, obj.Routes[1].FaultOnStatusCodes[1]);
		}
	}
}
