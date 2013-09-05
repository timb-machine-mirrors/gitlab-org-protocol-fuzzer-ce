﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Peach.Core.Agent;
using Managed.Adb;
using Peach.Core;

namespace Peach.Enterprise.Agent.Monitors
{
	[Monitor("Android", true)]
	[Parameter("CmdOnStart", typeof(string), "Command on beginning of each iteration.", "")]
	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		private Fault _fault = null;
		private Device _dev = null;
		private FileEntry _tombs = null;
		private int _retries = 20;

		public string CmdOnStart { get; private set; }

		public AndroidMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
		}

		private void CleanTombs(){
			foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
			{
				_dev.FileSystem.Delete(tomb);
			}
		}

		private void CleanLogs()
		{
			CommandResultReceiver creciever = new CommandResultReceiver();
			_dev.ExecuteShellCommand("logcat -c", creciever);
		}

		private void CleanUI(string type)
		{
			CommandResultReceiver creciever = new CommandResultReceiver();
			if (type.Equals("dismiss")){
				_dev.ExecuteShellCommand("input keyevent 66", creciever); //Enter
				_dev.ExecuteShellCommand("input keyevent 66", creciever); //Enter
			}
			else if (type.Equals("unlock")){
				_dev.ExecuteShellCommand("input keyevent 82", creciever); //Menu
			}
		}

		private bool devIsReady()
		{
			// HACK: none of the api's functions will reliably tell this.
			// _dev.isOnline() totally should work
			// or _dev.State == "Online"
			// But nope, those show it as always online.
 			// This works though, but it totally sucks.
			CommandResultReceiver creciever = new CommandResultReceiver();
			_dev.ExecuteShellCommand("monkey 0", creciever);
			if (creciever.Result.Equals("** Error: Unable to connect to activity manager; is the system running?") ||
				creciever.Result.Equals("** Error: Unable to connect to window manager; is the system running?"))
			{
				return false;
			}
			return true;
		}

		public override void SessionStarting()
		{
			_dev = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)[0];
			var path = "/data/tombstones/";
			_tombs = FileEntry.Find(_dev, path);
			CleanTombs();
			CleanLogs();
		}

		public override void SessionFinished()
		{
		}

		public override bool DetectedFault()
		{
			_fault = new Fault();
			_fault.type = FaultType.Fault;
			_fault.detectionSource = "AndroidMonitor";
			_fault.folderName = "AndroidMonitor";

			try
			{
				_fault.type = FaultType.Data;
				_fault.title = "Response";
				CommandResultReceiver creciever = new CommandResultReceiver();
				_dev.ExecuteShellCommand("logcat -s -d AndroidRuntime:e ActivityManager:e *:f", creciever);
				_fault.description = creciever.Result;
				if (_fault.description.Length > 0)
				{
					_fault.type = FaultType.Fault;
				}
			}
			catch (Exception ex)
			{
				_fault.title = "Exception";
				_fault.description = ex.Message;
			}
			return _fault.type == FaultType.Fault;

		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			_fault = null;

			// Make sure the device is ready
			int i = 0;
			bool screenLocked = false;
			while (!devIsReady()){
				screenLocked = true;
				Thread.Sleep(1000);
				i += 1;
				if (i >= _retries)
				{
					throw new SoftException("Unable to Communicate with Device after " + _retries.ToString() + " attempts.");
				}
			}
			//TODO: This part is stupid, this is not the right place for this, and the sleep is totally arbitrary
			// Replace with a blocking call that makes sure the ui has fully started
			// Also this makes the assumption that it is not ready because of a system crash.  That could totally not be true
			if (screenLocked)
			{
				Thread.Sleep(60000);
				CleanUI("unlock");
			}

			if (CmdOnStart != "")
			{
				ConsoleOutputReceiver creciever = new ConsoleOutputReceiver();
				_dev.ExecuteShellCommand(CmdOnStart, creciever);
			}
		}

		public override bool IterationFinished()
		{
			return false;
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		public override Fault GetMonitorData()
		{
			// Grab Tomb
			foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
			{
				CommandResultReceiver creciever = new CommandResultReceiver();
				_dev.ExecuteShellCommand("cat " + tomb.FullPath, creciever);
				string tombstr = creciever.Result;
				_fault.collectedData.Add(new Fault.Data(tomb.FullPath, System.Text.Encoding.ASCII.GetBytes(tombstr)));
				// TODO: this might be ok, since a core dump implies it was a native crash
				// And native crashes are the only crashes that need to physically dismiss the message
				Thread.Sleep(1000);
				CleanUI("dismiss");
			}
			CleanTombs();
			CleanLogs();
			return _fault;
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
