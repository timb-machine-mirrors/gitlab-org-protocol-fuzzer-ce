using Peach.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Runtime
{
	public class Program : Peach.Core.Runtime.Program
	{
		Uri webUri;
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

				return new Peach.Enterprise.Runtime.ConsoleWatcher(" ({0})".Fmt(webUri));

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

				webUri = svc.Uri;

				Core.Runtime.ConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Web site running at: {0}", svc.Uri);

				// Add the web logger as the 1st logger to each test
				foreach (var test in dom.tests)
					test.loggers.Insert(0, svc.Context.Logger);

				base.RunEngine();
			}
		}

		protected virtual void Syntax()
		{
			string syntax = @"This is the Peach Runtime.  The Peach Runtime is one of the many ways
to use Peach XML files.  Currently this runtime is still in development
but already exposes several abilities to the end-user such as performing
simple fuzzer runs and performing parsing tests of Peach XML files.

Please submit any bugs to https://forums.peachfuzzer.com.

Syntax:

  peach -a channel
  peach -c peach_xml_file [test_name]
  peach [--skipto #] peach_xml_flie [test_name]
  peach -p 10,2 [--skipto #] peach_xml_file [test_name]
  peach --range 100,200 peach_xml_file [test_name]
  peach -t peach_xml_file

  -1                         Perform a single iteration
  -a,--agent                 Launch Peach Agent
  -c,--count                 Count test cases
  -t,--test xml_file         Validate a Peach XML file
  -p,--parallel M,N          Parallel fuzzing.  Total of M machines, this
                             is machine N.
  --debug                    Enable debug messages. Usefull when debugging
                             your Peach XML file.  Warning: Messages are very
                             cryptic sometimes.
  --trace                    Enable even more verbose debug messages.
  --seed N                   Sets the seed used by the random number generator
  --parseonly                Test parse a Peach XML file
  --makexsd                  Generate peach.xsd
  --showenv                  Print a list of all DataElements, Fixups, Monitors
                             Publishers and their associated parameters.
  --showdevices              Display the list of PCAP devices
  --analyzer                 Launch Peach Analyzer
  --skipto N                 Skip to a specific test #.  This replaced -r
                             for restarting a Peach run.
  --range N,M                Provide a range of test #'s to be run.
  -D/define=KEY=VALUE        Define a substitution value.  In your PIT you can
                             ##KEY## and it will be replaced for VALUE.
  --config=FILENAME          XML file containing defined values
  --pits=PIT_LIBRARY_PATH    The path to the pit library.

Peach Web Interface

  Syntax: peach

  Starts up peach and provides a web site for configuring and starting
  a fuzzing job from the Peach Pit Library.

Peach Agent

  Syntax: peach -a channel
  
  Starts up a Peach Agent instance on this current machine.  User must provide
  a channel/protocol name (e.g. tcp).

  Note: Local agents are started automatically.

Performing Fuzzing Run

  Syntax: peach peach_xml_flie [test_name]
  Syntax: peach --skipto 1234 peach_xml_flie [test_name]
  Syntax: peach --range 100,200 peach_xml_flie [test_name]
  
  A fuzzing run is started by by specifying the Peach XML file and the
  name of a test to perform.
  
  If a run is interupted for some reason it can be restarted using the
  --skipto parameter and providing the test # to start at.
  
  Additionally a range of test cases can be specified using --range.

Performing A Parellel Fuzzing Run

  Syntax: peach -p 10,2 peach_xml_flie [test_name]

  A parallel fuzzing run uses multiple machines to perform the same fuzzing
  which shortens the time required.  To run in parallel mode we will need
  to know the total number of machines and which machine we are.  This
  information is fed into Peach via the " + "\"-p\"" + @" command line argument in the
  format " + "\"total_machines,our_machine\"." + @"

Validate Peach XML File

  Syntax: peach -t peach_xml_file
  
  This will perform a parsing pass of the Peach XML file and display any
  errors that are found.

Debug Peach XML File

  Syntax: peach -1 --debug peach_xml_file
  
  This will perform a single iteration (-1) of your pit file while displaying
  alot of debugging information (--debug).  The debugging information was
  origionally intended just for the developers, but can be usefull in pit
  debugging as well.
";
			Console.WriteLine(syntax);
			throw new Core.Runtime.SyntaxException();
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
