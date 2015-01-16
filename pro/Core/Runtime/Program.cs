
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
	public class Program
	{
		// PUT THIS INTO YOUR PROGRAM
		////public static int Run(string[] args)
		////{
		////    return new Program(args).exitCode;
		////}

		public static Thread CurrentThread = Thread.CurrentThread;

		public static ConsoleColor DefaultForground = Console.ForegroundColor;

		public static void LoadPlatformAssembly()
		{
			//if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
			//	throw new PeachException("Error: Cannot use the 32bit version of Peach 3 on a 64bit operating system.");

			//if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
			//	throw new PeachException("Error: Cannot use the 64bit version of Peach 3 on a 32bit operating system.");

			string osAssembly = null;

			switch (Platform.GetOS())
			{
				case Platform.OS.OSX:
					osAssembly = "Peach.Pro.OS.OSX.dll";
					break;
				case Platform.OS.Linux:
					osAssembly = "Peach.Pro.OS.Linux.dll";
					break;
				case Platform.OS.Windows:
					osAssembly = "Peach.Pro.OS.Windows.dll";
					break;
			}

			try
			{
				ClassLoader.LoadAssembly(osAssembly);
			}
			catch (Exception ex)
			{
				throw new PeachException(string.Format("Error, could not load platform assembly '{0}'.  {1}", osAssembly, ex.Message), ex);
			}
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

		public static bool VerifyCompatibility()
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

		/// <summary>
		/// The list of .config files to be parsed for defines
		/// </summary>
		protected List<string> configFiles = new List<string>();

		/// <summary>
		/// List of key,value pairs of extra defines to use
		/// </summary>
		protected List<KeyValuePair<string, string>> definedValues = new List<KeyValuePair<string, string>>();

		/// <summary>
		/// The result of parsing the pit file
		/// </summary>
		protected Peach.Core.Dom.Dom dom = null;

		/// <summary>
		/// Configuration options for the engine
		/// </summary>
		protected RunConfiguration config = new RunConfiguration();

		/// <summary>
		/// The exit code of the process
		/// </summary>
		public int exitCode = 1;

		/// <summary>
		/// Extra arguments passed on command line
		/// </summary>
		public List<string> extra = null;

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

		public Program(string[] args)
		{
			if (!VerifyCompatibility())
				return;

			AssertWriter.Register();

			config.commandLine = args;

			try
			{
				string analyzer = null;
				string agent = null;
				bool test = false;

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

				var p = new OptionSet()
				{
					// Gobal option
					{ "h|?|help", v => Syntax() },

					// Run analyzer and exit
					{ "analyzer=", v => analyzer = v },

					// Run agent and wait for ctrl-c
					{ "a|agent=", v => agent = v},

					{ "debug", v => config.debug = 1 },
					{ "trace", v => config.debug = 2 },
					{ "1", v => config.singleIteration = true},
					{ "range=", v => ParseRange("range", v)},
					{ "c|count", v => config.countOnly = true},
					{ "skipto=", v => config.skipToIteration = ParseUInt("skipToIteration", v)},
					{ "seed=", v => config.randomSeed = ParseUInt("randomSeed", v)},
					{ "p|parallel=", v => ParseParallel("parallel", v)},

					// Defined values & .config files
					{ "D|define=", v => AddNewDefine(v) },
					{ "definedvalues=", v => configFiles.Add(v) },
					{ "config=", v => configFiles.Add(v) },

					{ "t|test", v => test = true},

					// Global actions, get run and immediately exit
					{ "bob", var => Bob() },
					{ "charlie", var => Charlie() },
					{ "showdevices", var => ShowDevices() },
					{ "showenv", var => ShowEnvironment() },
					{ "makexsd", var => MakeSchema() },
				};

				AddCustomOptions(p);

				extra = p.Parse(args);

				// Enable debugging if asked for
				// If configuration was already done by a .config file, nothing will be changed
				Utilities.ConfigureLogging(config.debug);

				// Load the platform assembly
				LoadPlatformAssembly();

				if (agent != null)
				{
					OnRunAgent(agent);
				}
				else if (analyzer != null)
				{
					OnRunAnalyzer(analyzer);
				}
				else
				{
					OnRunJob(test, extra);
				}

				exitCode = 0;
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
				if (config.debug > 0)
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
		protected virtual void RunEngine()
		{
			var e = new Engine(GetUIWatcher());

			e.startFuzzing(dom, config);
		}

		/// <summary>
		/// Override to add custom options
		/// </summary>
		/// <param name="options"></param>
		protected virtual void AddCustomOptions(OptionSet options)
		{
		}

		/// <summary>
		/// Run a command line analyzer of the specified name
		/// </summary>
		/// <param name="analyzer"></param>
		protected virtual void OnRunAnalyzer(string analyzer)
		{
			var analyzerType = ClassLoader.FindTypeByAttribute<AnalyzerAttribute>((x, y) => y.Name == analyzer);
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
		protected virtual void OnRunAgent(string agent)
		{
			var agentType = ClassLoader.FindTypeByAttribute<AgentServerAttribute>((x, y) => y.name == agent);
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
				config.pitFile = extra[0];

			if (extra.Count > 1)
				config.runName = extra[1];

			AddNewDefine("Peach.Cwd=" + Environment.CurrentDirectory);
			AddNewDefine("Peach.Pwd=" + Utilities.ExecutionDirectory);

			// Do we have pit.xml.config file?
			// If so load it as the first defines file.
			if (extra.Count > 0 && File.Exists(extra[0]) &&
				extra[0].ToLower().EndsWith(".xml") &&
				File.Exists(extra[0] + ".config"))
			{
				configFiles.Insert(0, extra[0] + ".config");
			}

			var defs = ParseDefines();

			var parserArgs = new Dictionary<string, object>();
			parserArgs[PitParser.DEFINED_VALUES] = defs;

			var parser = GetParser();

			if (test)
			{
				ConsoleWatcher.WriteInfoMark();
				Console.Write("Validating file [" + config.pitFile + "]... ");
				parser.asParserValidation(parserArgs, config.pitFile);
				Console.WriteLine("No Errors Found.");
			}
			else
			{
				dom = parser.asParser(parserArgs, config.pitFile);

				RunEngine();
			}
		}

		/// <summary>
		/// Combines define files and define arguments into a single list
		/// Command line arguments override any .config file's defines
		/// </summary>
		/// <returns></returns>
		protected virtual List<KeyValuePair<string, string>> ParseDefines()
		{
			var items = new List<List<KeyValuePair<string, string>>>();

			// Add .config files in order
			foreach (var file in configFiles)
			{
				items.Add(PitParser.parseDefines(file));
			}

			// Add -D command line options last
			items.Add(definedValues);

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
			string syntax =
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

  -1                         Perform a single iteration
  -a,--agent CHANNEL         Launch Peach Agent
  -c,--count                 Count test cases
  -t,--test PIT              Validate a Peach Pit file
  -p,--parallel M,N          Parallel fuzzing.  Total of M machines, this
                             is machine N.
  --debug                    Enable debug messages. Usefull when debugging
                             your Peach Pit file.  Warning: Messages are very
                             cryptic sometimes.
  --trace                    Enable even more verbose debug messages.
  --seed N                   Sets the seed used by the random number generator
  --parseonly                Test parse a Peach Pit file
  --makexsd                  Generate peach.xsd
  --showenv                  Print a list of all DataElements, Fixups, Agents
                             Publishers and their associated parameters.
  --showdevices              Display the list of PCAP devices
  --analyzer                 Launch Peach Analyzer
  --skipto N                 Skip to a specific test #.  This replaced -r
                             for restarting a Peach run.
  --range N,M                Provide a range of test #'s to be run.
  -D/define=KEY=VALUE        Define a substitution value.  In your PIT you can
                             ##KEY## and it will be replaced for VALUE.
  --config=FILENAME          XML file containing defined values
  --pits=PIT_LIBRARY_PATH    The path to the Pit library.
  --noweb                    Disable the Peach web interface.

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
			Console.WriteLine(syntax);
			throw new SyntaxException();
		}

		#region Command line option parsing helpers

		protected uint ParseUInt(string arg, string v)
		{
			uint ret;

			if (!uint.TryParse(v, out ret))
				throw new PeachException(
					"An invalid option for --{0} was specified.  The value '{1}' could not be converted to an unsigned integer.".Fmt(
						arg, v));

			return ret;
		}

		protected void ParseRange(string arg, string v)
		{
			string[] parts = v.Split(',');
			if (parts.Length != 2)
				throw new PeachException("Invalid range: " + v);

			try
			{
				config.rangeStart = Convert.ToUInt32(parts[0]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range start iteration: " + parts[0], ex);
			}

			try
			{
				config.rangeStop = Convert.ToUInt32(parts[1]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range stop iteration: " + parts[1], ex);
			}

			if (config.parallel)
				throw new PeachException("--range is not supported when --parallel is specified");

			config.range = true;
		}

		protected void ParseParallel(string arg, string v)
		{
			string[] parts = v.Split(',');
			if (parts.Length != 2)
				throw new PeachException("Invalid parallel value: " + v);

			try
			{
				config.parallelTotal = Convert.ToUInt32(parts[0]);

				if (config.parallelTotal == 0)
					throw new ArgumentOutOfRangeException();
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid parallel machine total: " + parts[0], ex);
			}

			try
			{
				config.parallelNum = Convert.ToUInt32(parts[1]);
				if (config.parallelNum == 0 || config.parallelNum > config.parallelTotal)
					throw new ArgumentOutOfRangeException();
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid parallel machine number: " + parts[1], ex);
			}

			if (config.range)
				throw new PeachException("--parallel is not supported when --range is specified");

			config.parallel = true;
		}

		protected void AddNewDefine(string arg)
		{
			var idx = arg.IndexOf("=");
			if (idx < 0)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var key = arg.Substring(0, idx);
			var value = arg.Substring(idx + 1);

			// Allow command line options to override others
			definedValues.RemoveAll(i => i.Key == key);
			definedValues.Add(new KeyValuePair<string, string>(key, value));
		}

		#endregion

		#region Global Actions

		static void Bob()
		{
			string bob = @"
@@@@@@@^^~~~~~~~~~~~~~~~~~~~~^@@@@@@@@@
@@@@@@^     ~^  @  @@ @ @ @ I  ~^@@@@@@
@@@@@            ~ ~~ ~I          @@@@@
@@@@'                  '  _,w@<    @@@@
@@@@     @@@@@@@@w___,w@@@@@@@@  @  @@@
@@@@     @@@@@@@@@@@@@@@@@@@@@@  I  @@@
@@@@     @@@@@@@@@@@@@@@@@@@@*@[ i  @@@
@@@@     @@@@@@@@@@@@@@@@@@@@[][ | ]@@@
@@@@     ~_,,_ ~@@@@@@@~ ____~ @    @@@
@@@@    _~ ,  ,  `@@@~  _  _`@ ]L  J@@@
@@@@  , @@w@ww+   @@@ww``,,@w@ ][  @@@@
@@@@,  @@@@www@@@ @@@@@@@ww@@@@@[  @@@@
@@@@@_|| @@@@@@P' @@P@@@@@@@@@@@[|c@@@@
@@@@@@w| '@@P~  P]@@@-~, ~Y@@^'],@@@@@@
@@@@@@@[   _        _J@@Tk     ]]@@@@@@
@@@@@@@@,@ @@, c,,,,,,,y ,w@@[ ,@@@@@@@
@@@@@@@@@ i @w   ====--_@@@@@  @@@@@@@@
@@@@@@@@@@`,P~ _ ~^^^^Y@@@@@  @@@@@@@@@
@@@@^^=^@@^   ^' ,ww,w@@@@@ _@@@@@@@@@@
@@@_xJ~ ~   ,    @@@@@@@P~_@@@@@@@@@@@@
@@   @,   ,@@@,_____   _,J@@@@@@@@@@@@@
@@L  `' ,@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
";
			Console.WriteLine(bob);
			throw new SyntaxException();
		}


		static void Charlie()
		{
			Console.WriteLine(@"
,-----.   
\======'.                                                                 
 \  {}   '.                                                               
  \   \/ V '.                                                             
   \  || |   '._                                 _,cmmmnc,_               
    \___68FS___\'-._=----+- _______________,.-=:3H)###C--  `c._           
    :|=--------------`---" + "\"" + @"'`.   `  `.   `.   `,   `~\" + "\"\"" + @"===" + "\"" + @"~`    `'-.___   
  ,dH] '       =(*)=         :       ---==;=--;  .   ;    +-- -_ .-`      
  :HH]_:______________  ____,.........__     _____,.----=-" + "\"" + @"~ `            
  ;:" + "\"" + @"+" + "\"" + @"\" + "\"" + @"+@" + "\"" + @"" + "\"" + @"+" + "\"" + @"\" + "\"" + @"" + "\"" + @"+@" + "\"" + @"'" + "\"" + @"+" + "\"" + @"\" + "\"" + @"+@" + "\"" + @"'----._.------\`  :          .   `.'`'" + "\"" + @"'" + "\"" + @"'" + "\"" + @"P
  |:      .-'==-.__)___\. :        .   .'`___L~___(                       
  |:  _.'`       '|   / \.:      .  .-`" + "\"" + @"" + "\"" + @"`                                
  `'" + "\"" + @"'            `--'   \:    ._.-'                                      
                         }_`============>-             
");
			throw new SyntaxException();
		}

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
