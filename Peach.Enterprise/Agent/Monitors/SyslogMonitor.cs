using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

using LibSyslog = Syslog.Server;

using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;

namespace Peach.Enterprise.Agent.Monitors
{
	/// <summary>
	/// Start a syslog server to receive log messages. Messages are logged when a fault occurs and the
	/// log buffer reset. Logs are stored temporarily on disk.
	/// </summary>
	[Monitor("Syslog", true)]
	[Description("Syslog collection service")]
	[Parameter("Port", typeof(int), "Port number to listen on", "514")]
	[Parameter("Interface", typeof(string), "Interface to listen on", "0.0.0.0")]
	[Parameter("FaultRegex", typeof(string), "Fault when regular expression matches", "")]
	public class SyslogMonitor : Peach.Core.Agent.Monitor
	{
		//string _interface;
		//int _port;
		LibSyslog.Listener _syslog;
		FileStream _logfile;
		string _logfileName;
		int _messageCount = 0;
		Fault _fault = null;
		Regex _faultRegex = null;

		public int Port { get; set; }
		public string Interface { get; set; }
		public string FaultRegex { get; set; }

		public SyslogMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			if (!string.IsNullOrEmpty(FaultRegex))
				_faultRegex = new Regex(FaultRegex);
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		public override void SessionStarting()
		{
			_logfileName = Path.GetTempFileName();
			_logfile = File.OpenWrite(_logfileName);
			_syslog = LibSyslog.Listener.CreateInstance(Interface, Port, 30);
			LibSyslog.Listener.MessageReceived += Listener_MessageReceived;
			_syslog.Start();
		}

		void Listener_MessageReceived(LibSyslog.MessageReceivedEventArgs e)
		{
			lock (_syslog)
			{
				var msgText = e.SyslogMessage.ToString();
				var msg = System.Text.UTF8Encoding.UTF8.GetBytes(msgText);
				_logfile.Write(msg, 0, msg.Length);

				if (_faultRegex != null && _faultRegex.IsMatch(msgText))
				{
					_fault = new Fault();
					_fault.type = FaultType.Fault;
					_fault.exploitability = "Unknown";
					_fault.title = "FaultRegex matched syslog message";
					_fault.description = "Regular expression matched input text: vvvvvvvvvvvvvvv\n" + msgText
																			 + "\n^^^^^^^^^^^^^^^\n";
					_fault.collectedData.Add(new Fault.Data("SyslogMonitor-" + Name + "-FaultMsg.txt", msg));
				}
			}
		}

		public override void SessionFinished()
		{
			lock (_syslog)
			{
				if (_syslog != null)
				{
					_syslog.Stop();
					_syslog = null;
				}
			}

			if (_logfile != null)
				_logfile.Dispose();
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
		}

		public override bool IterationFinished()
		{
			return false;
		}

		public override bool DetectedFault()
		{
			return _fault != null;
		}

		public override Fault GetMonitorData()
		{
			lock (_syslog)
			{
				// Get data from log file

				byte [] data = new byte[_logfile.Length];

				_logfile.Seek(0, SeekOrigin.Begin);
				_logfile.Read(data, 0, (int)_logfile.Length);

				_logfile.Dispose();
				File.Delete(_logfileName);

				// Generate fault

				Fault fault;
				if (_fault == null)
				{
					fault = new Fault();
					fault.type = FaultType.Data;
					fault.description = "Collected " + _messageCount + " messages.";
					fault.title = "Collected " + _messageCount + " syslog messages.";
				}
				else
				{
					fault = _fault;
					_fault = null;
				}

				fault.monitorName = Name;
				fault.detectionSource = "SyslogMonitor-" + Name;
				fault.folderName = "SyslogMonitor-" + Name;
				fault.collectedData.Add(new Fault.Data("SyslogMonitor-" + Name + ".txt", data));

				try
				{
					_logfile = File.OpenWrite(_logfileName);
				}
				catch
				{
				}

				return fault;
			}
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
	}
}

// end
