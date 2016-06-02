
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Runtime;
using Peach.Core.Xsd;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Publishers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.License;

namespace Peach.Pro.Core.Runtime
{
	/// <summary>
	/// Command line interface for Peach 3.  Mostly backwards compatable with
	/// Peach 2.3.
	/// </summary>
	public class ConsoleProgram : BaseProgram
	{
		private static readonly string PitLibraryPath = "PitLibraryPath";
		public static ConsoleColor DefaultForground = Console.ForegroundColor;

		/// <summary>
		/// The list of .config files to be parsed for defines
		/// </summary>
		protected List<string> _configFiles = new List<string>();

		/// <summary>
		/// List of key,value pairs of extra defines to use
		/// </summary>
		protected Dictionary<string, string> _definedValues = new Dictionary<string, string>();

		/// <summary>
		/// Configuration options for the engine
		/// </summary>
		protected RunConfiguration _config = new RunConfiguration();

		/// <summary>
		/// The exit code of the process
		/// </summary>
		public int ExitCode = 1;

		private string _analyzer;
		private string _agent;
		private bool _test;
		private Uri _webUri;
		private string _pitLibraryPath;
		private string _defPitLibraryPath;
		private bool _noweb;
		private bool _nobrowser;
		private static volatile bool _shouldStop;
		private bool _polite;

		#region Public Properties

		/// <summary>
		/// Copyright message
		/// </summary>
		public virtual string Copyright
		{
			get { return Assembly.GetExecutingAssembly().GetCopyright(); }
		}

		/// <summary>
		/// Product name
		/// </summary>
		public virtual string ProductName
		{
			get { return "Peach Pro v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		#endregion

		public ConsoleProgram()
		{
		}

		public ConsoleProgram(string[] args)
		{
			ExitCode = Run(args);
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			// Run analyzer and exit
			options.Add(
				"analyzer=",
				"Launch Peach Analyzer",
				v => _analyzer = v
			);

			// Run agent and wait for ctrl-c
			options.Add(
				"a|agent=",
				"Launch Peach Agent",
				v => _agent = v
			);

			options.Add(
				"1",
				"Perform a single iteration",
				v => _config.singleIteration = true
			);
			options.Add(
				"polite",
				"Disable interactive console mode, which is based on curses",
				v => _polite = true
			);
			options.Add(
				"debug",
				"Enable debug messages. " +
				"Useful when debugging your Peach Pit file. " +
				"Warning: Messages are very cryptic sometimes.",
				v => _verbosity = 1
			);
			options.Add(
				"trace",
				"Enable even more verbose debug messages.",
				v => _verbosity = 2
			);
			options.Add(
				"range=",
				"Provide a range of test #'s to be run.",
				v => ParseRange("range", v)
			);
			options.Add(
				"duration=",
				"How long to run the fuzzer for.",
				(TimeSpan v) => _config.Duration = v
			);
			options.Add(
				"c|count",
				"Count test cases",
				v => _config.countOnly = true
			);
			options.Add(
				"skipto=",
				"Skip to a specific test #. This replaced -r for restarting a Peach run.",
				(uint v) => _config.skipToIteration = v
			);
			options.Add(
				"seed=",
				"Sets the seed used by the random number generator.",
				(uint v) => _config.randomSeed = v
			);
			options.Add(
				"p|parallel=",
				"Parallel fuzzing. Total of M machines, this is machine N.",
				v => ParseParallel("parallel", v)
			);

			// Defined values & .config files
			options.Add(
				"D|define=",
				"Define a substitution value. " +
				"In your PIT you can specify ##KEY## and it will be replaced with VALUE.",
				AddNewDefine
			);
			options.Add(
				"definedvalues=",
				v => _configFiles.Add(v)
			);
			options.Add(
				"config=",
				"XML file containing defined values",
				v => _configFiles.Add(v)
			);
			options.Add(
				"t|test",
				"Validate a Peach Pit file",
				v => _test = true
			);

			// Global actions, get run and immediately exit
			options.Add(
				"showdevices",
				"Display the list of PCAP devices",
				var => _cmd = ShowDevices
			);
			options.Add(
				"showenv",
				"Print a list of all DataElements, Fixups, Agents, " +
				"Publishers and their associated parameters.",
				var => _cmd = ShowEnvironment
			);
			options.Add(
				"makexsd",
				"Generate peach.xsd",
				var => _cmd = MakeSchema
			);

			// web ui
			options.Add(
				"pits=",
				"The path to the pit library.",
				v => _pitLibraryPath = v
			);
			options.Add(
				"noweb",
				"Disable the Peach web interface.",
				v => _noweb = true
			);
			options.Add(
				"nobrowser",
				"Disable launching browser on start.",
				v => _nobrowser = true
			);
			options.Add(
				"webport=",
				"Specifies port web interface runs on.",
				(int v) => _webPort = v
			);
		}

		protected override bool VerifyCompatibility()
		{
			if (!base.VerifyCompatibility())
				return false;

			var type = Type.GetType("Mono.Runtime");

			// If we are not on mono, no checks need to be performed.
			if (type == null)
				return true;

			try
			{
				Console.ForegroundColor = DefaultForground;
				Console.ResetColor();
				return true;
			}
			catch
			{
				var term = Environment.GetEnvironmentVariable("TERM");

				if (term == null)
					Console.WriteLine("An incompatible terminal type was detected.");
				else
					Console.WriteLine("An incompatible terminal type '{0}' was detected.", term);

				Console.WriteLine("Change your terminal type to 'linux', 'xterm' or 'rxvt' and try again.");
				return false;
			}
		}

		protected override int OnRun(List<string> args)
		{
			PrepareLicensing(_pitLibraryPath);

			_config.commandLine = args.ToArray();

			try
			{
				Console.Write("\n");
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.Write("[[ ");
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine(ProductName);
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.Write("[[ ");
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine(Copyright);
				if (_license.IsNearingExpiration())
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine();
					Console.WriteLine(_license.ExpirationWarning());
				}
				Console.ForegroundColor = DefaultForground;
				Console.WriteLine();

				if (_agent != null)
					return OnRunAgent(_agent, args);

				if (_analyzer != null)
					return OnRunAnalyzer(_analyzer, args);

				return OnRunJob(_test, args);
			}
			finally
			{
				// Reset console colors
				Console.ForegroundColor = DefaultForground;
			}
		}

		protected override int ShowUsage(List<string> args)
		{
			Syntax();
			return 0;
		}

		protected virtual Watcher GetUIWatcher()
		{
			try
			{
				if (_verbosity > 0 || _polite)
					return new ConsoleWatcher();

				// Ensure console is interactive
				Console.Clear();

				var title = _webUri == null ? "" : " ({0})".Fmt(_webUri);

				return new InteractiveConsoleWatcher(_license, title);
			}
			catch (IOException)
			{
				return new ConsoleWatcher();
			}
		}

		/// <summary>
		/// Create an engine and run the fuzzing job
		/// </summary>
		protected virtual void RunEngine(Peach.Core.Dom.Dom dom, PitConfig pitConfig)
		{
			// Ensure the database has been migrated prior to
			// creating the Job, as it will insert itself.
			using (var db = new NodeDatabase())
			{
				db.Migrate();
			}

			// Add the JobLogger as necessary
			Test test;

			if (!dom.tests.TryGetValue(_config.runName, out test))
				throw new PeachException("Unable to locate test named '{0}'.".Fmt(_config.runName));

			if (pitConfig != null && pitConfig.Weights != null)
			{
				foreach (var item in pitConfig.Weights)
				{
					test.weights.Add(new SelectWeight
					{
						Name = item.Id, 
						Weight = (ElementWeight)item.Weight
					});
				}
			}

			// Add the JobLogger as necessary
			if (!test.loggers.OfType<JobLogger>().Any())
				test.loggers.Insert(0, new JobLogger());

			var job = new Job(_config);

			if (_noweb || CreateWeb == null)
			{
				var e = new Engine(GetUIWatcher());
				e.startFuzzing(dom, _config);
				return;
			}

			using (var svc = CreateWeb(_license, "", new ConsoleJobMonitor(job)))
			{
				svc.Start(_webPort);

				_webUri = svc.Uri;

				InteractiveConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Web site running at: {0}", svc.Uri);

				var e = new Engine(GetUIWatcher());
				e.startFuzzing(dom, _config);
			}
		}

		/// <summary>
		/// Run a command line analyzer of the specified name
		/// </summary>
		/// <param name="analyzer"></param>
		/// <param name="extra"></param>
		protected virtual int OnRunAnalyzer(string analyzer, List<string> extra)
		{
			var analyzerType = ClassLoader.FindPluginByName<AnalyzerAttribute>(analyzer);
			if (analyzerType == null)
				throw new PeachException("Error, unable to locate analyzer called '" + analyzer + "'.\n");

			var field = analyzerType.GetField("supportCommandLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			System.Diagnostics.Debug.Assert(field != null);
			if ((bool)field.GetValue(null) == false)
				throw new PeachException("Error, analyzer not configured to run from command line.");

			var analyzerInstance = (Analyzer)Activator.CreateInstance(analyzerType);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting Analyzer");

			var args = new Dictionary<string, string>();
			for (var i = 0; i < extra.Count; i++)
				args[i.ToString()] = extra[i];

			analyzerInstance.asCommandLine(args);

			return 0;
		}

		/// <summary>
		/// Run a peach agent of the specified protocol
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="extra"></param>
		protected virtual int OnRunAgent(string agent, List<string> extra)
		{
			var agentType = ClassLoader.FindPluginByName<AgentServerAttribute>(agent);
			if (agentType == null)
				throw new PeachException("Error, unable to locate agent server for protocol '" + agent + "'.\n");

			var agentServer = (IAgentServer)Activator.CreateInstance(agentType);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting agent server");

			var args = new Dictionary<string, string>();
			for (var i = 0; i < extra.Count; i++)
				args[i.ToString()] = extra[i];

			agentServer.Run(args);

			return 0;
		}

		/// <summary>
		/// Run a fuzzing job
		/// </summary>
		/// <param name="test">Test only. Parses the pit and exits.</param>
		/// <param name="extra">Extra command line options</param>
		protected virtual int OnRunJob(bool test, List<string> extra)
		{
			if (!string.IsNullOrEmpty(_pitLibraryPath) && !string.IsNullOrEmpty(_defPitLibraryPath))
			{
				if (_pitLibraryPath != _defPitLibraryPath)
					throw new PeachException("--pits and -DPitLibraryPath should both specify the same path.");
			}

			if (string.IsNullOrEmpty(_pitLibraryPath))
				_pitLibraryPath = _defPitLibraryPath;

			if (extra.Count > 0)
			{
				// Pit was specified on the command line, do normal behavior
				// Ensure the EULA has been accepted before running a job
				// on the command line.  The WebUI will present a EULA
				// in the later case.

				if (!_license.EulaAccepted)
					ShowEula();

				// Let Web-UI show errors when no command line args are specified
				if (!_license.IsValid)
				{
					Console.WriteLine(_license.ErrorText);
					return -1;
				}

				_config.shouldStop = () => _shouldStop;
				Console.CancelKeyPress += Console_CancelKeyPress;

				var pitPath = _config.pitFile = extra[0];

				PitConfig pitConfig = null;
				if (Path.GetExtension(pitPath) == ".peach")
				{
					// Ensure pit library exists
					_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
					_definedValues[PitLibraryPath] = _pitLibraryPath;

					pitConfig = PitDatabase.LoadPitConfig(pitPath);
					pitPath = Path.Combine(_pitLibraryPath, pitConfig.OriginalPit);
				}

				if (extra.Count > 1)
					_config.runName = extra[1];

				var defs = ParseDefines(pitPath + ".config", pitConfig);

				var parserArgs = new Dictionary<string, object>();
				parserArgs[PitParser.DEFINED_VALUES] = defs;

				var parser = new ProPitParser(_license, _pitLibraryPath, pitPath);

				if (test)
				{
					InteractiveConsoleWatcher.WriteInfoMark();
					Console.Write("Validating file [" + pitPath + "]... ");
					parser.asParserValidation(parserArgs, pitPath);
					Console.WriteLine("No Errors Found.");
				}
				else
				{
					var dom = parser.asParser(parserArgs, pitPath);

					if (pitConfig != null)
						PitInjector.InjectAgents(pitConfig, defs, dom);

					RunEngine(dom, pitConfig);
				}
			}
			else if (!_noweb && CreateWeb != null)
			{
				_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
				RunWeb(_pitLibraryPath, !_nobrowser, new InternalJobMonitor(_license));
			}

			return 0;
		}

		/// <summary>
		/// Combines define files and define arguments into a single list
		/// Command line arguments override any .config file's defines
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<KeyValuePair<string, string>> ParseDefines(string xmlConfig, PitConfig pitConfig)
		{
			// Parse pit.xml.config to poopulate system defines and add
			// -D command line overrides.
			// This will succeed even if pitConfig doesn't exist.
			var defs = PitDefines.ParseFile(xmlConfig, _definedValues);

			foreach (var item in _configFiles)
			{
				var normalized = Path.GetFullPath(item);

				if (!File.Exists(normalized))
					throw new PeachException("Error, defined values file \"" + item + "\" does not exist.");

				var cfg = XmlTools.Deserialize<PitDefines>(normalized);

				// Add defines from extra config files in order
				defs.Children.AddRange(cfg.Children);
			}

			var ret = defs.Evaluate();

			if (pitConfig != null)
				PitInjector.InjectDefines(pitConfig, defs, ret);

			return ret;
		}

		/// <summary>
		/// Override to change syntax message.
		/// </summary>
		protected virtual void Syntax()
		{
			const string syntax1 =
@"This is the Peach Runtime.  The Peach Runtime is one of the many ways
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
";
			const string syntax2 = @"
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

			Console.WriteLine(syntax1);
			_options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine(syntax2);
		}

		private void ShowEula()
		{
			Console.WriteLine(_license.EulaText);

			Console.WriteLine(
@"BY TYPING ""YES"" YOU ACKNOWLEDGE THAT YOU HAVE READ, UNDERSTAND, AND
AGREE TO BE BOUND BY THE TERMS ABOVE.
");

			while (true)
			{
				Console.WriteLine("Do you accept the end user license agreement?");

				Console.Write("(yes/no) ");
				var answer = Console.ReadLine();
				Console.WriteLine();

				if (answer == "no")
					Environment.Exit(-1);

				if (answer == "yes")
				{
					_license.EulaAccepted = true;
					return;
				}

				Console.WriteLine("The answer \"{0}\" is invalid. It must be one of \"yes\" or \"no\".", answer);
				Console.WriteLine();
			}
		}

		#region Command line option parsing helpers

		protected void ParseRange(string arg, string v)
		{
			var parts = v.Split(',');
			if (parts.Length != 2)
				throw new PeachException("Invalid range: " + v);

			try
			{
				_config.rangeStart = Convert.ToUInt32(parts[0]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range start iteration: " + parts[0], ex);
			}

			try
			{
				_config.rangeStop = Convert.ToUInt32(parts[1]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range stop iteration: " + parts[1], ex);
			}

			if (_config.parallel)
				throw new PeachException("--range is not supported when --parallel is specified");

			_config.range = true;
		}

		protected void ParseParallel(string arg, string v)
		{
			var parts = v.Split(',');
			if (parts.Length != 2)
				throw new PeachException("Invalid parallel value: " + v);

			try
			{
				_config.parallelTotal = Convert.ToUInt32(parts[0]);

				if (_config.parallelTotal == 0)
					throw new ArgumentOutOfRangeException();
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid parallel machine total: " + parts[0], ex);
			}

			try
			{
				_config.parallelNum = Convert.ToUInt32(parts[1]);
				if (_config.parallelNum == 0 || _config.parallelNum > _config.parallelTotal)
					throw new ArgumentOutOfRangeException();
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid parallel machine number: " + parts[1], ex);
			}

			if (_config.range)
				throw new PeachException("--parallel is not supported when --range is specified");

			_config.parallel = true;
		}

		protected void AddNewDefine(string arg)
		{
			var parts = arg.Split('=');
			if (parts.Length != 2)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var key = parts[0];
			var value = parts[1];

			// Allow command line options to override others
			_definedValues[key] = value;

			if (key == PitLibraryPath)
				_defPitLibraryPath = value;
		}

		#endregion

		#region Global Actions

		static int ShowDevices(List<string> args)
		{
			var devices = RawEtherPublisher.Devices();

			if (devices.Count == 0)
			{
				Console.WriteLine();
				Console.WriteLine("No capture devices were found.  Ensure you have the proper");
				Console.WriteLine("permissions for performing PCAP captures and try again.");
				Console.WriteLine();
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("The following devices are available on this machine:");
				Console.WriteLine("----------------------------------------------------");
				Console.WriteLine();

				// Print out all available devices
				foreach (var dev in devices)
				{
					Console.WriteLine("Name: {0}\nDescription: {1}\n\n", dev.Interface.FriendlyName, dev.Description);
				}
			}

			return 0;
		}

		static int ShowEnvironment(List<string> args)
		{
			Usage.Print();
			return 0;
		}

		static int MakeSchema(List<string> args)
		{
			try
			{
				Console.WriteLine();

				using (var stream = new FileStream("peach.xsd", FileMode.Create, FileAccess.Write))
				{
					SchemaBuilder.Generate(typeof(Peach.Core.Xsd.Dom), stream);

					Console.WriteLine("Successfully generated {0}", stream.Name);
				}

				return 0;
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new PeachException("Error creating schema. {0}".Fmt(ex.Message), ex);
			}
		}

		#endregion

		protected static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(" --- Ctrl+C Detected --- ");

			if (!_shouldStop)
			{
				Console.WriteLine(" --- Waiting for last iteration to complete --- ");
				_shouldStop = true;
			}
			else
			{
				Console.WriteLine(" --- Aborting --- ");

				// Need to call Environment.Exit from outside this event handler
				// to ensure the finalizers get called...
				// http://www.codeproject.com/Articles/16164/Managed-Application-Shutdown
				new Thread(() => Environment.Exit(0)).Start();
			}
		}
	}

	class ConsoleJobMonitor : IJobMonitor
	{
		readonly Guid _guid;
		readonly int _pid = Utilities.GetCurrentProcessId();

		public ConsoleJobMonitor(Job job)
		{
			_guid = job.Guid;
		}

		public void Dispose()
		{
		}

		public int Pid { get { return _pid; } }

		public bool IsTracking(Job job)
		{
			lock (this)
			{
				return _guid == job.Guid;
			}
		}

		public bool IsControllable { get { return false; } }

		public Job GetJob()
		{
			using (var db = new NodeDatabase())
			{
				return db.GetJob(_guid);
			}
		}

		#region Not Implemented

		public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
		{
			throw new NotImplementedException();
		}

		public bool Pause()
		{
			throw new NotImplementedException();
		}

		public bool Continue()
		{
			throw new NotImplementedException();
		}

		public bool Stop()
		{
			throw new NotImplementedException();
		}

		public bool Kill()
		{
			throw new NotImplementedException();
		}

		public EventHandler InternalEvent
		{
			set { throw new NotImplementedException(); }
		}

		#endregion
	}
}
