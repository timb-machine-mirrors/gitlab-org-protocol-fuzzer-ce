using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Peach.Core.Agent;
using Peach.Enterprise;
using Managed.Adb;
using Peach.Core;
using System.Text.RegularExpressions;
using NLog;

namespace Peach.Enterprise.Agent.Monitors
{
	[Monitor("Android", true)]
	[Parameter("ApplicationName", typeof(string), "Android Application")]
	[Parameter("AdbPath", typeof(string), "Directory Path to Adb", "")]
	[Parameter("DeviceSerial", typeof(string), "The Serial of the device to fuzz", "")]
	[Parameter("ActivityName", typeof(string), "Application Activity", "")]
	[Parameter("RestartEveryIteration", typeof(bool), "Restart Application on Every Iteration (defaults to true)", "true")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart Application after Faults (defaults to true)", "true")]

	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static Regex logFilter = new Regex(@"^-+ beginning of (/\w+)+(\r\n|\r|\n)?", RegexOptions.Multiline);
		private Regex reHash = new Regex(@"^backtrace:((\r)?\n    #([^\r\n])*)*", RegexOptions.Multiline);
		private Regex amProcessSuccess = new Regex(@".*\n(Status: ok)\r?\n.*\nComplete(\r)?\n?", RegexOptions.Multiline | RegexOptions.Singleline);
		private Regex amProcessFailure = new Regex(@".*\Error: (.*?)\r?\n?", RegexOptions.Multiline | RegexOptions.Singleline);

		private Fault _fault = null;
		private Device _dev = null;
		private FileEntry _tombs = null;
		private int _retries = 200;
		private bool _muststop = false;
		private ConsoleOutputReceiver _creciever = null;
		private CommandResultReceiver _cmdreciever = null;

		public string ApplicationName { get; private set; }
		public string AdbPath { get; private set; }
		public string ActivityName { get; private set; }
		public string DeviceSerial { get; set; }
		public bool RestartEveryIteration { get; private set; }
		public bool RestartAfterFault { get; private set; }

		public AndroidMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
			AndroidBridge.SetAdbPath(AdbPath);
			_creciever = new ConsoleOutputReceiver();
			_cmdreciever = new CommandResultReceiver();
		}


		private void CleanTombs(){
			foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
			{
				// this can fail, add exception handling
				_dev.FileSystem.Delete(tomb);
			}
		}

		private void CleanLogs()
		{
			// this can fail, add exception handling
			_dev.ExecuteShellCommand("logcat -c", _creciever);
		}

		private void CleanUI(string type)
		{
			string _cmd = "";
			if (type.Equals("dismiss"))
			{
				logger.Debug("Clearing UI running 2x " + _cmd);
				_cmd = "input keyevent 66";
				// this can fail, add exception handling
				_dev.ExecuteShellCommand(_cmd, _creciever); //Enter
				Thread.Sleep(250);
				// this can fail, add exception handling
				_dev.ExecuteShellCommand(_cmd, _creciever); //Enter
			}
			else if (type.Equals("unlock"))
			{
				_cmd = "input keyevent 82";
				// this can fail, add exception handling
				_dev.ExecuteShellCommand(_cmd, _creciever); //Menu
			}
		}

		private void StartApp()
		{
			string cmd = "am start -W -S -n " + ApplicationName;
			if (!string.IsNullOrEmpty(ActivityName))
			{
				cmd = cmd + "/" + ActivityName;
			}
			// this can fail, add exception handling.
			// ways it could fail
			// 1. no device
			// 2. adb not there
			// ..
			_dev.ExecuteShellCommand(cmd, _cmdreciever);
			if (!string.IsNullOrEmpty(_cmdreciever.Result))
			{
				if (!amProcessSuccess.Match(_cmdreciever.Result).Success)
				{
					Match m = amProcessFailure.Match(_cmdreciever.Result);
					if (m.Success)
						throw new PeachException("Activity Manager failed to start application: \n" + m);
					else
						throw new PeachException("Peach failed to parse response output of Activity manager: \n" + _cmdreciever.Result);
				}
			}
			else
				throw new PeachException("Shell command `" + cmd +"` had no output");
		}


		private bool devRestarted()
		{
			// what? This doesn't do what it says it does....
			bool restarted = false;
			int i = 0;
			while (i < _retries)
			{
				try
				{
					_dev.ExecuteShellCommand("getprop init.svc.bootanim", _cmdreciever);
				}
				catch
				{
					try
					{
						if (!string.Equals(_dev.State,"Online"))
							Thread.Sleep(1000);
					}
					catch
					{
					}
					continue;
				}

				if (!_cmdreciever.Result.Equals("running"))
				{
					return restarted;
				}
				else
				{
					restarted = true;
					Thread.Sleep(1000);
					i += 1;
				}
			}
			throw new Exception("Unable to Communicate with Android Device after " + i.ToString() + " attempts.");
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


		private void restartAdb()
		{
			// this didn't actually perform the function that was stated by the function name. fixed -JD
			AndroidBridge.RestartADB();
		}

		public override void SessionStarting()
		{
			_dev = AndroidBridge.GetDevice(DeviceSerial);
			if (_dev.CanSU())
			{
				var path = "/data/tombstones/";
				_tombs = FileEntry.FindOrCreate(_dev, path);
				if (!_tombs.IsDirectory)
					throw new PeachException("Tomb path " + _tombs + " could not be found or created");
				CleanTombs();
			}
			// file creation can silently fail
			CleanLogs();
			if (!RestartEveryIteration)
			{
				StartApp();
			}
		}

		public override void SessionFinished()
		{
		}

		public override bool DetectedFault()
		{
			return _fault.type == FaultType.Fault;
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			// needs to check if the system is booted and unlocked
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
					StartApp();
				}
				catch (Managed.Adb.Exceptions.DeviceNotFoundException ex)
				{
					restartAdb();
					throw new SoftException(ex);
				}
				// We don't want to just swollow exceptions 
				// catch (Exception ex)
				// {
				// 	throw new SoftException("Unable to Restart Android Application:\n" + ex);
				// }
			}
			if (_dev.CanSU())
				CleanTombs();
			CleanLogs();
		}

		public override bool IterationFinished()
		{
			_fault = new Fault();
			_fault.type = FaultType.Fault;
			_fault.detectionSource = "AndroidMonitor";
			_fault.exploitability = "Unknown";
			try
			{
				// this can fail
				_dev.ExecuteShellCommand("logcat -s -d AndroidRuntime:e ActivityManager:e *:f", _cmdreciever);

				_fault.type = FaultType.Data;
				_fault.title = "Response";
				_fault.description = _cmdreciever.Result;

				if (!string.IsNullOrEmpty(_fault.description))
				{
					// Filter out lines that look like:
					// --------- beginning of /dev/log/main
					var filtered = logFilter.Replace(_fault.description, "");
					if (!string.IsNullOrEmpty(filtered))
						_fault.type = FaultType.Fault;
				}
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.ToString());
			}
			return true;
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		public override Fault GetMonitorData()
		{
			CommandResultBinaryReceiver breciever = new CommandResultBinaryReceiver();;

			// STEP 1: Get 1st Screenshot
			try
			{
				// this can fail, add exception handling
				//Image img = _dev.Screenshot; // this is the faster way to do it, but we need a way to convert to an image in mono...
				//_dev.Screenshot.ToImage().Save("test.bmp");

				_dev.ExecuteShellCommand("screencap -p", breciever);
				_fault.collectedData.Add(new Fault.Data("screenshot1.png", imageClean(breciever.Result)));
				breciever.Flush();
			}
			catch (Exception ex)
			{
				logger.Warn("AndroidScreenshot: Warn, Unable to capture first screenshot:\n" + ex);
			}

			// STEP 2: Wait for boot if system crashed.
			// Make sure the device is ready
			logger.Debug("Checking for restart");
			bool restarted = false;
			try
			{
				if (restarted = devRestarted())
				{
					Thread.Sleep(2000); //Boot Animation is finished, give it two seconds before unlocking
					CleanUI("unlock");
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex.Message);
				_muststop = true;
			}

			// STEP 3: Grab Tomb
			try
			{
				logger.Debug("Getting tombstones");
				foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
				{
					// also, this can fail, add exception handling
					_dev.ExecuteShellCommand("cat " + tomb.FullPath, _cmdreciever);
					string tombstr = _cmdreciever.Result;

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
					if (!restarted)
					{
						Thread.Sleep(1000);
						CleanUI("dismiss");
					}
				}
				// This should happen on each iteration start, not after we
				// collect data. This would not properly clean up exceptions that may happen elsewhere

				//CleanTombs();
				//CleanLogs();
			}
			catch (Exception ex)
			{
				logger.Warn(ex.Message);
			}
			logger.Debug("Taking screenshots");
			// STEP 4: Get 2nd ScreenShot
			try
			{
				// also, this can fail, add exception handling
				_dev.ExecuteShellCommand("screencap -p", breciever);
				_fault.collectedData.Add(new Fault.Data("screenshot2.png", imageClean(breciever.Result)));
				breciever.Flush();
			}
			catch (Exception ex)
			{
				logger.Warn("AndroidScreenshot: Warn, Unable to capture second screenshot:\n" + ex.Message);
			}

			return _fault;
		}

		public override bool MustStop()
		{
			return _muststop;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
	}
}
