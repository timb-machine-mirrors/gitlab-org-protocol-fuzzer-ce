
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
using Peach.Core.Runtime;
using SharpPcap;
using Version = System.Version;

namespace Peach.Pro.Core.Runtime
{
	/// <summary>
	/// Command line interface for Peach 3.  Mostly backwards compatable with
	/// Peach 2.3.
	/// </summary>
	public class ConsoleProgram : Program
	{
		// PUT THIS INTO YOUR PROGRAM
		////public static int Run(string[] args)
		////{
		////    return new ConsoleProgram(args).ExitCode;
		////}

		public static Thread CurrentThread = Thread.CurrentThread;

		public static ConsoleColor DefaultForground = Console.ForegroundColor;

		/// <summary>
		/// The list of .config files to be parsed for defines
		/// </summary>
		protected List<string> _configFiles = new List<string>();

		/// <summary>
		/// List of key,value pairs of extra defines to use
		/// </summary>
		protected List<KeyValuePair<string, string>> _definedValues = new List<KeyValuePair<string, string>>();

		/// <summary>
		/// Configuration options for the engine
		/// </summary>
		protected RunConfiguration _config = new RunConfiguration();

		/// <summary>
		/// The exit code of the process
		/// </summary>
		public int ExitCode = 1;

		private string _analyzer = null;
		private string _agent = null;
		private bool _test = false;

		#region Public Properties

		/// <summary>
		/// Copyright message
		/// </summary>
		public virtual string Copyright
		{
			get { return "Copyright (c) Michael Eddington"; }
		}

		/// <summary>
		/// Product name
		/// </summary>
		public virtual string ProductName
		{
			get { return "Peach v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		#endregion

		public ConsoleProgram(string[] args)
		{
			_config.commandLine = args;
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
				"debug",
				"Enable debug messages. " + 
				"Useful when debugging your Peach Pit file. " +
				"Warning: Messages are very cryptic sometimes.",
				v => _config.debug = 1
			);
			options.Add(
				"trace",
 				"Enable even more verbose debug messages.",
				v => _config.debug = 2
			);
			options.Add(
				"range=", 
				"Provide a range of test #'s to be run.",
				v => ParseRange("range", v)
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
				v => AddNewDefine(v)
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
				var => ShowDevices()
			);
			options.Add(
				"showenv",
				"Print a list of all DataElements, Fixups, Agents, " +
				"Publishers and their associated parameters.",
				var => ShowEnvironment()
			);
			options.Add(
				"makexsd", 
				"Generate peach.xsd",
				var => MakeSchema()
			);
		}

		protected override bool VerifyCompatibility()
		{
			var type = Type.GetType("Mono.Runtime");

			// If we are not on mono, no checks need to be performed.
			if (type == null)
				return true;

			try
			{
				Console.ForegroundColor = DefaultForground;
				Console.ResetColor();
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

			return base.VerifyCompatibility();
		}

		protected override void OnRun(List<string> args)
		{
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
				Console.ForegroundColor = DefaultForground;
				Console.WriteLine();

				// Enable debugging if asked for
				// If configuration was already done by a .config file, nothing will be changed
				Utilities.ConfigureLogging(_config.debug);

				// Load the platform assembly
				LoadPlatformAssembly();

				if (_agent != null)
				{
					OnRunAgent(_agent, args);
				}
				else if (_analyzer != null)
				{
					OnRunAnalyzer(_analyzer, args);
				}
				else
				{
					OnRunJob(_test, args);
				}

				ExitCode = 0;
			}
			catch (ArgumentException ae)
			{
				Console.WriteLine(ae.Message + " " + ae.ParamName + "\n");
				Console.WriteLine("Use -h for help");
			}
			catch (SyntaxException)
			{
				// Ignore, thrown by syntax()
			}
			catch (OptionException oe)
			{
				Console.WriteLine(oe.Message + "\n");
			}
			catch (PeachException ee)
			{
				if (_config.debug > 0)
					Console.WriteLine(ee);
				else
					Console.WriteLine(ee.Message + "\n");
			}
			finally
			{
				// HACK - Required on Mono with NLog 2.0
				Utilities.ConfigureLogging(-1);

				// Reset console colors
				Console.ForegroundColor = DefaultForground;
			}
		}

		protected override void ShowUsage()
		{
			Syntax();
		}

		protected virtual Watcher GetUIWatcher()
		{
			return new ConsoleWatcher();
		}

		protected virtual Analyzer GetParser()
		{
			return Analyzer.defaultParser;
		}

		/// <summary>
		/// Create an engine and run the fuzzing job
		/// </summary>
		protected virtual void RunEngine(Peach.Core.Dom.Dom dom)
		{
			var e = new Engine(GetUIWatcher());

			e.startFuzzing(dom, _config);
		}

		/// <summary>
		/// Run a command line analyzer of the specified name
		/// </summary>
		/// <param name="analyzer"></param>
		/// <param name="extra"></param>
		protected virtual void OnRunAnalyzer(string analyzer, List<string> extra)
		{
			var analyzerType = ClassLoader.FindPluginByName<AnalyzerAttribute>(analyzer);
			if (analyzerType == null)
				throw new PeachException("Error, unable to locate analyzer called '" + analyzer + "'.\n");

			var field = analyzerType.GetField("supportCommandLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			if ((bool)field.GetValue(null) == false)
				throw new PeachException("Error, analyzer not configured to run from command line.");

			var analyzerInstance = (Analyzer)Activator.CreateInstance(analyzerType);

			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting Analyzer");

			var args = new Dictionary<string, string>();
			for (int i = 0; i < extra.Count; i++)
				args[i.ToString()] = extra[i];

			analyzerInstance.asCommandLine(args);
		}

		/// <summary>
		/// Run a peach agent of the specified protocol
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="extra"></param>
		protected virtual void OnRunAgent(string agent, List<string> extra)
		{
			var agentType = ClassLoader.FindPluginByName<AgentServerAttribute>(agent);
			if (agentType == null)
				throw new PeachException("Error, unable to locate agent server for protocol '" + agent + "'.\n");

			var agentServer = (IAgentServer)Activator.CreateInstance(agentType);

			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting agent server");

			var args = new Dictionary<string, string>();
			for (int i = 0; i < extra.Count; i++)
				args[i.ToString()] = extra[i];

			agentServer.Run(args);
		}

		/// <summary>
		/// Run a fuzzing job
		/// </summary>
		/// <param name="test">Test only. Parses the pit and exits.</param>
		/// <param name="extra">Extra command line options</param>
		protected virtual void OnRunJob(bool test, List<string> extra)
		{
			Console.CancelKeyPress += Console_CancelKeyPress;

			if (extra.Count == 0)
				Syntax();

			if (extra.Count > 0)
				_config.pitFile = extra[0];

			if (extra.Count > 1)
				_config.runName = extra[1];

			AddNewDefine("Peach.Cwd=" + Environment.CurrentDirectory);
			AddNewDefine("Peach.Pwd=" + Utilities.ExecutionDirectory);

			// Do we have pit.xml.config file?
			// If so load it as the first defines file.
			if (extra.Count > 0 && File.Exists(extra[0]) &&
				extra[0].ToLower().EndsWith(".xml") &&
				File.Exists(extra[0] + ".config"))
			{
				_configFiles.Insert(0, extra[0] + ".config");
			}

			var defs = ParseDefines();

			var parserArgs = new Dictionary<string, object>();
			parserArgs[PitParser.DEFINED_VALUES] = defs;

			var parser = GetParser();

			if (test)
			{
				ConsoleWatcher.WriteInfoMark();
				Console.Write("Validating file [" + _config.pitFile + "]... ");
				parser.asParserValidation(parserArgs, _config.pitFile);
				Console.WriteLine("No Errors Found.");
			}
			else
			{
				var dom = parser.asParser(parserArgs, _config.pitFile);

				RunEngine(dom);
			}
		}

		/// <summary>
		/// Combines define files and define arguments into a single list
		/// Command line arguments override any .config file's defines
		/// </summary>
		/// <returns></returns>
		protected virtual List<KeyValuePair<string, string>> ParseDefines()
		{
			var items = _configFiles.Select(PitParser.parseDefines).ToList();

			// Add .config files in order

			// Add -D command line options last
			items.Add(_definedValues);

			var ret = new List<KeyValuePair<string, string>>();

			// Foreach #define, save it, overwriting and previous value
			foreach (var defs in items)
			{
				foreach (var kv in defs)
				{
					ret.RemoveAll(i => i.Key == kv.Key);
					ret.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
				}
			}

			return ret;
		}

		/// <summary>
		/// Override to change syntax message.
		/// </summary>
		protected virtual void Syntax()
		{
			const string syntax1 =
@"This is the Peach Runtime.  The Peach Runtime is one of the many ways
to use Peach Pit files.  Currently this runtime is still in development
but already exposes several abilities to the end-user such as performing
simple fuzzer runs and performing parsing tests of Peach Pit files.

Please submit any bugs to https://forums.peachfuzzer.com.

Syntax:

  peach -a CHANNEL
  peach -c PIT [TEST]
  peach [--skipto #] PIT [TEST]
  peach -p 10,2 [--skipto #] PIT [TEST]
  peach --range 100,200 PIT [TEST]
  peach -t PIT
";
			const string syntax2 = @"
Peach Web Interface

  Syntax: peach

  Starts up peach and provides a web site for configuring and starting
  a fuzzing job from the Peach Pit Library.

Peach Agent

  Syntax: peach -a CHANNEL
  
  Starts up a Peach Agent instance on the current machine.  User must provide
  a channel/protocol name (e.g. tcp).

  Note: Local agents are implicitly started.

Performing Fuzzing Run

  Syntax: peach PIT [TEST]
  Syntax: peach --skipto 1234 PIT [TEST]
  Syntax: peach --range 100,200 PIT [TEST]
  
  To start a fuzzing run, specify the Peach Pit file and optionally, the
  name of a test to perform.
  
  The --skipto parameter is useful for resuming a run in case it was interrupted
  for any reason.  This parameter accepts the test # to resume.
  
  Additionally a range of test cases can be specified using --range.

Performing A Parellel Fuzzing Run

  Syntax: peach -p 10,2 PIT [TEST]

  A parallel fuzzing run uses multiple machines to perform the same fuzzing
  which shortens the time required.  To run in parallel mode we will need
  to know the total number of machines and which machine we are.  This
  information is fed into Peach via the " + "\"-p\"" + @" command line argument in the
  format " + "\"total_machines,our_machine\"." + @"

Validate Peach Pit File

  Syntax: peach -t PIT
  
  This will perform a parsing pass of the Peach Pit file and display any
  errors that are found.

Debug Peach Pit File

  Syntax: peach -1 --debug PIT
  
  This will perform a single iteration (-1) of your pit file while displaying
  alot of debugging information (--debug).  The debugging information was
  origionally intended just for the developers, but can be usefull in pit
  debugging as well.
";
			Console.WriteLine(syntax1);
			_options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine(syntax2);
		}

		#region Command line option parsing helpers

		protected void ParseRange(string arg, string v)
		{
			string[] parts = v.Split(',');
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
			string[] parts = v.Split(',');
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
			var idx = arg.IndexOf("=");
			if (idx < 0)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var key = arg.Substring(0, idx);
			var value = arg.Substring(idx + 1);

			// Allow command line options to override others
			_definedValues.RemoveAll(i => i.Key == key);
			_definedValues.Add(new KeyValuePair<string, string>(key, value));
		}

		#endregion

		#region Global Actions

		static void ShowDevices()
		{
			var devices = CaptureDeviceList.Instance;

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
					Console.WriteLine("Name: {0}\nDescription: {1}\n\n", dev.Name, dev.Description);
				}
			}

			throw new SyntaxException();
		}

		static void ShowEnvironment()
		{
			Peach.Core.Usage.Print();
			throw new SyntaxException();
		}

		static void MakeSchema()
		{
			try
			{
				Console.WriteLine();

				using (var stream = new FileStream("peach.xsd", FileMode.Create, FileAccess.Write))
				{
					Peach.Core.Xsd.SchemaBuilder.Generate(typeof(Peach.Core.Xsd.Dom), stream);

					Console.WriteLine("Successfully generated {0}", stream.Name);
				}

				throw new SyntaxException();
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

			if (CurrentThread == null)
				return;

			Console.WriteLine();
			Console.WriteLine(" --- Ctrl+C Detected --- ");

			var th = CurrentThread;
			CurrentThread = null;

			// Need to call Environment.Exit from outside this event handler
			// to ensure the finalizers get called...
			// http://www.codeproject.com/Articles/16164/Managed-Application-Shutdown
			new Thread(delegate()
			{
				Environment.Exit(0);
			}).Start();

			System.Diagnostics.Debug.Assert(th != null);

			// TODO: Eventually move to use Thread.Abort() since it will properly cleanup!

			// Don't use a lambda here because we don't want it to get jitted
			// in the ctrl-c handler
			// new Thread(StopperThread).Start(th);
		}

		private static void StopperThread(object param)
		{
			((Thread)param).Abort();
		}
	}
}

// end
