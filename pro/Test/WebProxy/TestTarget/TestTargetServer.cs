using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using Owin;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Tracing;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Process = System.Diagnostics.Process;

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

		public static IDisposable StartServer(int port = 8002)
		{
			return new TestTargetServer().Start(port);
		}

		public void Start(int? port)
		{
			throw new NotImplementedException();
		}

		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
		// ReSharper disable InconsistentNaming
		private const int ERROR_ACCESS_DENIED = 5;
		private const int ERROR_SHARING_VIOLATION = 32;
		private const int ERROR_ALREADY_EXISTS = 183;
		// ReSharper restore InconsistentNaming

		public IDisposable Start(int port = 8002)
		{
			var added = false;

			while (_server == null)
			{
				try
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

				}
				catch (Exception ex)
				{
					var inner = ex.GetBaseException();

					var lex = inner as HttpListenerException;
					if (lex != null)
					{
						if (lex.ErrorCode == ERROR_ACCESS_DENIED)
						{
							var error = added;

							if (!added)
								error = !UacHelpers.AddUrl(BaseUrl);

							if (!error)
							{
								// UAC reservation added, don't increment port
								added = true;
								continue;
							}

							var sb = new StringBuilder();

							sb.AppendFormat("Access was denied when starts the web server at url '{0}'.", BaseUrl);
							sb.AppendLine();
							sb.AppendLine();
							sb.AppendLine("Please create the url reservations by executing the following");
							sb.AppendLine("from a command prompt with elevated privileges:");
							sb.AppendFormat("{0} {1}", UacHelpers.Command, UacHelpers.GetArguments(BaseUrl));

							throw new PeachException(sb.ToString(), ex);
						}

						// Windows gives ERROR_SHARING_VIOLATION when port in use
						// Windows gives ERROR_ALREADY_EXISTS when two http instances are running
						// Mono raises "Prefix already in use" message
						if (lex.ErrorCode == ERROR_SHARING_VIOLATION ||
						    lex.ErrorCode == ERROR_ALREADY_EXISTS ||
						    lex.Message == "Prefix already in use.")
						{
							throw new PeachException(
								"Unable to start the web server at http://localhost:{0}/ because the port is currently in use.".Fmt(port));
						}
					}

					throw new PeachException("Unable to start the web server: " + inner.Message + ".", ex);
				}
			}

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

	internal static class UacHelpers
	{
		private static readonly string user;

		static UacHelpers()
		{
			var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
			var account = sid.Translate(typeof(NTAccount)) as NTAccount;

			user = account != null ? account.Value : "Everyone";

		}

		public static bool AddUrl(string url)
		{
			try
			{
				using (var p = new Process())
				{
					p.StartInfo = new ProcessStartInfo
					{
						Verb = "runas",
						FileName = Command,
						Arguments = GetArguments(url)
					};

					p.Start();
					p.WaitForExit();

					// 0 == success, 1 == already registered
					return p.ExitCode == 0 || p.ExitCode == 1;
				}
			}
			catch
			{
				return false;
			}
		}

		public static readonly string Command = "netsh";

		public static string GetArguments(string url)
		{
			return "http add urlacl url=\"{0}\" user=\"{1}\"".Fmt(url, user);
		}
	}

	public class WebStartup : IDisposable
	{
		public void OnStartup(IAppBuilder app)
		{
			var cfg = new HttpConfiguration();

			cfg.Formatters.JsonFormatter.SerializerSettings = JsonUtilities.GetSettings();
			cfg.MapHttpAttributeRoutes();
			app.UseWebApi(cfg);
			cfg.EnsureInitialized(); 
		}

		public void Dispose()
		{
		}
	}
}
