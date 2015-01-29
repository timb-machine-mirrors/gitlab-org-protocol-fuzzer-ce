using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("RunCommand", true)]
	[Description("Launches the specified command to perform a utility function")]
	[Parameter("Command", typeof(string), "Command line command to run")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("When", typeof(MonitorWhen), "Period _When the command should be ran (OnCall, OnStart, OnEnd, OnIterationStart, OnIterationEnd, OnFault, OnIterationStartAfterFault)", "OnCall")]
	[Parameter("StartOnCall", typeof(string), "Run when signaled by the state machine", "")]
	[Parameter("FaultOnExitCode", typeof(bool), "Fault when FaultExitCode matches exit code", "false")]
	[Parameter("FaultExitCode", typeof(int), "Exit code to fault on", "1")]
	[Parameter("FaultOnNonZeroExit", typeof(bool), "Fault if exit code is non-zero", "false")]
	[Parameter("FaultOnRegex", typeof(string), "Fault if regex matches", "")]
	[Parameter("AddressSanitizer", typeof(bool), "Enable Google AddressSanitizer support", "false")]
	[Parameter("Timeout", typeof(int), "Fault if process takes more than Timeout seconds where -1 is infinite timeout ", "-1")]
	public class RunCommand  : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Command { get; set; }
		public string Arguments { get; set; }
		public string StartOnCall { get; set; }
		public MonitorWhen When { get; set; }
		public string CheckValue { get; set; }
		public int Timeout { get; set; }
		public bool FaultOnNonZeroExit { get; set; }
		public int FaultExitCode { get; set; }
		public bool FaultOnExitCode { get; set; }
		public string FaultOnRegex { get; set; }
		public bool AddressSanitizer { get; set; }

		static readonly Regex AsanMatch = new Regex(@"==\d+==ERROR: AddressSanitizer:");
		static readonly Regex AsanBucket = new Regex(@"==\d+==ERROR: AddressSanitizer: ([^\s]+) on address ([0-9a-z]+) at pc ([0-9a-z]+)");
		static readonly Regex AsanMessage = new Regex(@"(==\d+==ERROR: AddressSanitizer:.*==\d+==ABORTING)");
		static readonly Regex AsanTitle = new Regex(@"==\d+==ERROR: AddressSanitizer: ([^\r\n]+)");

		private Regex _faulOnRegex;
		private Fault _fault;

		public RunCommand(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (!string.IsNullOrWhiteSpace(FaultOnRegex))
				_faulOnRegex = new Regex(FaultOnRegex);
		}

		void _Start()
		{
			_fault = null;

			Logger.Debug("_Start(): Running command " + Command + " with arguments " + Arguments);

			try
			{
				var p = SubProcess.Run(Command, Arguments, Timeout);

				var stdout = p.StdOut.ToString();
				var stderr = p.StdErr.ToString();

				_fault = new Fault
				{
					detectionSource = "RunCommand",
					folderName = "RunCommand"
				};

				_fault.collectedData.Add(new Fault.Data("stdout", System.Text.Encoding.ASCII.GetBytes(stdout)));
				_fault.collectedData.Add(new Fault.Data("stderr", System.Text.Encoding.ASCII.GetBytes(stderr)));

				if (p.Timeout)
				{
					_fault.title = _fault.description = "Process failed to exit in allotted time.";
					_fault.type = FaultType.Fault;
				}
				else if (FaultOnExitCode && p.ExitCode == FaultExitCode)
				{
					_fault.title = _fault.description = "Process exited with code {0}.".Fmt(p.ExitCode);
					_fault.type = FaultType.Fault;
				}
				else if (FaultOnNonZeroExit && p.ExitCode != 0)
				{
					_fault.title = _fault.description = "Process exited with code {0}.".Fmt(p.ExitCode);
					_fault.type = FaultType.Fault;
				}
				else if (_faulOnRegex != null)
				{
					if (_faulOnRegex.Match(stdout).Success)
					{
						_fault.title = _fault.description = "Process output matched FaulOnRegex \"{0}\".".Fmt(FaultOnRegex);
						_fault.type = FaultType.Fault;
					}
					else if (_faulOnRegex.Match(stderr).Success)
					{
						_fault.title = _fault.description = "Process error output matched FaulOnRegex \"{0}\".".Fmt(FaultOnRegex);
						_fault.type = FaultType.Fault;
					}
				}
				else if (AddressSanitizer && AsanMatch.IsMatch(stderr))
				{
					_fault.type = FaultType.Fault;
					_fault.folderName = null;

					var match = AsanBucket.Match(stderr);
					_fault.exploitability = match.Groups[1].Value;
					_fault.majorHash = match.Groups[3].Value;
					_fault.minorHash = match.Groups[2].Value;

					match = AsanTitle.Match(stderr);
					_fault.title = match.Groups[1].Value;

					match = AsanMessage.Match(stderr);
					_fault.description = stderr.Substring(match.Groups[1].Index, match.Groups[1].Length);
				}
				else
				{
					_fault.description = stdout;
					_fault.type = FaultType.Data;
				}
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not run command '" + Command + "'.  " + ex.Message + ".", ex);
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (When == MonitorWhen.OnIterationStart ||
				(args.LastWasFault && When == MonitorWhen.OnIterationStartAfterFault))
				_Start();
		}

		public override bool DetectedFault()
		{
			return _fault != null && _fault.type == FaultType.Fault;
		}

		public override Fault GetMonitorData()
		{
			if (When == MonitorWhen.OnFault)
				_Start();

			return _fault;
		}

		public override void SessionStarting()
		{
			if (When == MonitorWhen.OnStart)
				_Start();
		}

		public override void SessionFinished()
		{
			if (When == MonitorWhen.OnEnd)
				_Start();
		}

		public override void IterationFinished()
		{
			if (When == MonitorWhen.OnIterationEnd)
				_Start();
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall && When == MonitorWhen.OnCall)
				_Start();
		}
	}
}
