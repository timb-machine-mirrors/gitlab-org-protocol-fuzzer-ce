using Peach.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Runtime
{
	public class Program : Peach.Core.Runtime.Program
	{
		string pitLibraryPath;

		public Program(string[] args)
			: base(args)
		{
		}

		protected override void AddCustomOptions(Core.Runtime.OptionSet options)
		{
			options.Add("pits=", v => pitLibraryPath = v);
		}

		/// <summary>
		/// Copyright message
		/// </summary>
		public override string Copyright
		{
			get { return "Copyright (c) Deja vu Security"; }
		}

		/// <summary>
		/// Product name
		/// </summary>
		public override string ProductName
		{
			get { return "Peach Pro v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		protected override Watcher GetUIWatcher()
		{
			try
			{
				if (config.debug > 0)
				{
					return new Peach.Core.Runtime.ConsoleWatcher();
				}

				// Ensure console is interactive
				Console.Clear();

				return new Peach.Enterprise.Runtime.ConsoleWatcher();

			}
			catch (IOException)
			{
				return new Peach.Core.Runtime.ConsoleWatcher();
			}
		}

		protected override Analyzer GetParser()
		{
			var parser = new Godel.Core.GodelPitParser();
			Analyzer.defaultParser = parser;

			return base.GetParser();
		}

		protected override List<KeyValuePair<string, string>> ParseDefines()
		{
			var defs = base.ParseDefines();
			var ret = PitDefines.Evaluate(defs);
			return ret;
		}

		protected override void OnRunJob(bool test, System.Collections.Generic.List<string> extra)
		{
			if (extra.Count > 0)
			{
				// Pit was specified on the command line, do normal behavior
				base.OnRunJob(test, extra);
			}
			else
			{
				// Ensure pit library exists
				var pits = FindPitLibrary();

				Peach.Enterprise.WebServices.WebServer.Run(pits);
			}
		}

		protected override void RunEngine()
		{
			// Pass an empty pit library path if we are running a job off of
			// the command line.

			using (var svc = new WebServices.WebServer(""))
			{
				// Tell the web service the job is running off the command line
				svc.Context.AttachJob(config);

				svc.Start();

				Core.Runtime.ConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Web site running at: {0}", svc.Uri);

				// Add the web logger as the 1st logger to each test
				foreach (var test in dom.tests)
					test.loggers.Insert(0, svc.Context.Logger);

				base.RunEngine();
			}
		}

		private string FindPitLibrary()
		{
			if (pitLibraryPath == null)
			{
				var pwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var lib = Path.Combine(pwd, "pits");

				if (!Directory.Exists(lib))
					throw new PeachException("Could not locate the Peach Pit Library. Ensure there is a 'pits' folder in your Peach installation directory or speficy the location of the Peach Pit Library using the '--pits' command line option.");

				return lib;
			}
			else
			{
				if (!Directory.Exists(pitLibraryPath))
					throw new PeachException("The specified Peach Pit Library location '{0}' does not exist.".Fmt(pitLibraryPath));

				return pitLibraryPath;
			}
		}
	}
}
