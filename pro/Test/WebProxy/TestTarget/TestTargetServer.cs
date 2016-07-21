using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Owin;
using Peach.Pro.Test.WebProxy.TestTarget;
using Owin;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Tracing;
using System.Web.Http;
using Peach.Pro.Core;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;

namespace Peach.Pro.Test.WebProxy.TestTarget
{
	public class TestTargetServer
	{
		class NullTraceOutputFactory : ITraceOutputFactory
		{
			public TextWriter Create(string outputFile)
			{
				return StreamWriter.Null;
			}
		}

		public static string BaseUrl = "http://localhost:9000";
		public static IDisposable StartServer()
		{
			//return WebApp.Start<TestTargetServer>(url: BaseUrl);

			var options = new StartOptions(BaseUrl);
			options.Settings.Add(
				typeof(ITraceOutputFactory).FullName,
				typeof(NullTraceOutputFactory).AssemblyQualifiedName
			);

			return WebApp.Start(options, new TestTargetServer().OnStartup);
		}

		private void OnStartup(IAppBuilder appBuilder)
		{
			// Configure Web API for self-host. 
			var config = new HttpConfiguration();

			config.Formatters.JsonFormatter.SerializerSettings = JsonUtilities.GetSettings();
			config.MapHttpAttributeRoutes();

			var container = new SimpleInjector.Container();
			container.Options.DefaultScopedLifestyle = new WebApiRequestLifestyle();
			container.RegisterSingleton(new ValuesController());
			container.RegisterWebApiControllers(config);
			container.Verify();

			config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);

			appBuilder.UseWebApi(config);
		}
	}

	[RoutePrefix(Prefix)]
	public class ValuesController : ApiController
	{
		public const string Prefix = "/api/values";

		// GET api/values 
		[Route("")]
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/values/5 
		[Route("{id}")]
		public string Get(int id)
		{
			return "value";
		}

		// POST api/values 
		[Route("")]
		public void Post([FromBody]string value)
		{
		}

		// PUT api/values/5 
		[Route("{id}")]
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/values/5 
		[Route("{id}")]
		public void Delete(int id)
		{
		}
	} 
}
