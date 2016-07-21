using System;
using System.IO;
using System.Reflection;
using System.Web.Http;
using Owin;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Tracing;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.Test.WebProxy.TestTarget
{
	public class TestTargetServer : IWebStatus
	{
		readonly WebStartup _startup;
		IDisposable _server;

		public static string BaseUrl;

		public TestTargetServer()
		{
			_startup = new WebStartup();
		}

		class NullTraceOutputFactory : ITraceOutputFactory
		{
			public TextWriter Create(string outputFile)
			{
				return StreamWriter.Null;
			}
		}

		public static IDisposable StartServer(int port = 8001)
		{
			return new TestTargetServer().Start(port);
		}

		public void Start(int? port)
		{
			throw new NotImplementedException();
		}

		public IDisposable Start(int port = 8001)
		{
			BaseUrl = string.Format("http://127.0.0.1:{0}/", port);
			Uri = new Uri(BaseUrl);

			// Owin adds a TextWriterTraceListener during startup
			// we need to replace it to avoid spewing to console

			var options = new StartOptions(BaseUrl);
			options.Settings.Add(
				typeof(ITraceOutputFactory).FullName,
				typeof(NullTraceOutputFactory).AssemblyQualifiedName
			);

			_server = WebApp.Start(options, _startup.OnStartup);

			return this;
		}

		public Uri Uri
		{
			get;
			private set;
		}

		public void Dispose()
		{
			if (_startup != null)
				_startup.Dispose();

			if (_server != null)
				_server.Dispose();
		}
	}

	public class WebStartup : IDisposable
	{
		public void OnStartup(IAppBuilder app)
		{
			var cfg = new HttpConfiguration();

			cfg.Formatters.JsonFormatter.SerializerSettings = JsonUtilities.GetSettings();

			cfg.MapHttpAttributeRoutes();

			var builder = new ContainerBuilder();

			//builder.RegisterInstance(new Controllers.ValuesController());

			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
			builder.RegisterWebApiFilterProvider(cfg);

			var container = builder.Build();
			cfg.DependencyResolver = new AutofacWebApiDependencyResolver(container);

			app.UseAutofacMiddleware(container);
			app.UseAutofacWebApi(cfg);
			app.UseWebApi(cfg);

		}

		public void Dispose()
		{
		}
	}
}
