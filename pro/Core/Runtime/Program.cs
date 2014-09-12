
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
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;

using System.Linq;

using Peach.Core.Dom;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;

using SharpPcap;
using NLog;
using NLog.Targets;
using NLog.Config;
using System.Threading;

namespace Peach.Core.Runtime
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
		protected Dom.Dom dom = null;

		/// <summary>
		/// Configuration options for the engine
		/// </summary>
		protected RunConfiguration config = new RunConfiguration();

		/// <summary>
		/// The exit code of the process
		/// </summary>
		public int exitCode = 1;

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
			Peach.Core.AssertWriter.Register();

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
					{ "range=", v => ParseRange(v)},
					{ "c|count", v => config.countOnly = true},
					{ "skipto=", v => config.skipToIteration = Convert.ToUInt32(v)},
					{ "seed=", v => config.randomSeed = Convert.ToUInt32(v)},
					{ "p|parallel=", v => ParseParallel(v)},

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

				List<string> extra = p.Parse(args);

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
			catch (SyntaxException)
			{
				// Ignore, thrown by syntax()
			}
			catch (OptionException oe)
			{
					Console.WriteLine(oe.Message +"\n"); 
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

			var analyzerInstance = Activator.CreateInstance(analyzerType) as Analyzer;

			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting Analyzer");

			analyzerInstance.asCommandLine(new Dictionary<string, string>());
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

			var agentServer = Activator.CreateInstance(agentType) as IAgentServer;

			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting agent server");

			agentServer.Run(new Dictionary<string, string>());
		}

		/// <summary>
		/// Run a fuzzing job
		/// </summary>
		/// <param name="test">Test only. Parses the pit and exits.</param>
		/// <param name="extra">Extra command line options</param>
		protected virtual void OnRunJob(bool test, List<string> extra)
		{
			Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

			if (extra.Count == 0)
				Syntax();

			if (extra.Count > 0)
				config.pitFile = extra[0];

			if (extra.Count > 1)
				config.runName = extra[1];

			AddNewDefine("Peach.Cwd=" + Environment.CurrentDirectory);
			AddNewDefine("Peach.Pwd=" + Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));

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
			throw new SyntaxException();
		}

		#region Command line option parsing helpers

		protected void ParseRange(string v)
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

		protected void ParseParallel(string v)
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
			Console.WriteLine();
			Console.WriteLine("The following devices are available on this machine:");
			Console.WriteLine("----------------------------------------------------");
			Console.WriteLine();

			int i = 0;

			var devices = CaptureDeviceList.Instance;

			// Print out all available devices
			foreach (ICaptureDevice dev in devices)
			{
				Console.WriteLine("Name: {0}\nDescription: {1}\n\n", dev.Name, dev.Description);
				i++;
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
					Xsd.SchemaBuilder.Generate(typeof(Xsd.Dom), stream);

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
			Console.WriteLine();
			Console.WriteLine(" --- Ctrl+C Detected --- ");

			e.Cancel = true;

			// Need to call Environment.Exit from outside this event handler
			// to ensure the finalizers get called...
			// http://www.codeproject.com/Articles/16164/Managed-Application-Shutdown
			new Thread(delegate()
			{
				Environment.Exit(0);
			}).Start();
		}
	}
}

// end
