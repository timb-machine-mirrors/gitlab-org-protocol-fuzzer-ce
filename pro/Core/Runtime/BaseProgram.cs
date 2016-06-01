﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core.License;
using Peach.Pro.Core.WebServices;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.Runtime
{
	public interface IWebStatus : IDisposable
	{
		/// <summary>
		/// Starts the web server.
		/// </summary>
		/// <param name="port">If not specified an unused port will be picked.</param>
		void Start(int? port);

		Uri Uri { get; }
	}

	public abstract class BaseProgram
	{
		// PitLibraryPath, IJobMonitor
		public Func<ILicense, string, IJobMonitor, IWebStatus> CreateWeb { get; set; }

		protected OptionSet _options;
		protected int _verbosity;
		protected int? _webPort;
		protected ILicense _license;
		protected string _pluginsPath;

		protected void PrepareLicensing(string pitLibraryPath)
		{
			ResourceRoot root = null;
			if (pitLibraryPath != null)
				root = ResourceRoot.GetDefault(Path.GetFullPath(pitLibraryPath));

			if (root == null)
			{
				_license = new PortableLicense(null, null);
			}
			else
			{
				var manifest = PitResourceLoader.LoadManifest(root);
				var secrets = PitResourceLoader.LoadManifest(new ResourceRoot
				{
					Assembly = Assembly.GetExecutingAssembly(),
					Prefix = "Peach.Pro.Core.Resources"
				});
				_license = new PortableLicense(manifest, secrets);
			}
		}

		void LoadAssemblies()
		{
			// Peach.Core.dll
			ClassLoader.LoadAssembly(typeof(ClassLoader).Assembly);

			// Peach.Pro.dll
			ClassLoader.LoadAssembly(Assembly.GetExecutingAssembly());

			var path =
				_pluginsPath ??
				Utilities.GetUserConfig().AppSettings.Settings.Get("Plugins") ??
				Utilities.GetAppResourcePath("Plugins");
			ClassLoader.LoadPlugins(Path.GetFullPath(path));
		}

		private static Version ParseMonoVersion(string str)
		{
			// Example version string:
			// 3.2.8 (Debian 3.2.8+dfsg-4ubuntu1)

			var idx = str.IndexOf(' ');
			if (idx < 0)
				return null;

			var part = str.Substring(0, idx);

			Version ret;
			Version.TryParse(part, out ret);

			return ret;
		}

		private static bool SupportedKernel()
		{
			if (Platform.GetOS() != Platform.OS.Linux)
				return true;

			string osrelease;

			try
			{
				osrelease = File.ReadAllText("/proc/sys/kernel/osrelease");
			}
			catch
			{
				return true;
			}

			var m = Regex.Match(osrelease, @"^([\d\.]+)-(\d+)-(.*)$", RegexOptions.Multiline);

			if (!m.Success)
				return true;

			var rev = int.Parse(m.Groups[2].Value);

			// SIGSEGVs when running certain workloads on multi-cpu VMs.
			// https://bugs.launchpad.net/ubuntu/+source/linux/+bug/1450584
			if (m.Groups[1].Value == "3.13.0" && rev >= 48 && rev <= 54)
			{
				Console.WriteLine("Kernel version {0} is incompatible with peach.", m.Value);
				Console.WriteLine("Please upgrade your kernel to version 3.13.0-55 or newer.");
				return false;
			}

			return true;
		}

		protected virtual bool VerifyCompatibility()
		{
			if (!SupportedKernel())
				return false;

			var type = Type.GetType("Mono.Runtime");

			// If we are not on mono, no checks need to be performed.
			if (type == null)
				return true;

			var minVer = new Version(2, 10, 8);

			var mi = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

			if (mi == null)
			{
				Console.WriteLine("Unable to locate the version of mono installed.");
			}
			else
			{
				var str = mi.Invoke(null, null) as string;

				if (str == null)
				{
					Console.WriteLine("Unable to query the version of mono installed.");
				}
				else
				{
					var ver = ParseMonoVersion(str);

					if (ver == null || ver < minVer)
					{
						Console.WriteLine("The installed mono version {0} is not supported.", str);
					}
					else
					{
						return true;
					}
				}
			}

			Console.WriteLine("Ensure mono version {0} or newer is installed and try again.", minVer);
			return false;
		}

		public int Run(string[] args)
		{
			try
			{
				AssertWriter.Register();

				if (!VerifyCompatibility())
					throw new PeachException("");

				_options = new OptionSet();
				AddStandardOptions(_options);
				AddCustomOptions(_options);

				var extra = _options.Parse(args);

				ConfigureLogging();

				LoadAssemblies();

				return OnRun(extra);
			}
			catch (OptionException ex)
			{
				return ReportError(true, ex);
			}
			catch (SyntaxException ex)
			{
				return ReportError(ex.ShowUsage, ex);
			}
			catch (Exception ex)
			{
				ReportError(false, ex);
				return 1;
			}
		}

		protected abstract int OnRun(List<string> args);

		protected virtual void ConfigureLogging()
		{
			// Enable debugging if asked for
			// If configuration was already done by a .config file, nothing will be changed
			Utilities.ConfigureLogging(_verbosity);
			Configuration.LogLevel = LogLevel;
		}

		/// <summary>
		/// Override to add custom options
		/// </summary>
		/// <param name="options"></param>
		protected abstract void AddCustomOptions(OptionSet options);

		protected virtual void AddStandardOptions(OptionSet options)
		{
			options.Add(
				"h|help",
				"Display this help and exit",
				v => { throw new SyntaxException(true); }
			);
			options.Add(
				"V|version",
				"Display version information and exit",
				v => ShowVersion()
			);
			options.Add(
				"v|verbose",
				"Increase verbosity, can use multiple times",
				v => _verbosity++
			);
			options.Add(
				"plugins=",
				"Specify the plugins path",
				v => _pluginsPath = v
			);
		}

		protected virtual LogLevel LogLevel
		{
			get
			{
				switch (_verbosity)
				{
					case 0:
						return LogLevel.Info;
					case 1:
						return LogLevel.Debug;
					default:
						return LogLevel.Trace;
				}
			}
		}

		protected virtual int ReportError(bool showUsage, Exception ex)
		{
			if (ex is TargetInvocationException && ex.InnerException != null)
				ex = ex.InnerException;

			if (_verbosity > 1)
				Console.Error.WriteLine(ex);
			else if (!string.IsNullOrEmpty(ex.Message))
				Console.Error.WriteLine(ex.Message);
	
			Console.Error.WriteLine();

			if (showUsage)
				ShowUsage();
	
			return string.IsNullOrEmpty(ex.Message) ? 0 : 2;
		}

		protected virtual string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION]...".Fmt(name.Name);
			}
		}

		protected virtual string Synopsis
		{
			get { return ""; }
		}

		protected virtual void ShowUsage()
		{
			var usage = new[]
			{
				UsageLine,
				"Valid options:",
			};

			Console.WriteLine(string.Join(Environment.NewLine, usage));
			_options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine(Synopsis);
		}

		protected virtual void ShowVersion()
		{
			var name = Assembly.GetEntryAssembly().GetName();
			Console.WriteLine("{0}: Version {1}".Fmt(name.Name, name.Version));
			throw new SyntaxException();
		}

		protected string FindPitLibrary(string pitLibraryPath)
		{
			if (pitLibraryPath == null)
			{
				var lib = Utilities.GetAppResourcePath("pits");
				if (!Directory.Exists(lib))
					throw new PeachException(
						"Could not locate the Peach Pit Library.\r\n" +
						"Ensure there is a 'pits' folder in your Peach installation directory or\r\n" +
						"specify the location of the Peach Pit Library using the '--pits' " +
						"command line option.");
				return lib;
			}

			if (!Directory.Exists(pitLibraryPath))
				throw new PeachException(
					"The specified Peach Pit Library location '{0}' does not exist.".Fmt(
						pitLibraryPath));
			return Path.GetFullPath(pitLibraryPath);
		}

		protected int RunWeb(string pitLibraryPath, bool shouldStartBrowser, IJobMonitor jobMonitor)
		{
			// Don't try and open the browser on linux hosts as this can cause lynx to start
			if (Platform.GetOS() == Platform.OS.Linux)
				shouldStartBrowser = false;

			using (var evt = new AutoResetEvent(false))
			{
				ConsoleCancelEventHandler handler = (s, e) =>
				{
					// ReSharper disable once AccessToDisposedClosure
					evt.Set();
					e.Cancel = true;
				};

				using (var svc = CreateWeb(_license, pitLibraryPath, jobMonitor))
				{
					svc.Start(_webPort);

					if (!Debugger.IsAttached && shouldStartBrowser)
					{
						try
						{
							SysProcess.Start(svc.Uri.ToString());
						}
						catch
						{
							// Eat exceptions
						}
					}

					ConsoleWatcher.WriteInfoMark();
					Console.WriteLine("Web site running at: {0}", svc.Uri);

					ConsoleWatcher.WriteInfoMark();
					Console.WriteLine("Press Ctrl-C to exit.");

					try
					{
						Console.CancelKeyPress += handler;
						evt.WaitOne();
					}
					finally
					{
						Console.CancelKeyPress -= handler;
					}

					return 0;
				}
			}
		}
	}
}
