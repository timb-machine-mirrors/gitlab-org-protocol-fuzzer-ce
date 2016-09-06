using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using Owin;
using Peach.Pro.Core;
using Peach.Pro.WebApi2;

namespace Peach.Pro.Test.WebProxy.TestTarget
{
	public class TestTargetServer : BaseWebServer
	{
		private class Startup : IWebStartup
		{
			private class RouteProvider : DefaultDirectRouteProvider
			{
				private readonly Assembly _thisAsm = Assembly.GetExecutingAssembly();

				public override IReadOnlyList<RouteEntry> GetDirectRoutes(
					HttpControllerDescriptor controllerDescriptor,
					IReadOnlyList<HttpActionDescriptor> actionDescriptors,
					IInlineConstraintResolver constraintResolver)
				{
					if (controllerDescriptor.ControllerType.Assembly != _thisAsm)
						return new RouteEntry[0];

					var ret = base.GetDirectRoutes(controllerDescriptor, actionDescriptors, constraintResolver);
					return ret;
				}
			}

			public void Dispose()
			{
			}

			public void OnStartup(IAppBuilder app)
			{
				var cfg = new HttpConfiguration();

				cfg.Formatters.JsonFormatter.SerializerSettings = JsonUtilities.GetSettings();
				cfg.MapHttpAttributeRoutes(new RouteProvider());
				app.UseWebApi(cfg);
				cfg.EnsureInitialized();
			}
		}

		public TestTargetServer()
			: base(new Startup())
		{
		}

		public static TestTargetServer StartServer()
		{
			var ret = new TestTargetServer();

			try
			{
				ret.Start(8002, true);
				return ret;
			}
			catch (Exception)
			{
				ret.Dispose();
				throw;
			}
		}
	}
}
