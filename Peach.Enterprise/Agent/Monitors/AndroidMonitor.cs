﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Peach.Core.Agent;
using Managed.Adb;
using Peach.Core;
using System.Text.RegularExpressions;
using NLog;
using System.Diagnostics;
using System.IO;

namespace Peach.Enterprise.Agent.Monitors
{
	[Monitor("Android", true)]
	[Parameter("ApplicationName", typeof(string), "Android Application")]
	[Parameter("AdbPath", typeof(string), "Directory Path to Adb")]
	[Parameter("ActivityName", typeof(string), "Application Activity", "")]
	[Parameter("RestartEveryIteration", typeof(bool), "Restart Application on Every Iteration (defaults to true)", "true")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart Application after Faults (defaults to true)", "true")]
	
	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private Fault _fault = null;
		private Device _dev = null;
		private FileEntry _tombs = null;
		private int _retries = 20;
		private ConsoleOutputReceiver _creciever = null;

		private Regex reHash = new Regex(@"^backtrace:((\r)?\n    #([^\r\n])*)*", RegexOptions.Multiline);

		public string ApplicationName { get; private set; }
		public string AdbPath { get; private set; }
		public string ActivityName { get; private set; }
		public bool RestartEveryIteration { get; private set; }
		public bool RestartAfterFault { get; private set; }

		public AndroidMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
			_creciever = new ConsoleOutputReceiver();
		}

		//TODO Duplicate method
		private void grabDevice()
		{
			try
			{
				_dev = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)[0];
				var path = "/data/tombstones/";
				_tombs = FileEntry.FindOrCreate(_dev, path);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.Message);
			}
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
			if (type.Equals("dismiss"))
			{
				_dev.ExecuteShellCommand("input keyevent 66", creciever); //Enter
				Thread.Sleep(250);
				_dev.ExecuteShellCommand("input keyevent 66", creciever); //Enter
			}
			else if (type.Equals("unlock"))
			{
				_dev.ExecuteShellCommand("input keyevent 82", creciever); //Menu
			}
		}

		private void restartApp()
		{
			string cmd = "am start -S -n " + ApplicationName;
			if (!string.IsNullOrEmpty(ActivityName))
			{
				cmd = cmd + "/" + ActivityName;
			}
			cmd += " && sleep 2";
			_dev.ExecuteShellCommand(cmd, _creciever);
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

		private byte[] imageClean(byte[] unclean)
		{
			List<byte> clean = new List<byte>();
			int i = 0;
			while (i < unclean.Length - 1)
			{
				if (unclean[i] != '\r' || unclean[i + 1] != '\n')
				{
					clean.Add(unclean[i]);
					i += 1;
				}
				else
				{
					clean.Add(unclean[i+1]);
					i += 2;
				}
			}
			return clean.ToArray();
		}

		private void controlAdb(string command)
		{
			Process p = new Process();
			p.StartInfo.FileName = Path.Combine(AdbPath, "adb");
			p.StartInfo.Arguments = command;
			p.StartInfo.UseShellExecute = false;
			p.Start();
			p.WaitForExit();
		}

		private void restartAdb()
		{
			controlAdb("kill-server");
			controlAdb("start-server");
		}

		public override void SessionStarting()
		{
			restartAdb();
			grabDevice();
			CleanTombs();
			CleanLogs();
			if (!RestartEveryIteration)
			{
				restartApp();
			}
		}

		public override void SessionFinished()
		{
		}

		public override bool DetectedFault()
		{
			_fault = new Fault();
			_fault.type = FaultType.Fault;
			_fault.detectionSource = "AndroidMonitor";
			_fault.exploitability = "Unknown";

			try
			{
				CommandResultReceiver creciever = new CommandResultReceiver();
				_dev.ExecuteShellCommand("logcat -s -d AndroidRuntime:e ActivityManager:e *:f", creciever);

				_fault.type = FaultType.Data;
				_fault.title = "Response";
				_fault.description = creciever.Result;

				if (!string.IsNullOrEmpty(_fault.description))
					_fault.type = FaultType.Fault;
			}
			catch (Exception ex)
			{
				_fault.title = "Exception";
				_fault.description = ex.Message;
				_fault.type = FaultType.Fault;
			}

			return _fault.type == FaultType.Fault;

		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			bool restart = false;
			if (RestartEveryIteration || (_fault != null && _fault.type == FaultType.Fault && RestartAfterFault))
			{
				restart = true;
			}
			
			_fault = null;

			if (restart)
			{
				try
				{
					restartApp();
				}
				catch (Managed.Adb.Exceptions.DeviceNotFoundException ex)
				{
					restartAdb();
					grabDevice();
					throw new SoftException(ex);
				}
				catch (Exception ex)
				{
					throw new SoftException("Unable to Restart Android Application:\n" + ex);
				}
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
			CommandResultReceiver creciever = null;
			CommandResultBinaryReceiver breciever = null;

			// STEP 1: Get 1st Screenshot
			try
			{
				breciever = new CommandResultBinaryReceiver();
				_dev.ExecuteShellCommand("screencap -p", breciever);
				_fault.collectedData.Add(new Fault.Data("screenshot1.png", imageClean(breciever.Result)));
			}
			catch (Exception ex)
			{
				logger.Warn("AndroidScreenshot: Warn, Unable to capture first screenshot:\n" + ex);
			}


			// STEP 2: Wait for boot if system crashed.
			// Make sure the device is ready
			int i = 0;
			bool screenLocked = false;
			try
			{
				while (!devIsReady())
				{
					screenLocked = true;
					Thread.Sleep(1000);
					i += 1;
					if (i >= _retries)
					{
						throw new SoftException("Unable to Communicate with Device after " + _retries.ToString() + " attempts.");
					}
				}
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
			//TODO: This part is stupid, this is not the right place for this, and the sleep is totally arbitrary
			// Replace with a blocking call that makes sure the ui has fully started
			// Also this makes the assumption that it is not ready because of a system crash.  That could totally not be true
			if (screenLocked)
			{
				Thread.Sleep(60000);
				try
				{
					restartAdb();
					grabDevice();
					CleanUI("unlock");
				}
				catch (Exception ex)
				{
					throw new SoftException("Unable to Clean Android UI:\n" + ex);
				}
			}

			// STEP 3: Grab Tomb
			try
			{

				foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
				{
					creciever = new CommandResultReceiver();
					_dev.ExecuteShellCommand("cat " + tomb.FullPath, creciever);
					string tombstr = creciever.Result;

					var hash = reHash.Match(tombstr);
					if (hash.Success)
					{
						// TODO not real major minor hashes
						// Also these wont bucket when from a linux and windows node
						// Since it will have different newlines and thus different hashcodes
						_fault.majorHash = hash.Groups[0].Value.GetHashCode().ToString();
						_fault.minorHash = hash.Groups[0].Value.GetHashCode().ToString();
					}
					_fault.collectedData.Add(new Fault.Data(tomb.FullPath, System.Text.Encoding.ASCII.GetBytes(tombstr)));
					// TODO: this might be ok, since a core dump implies it was a native crash
					// And native crashes are the only crashes that need to physically dismiss the message
					if (!screenLocked)
					{
						Thread.Sleep(1000);
						CleanUI("dismiss");
					}
				}
				CleanTombs();
				CleanLogs();
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}

			// STEP 4: Get 2nd ScreenShot
			try
			{
				breciever = new CommandResultBinaryReceiver();
				_dev.ExecuteShellCommand("screencap -p", breciever);
				_fault.collectedData.Add(new Fault.Data("screenshot2.png", imageClean(breciever.Result)));
			}
			catch (Exception ex)
			{
				logger.Warn("AndroidScreenshot: Warn, Unable to capture second screenshot:\n" + ex.Message);
			}

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
