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
	[Parameter("RestartEveryIteration", typeof(bool), "Restart Application on Every Iteration", "true")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart Application after Faults", "true")]
	[Parameter("ClearAppDataOnFault", typeof(bool), "Remove Application data and cache on fault iterations", "false")]
	//	[Parameter("CommandTimout", typeof(int), "Time to wait for completion of commands sent to device, fault on timeout", "10")]
	[Parameter("DeviceRetryCount", typeof(int), "Number of times to try to connect to the device before failing", "50")]

	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static Regex logFilter = new Regex(@"^-+ beginning of (/\w+)+(\r\n|\r|\n)?", RegexOptions.Multiline);
		private Regex reHash = new Regex(@"^backtrace:((\r)?\n    #([^\r\n])*)*", RegexOptions.Multiline);
		private Regex amProcessSuccess = new Regex(@".*\n(Status: ok)\r?\n.*\nComplete(\r)?\n?", RegexOptions.Multiline | RegexOptions.Singleline);
		private Regex amProcessFailure = new Regex(@".*\Error: (.*?)\r?\n?", RegexOptions.Multiline | RegexOptions.Singleline);

		private Regex pmProcessSuccess = new Regex(@"^Success$\r?\n?");
		private Regex pmProcessFailure = new Regex(@"^Failed$\r?\n?");

		private Regex bmgrProcessSuccess = new Regex(@"^Wiped backup data for .*\r?\n?");
		private Regex bmgrProcessFailure = new Regex(@"^Failed$\r?\n?");

		private Fault _fault = null;
		private Device _dev = null;
		private FileEntry _tombs = null;
		private bool _muststop = false;
		private CommandResultReceiver _cmdreciever = null;

		public string ApplicationName { get; private set; }
		public string AdbPath { get; private set; }
		public string ActivityName { get; private set; }
		public string DeviceSerial { get; set; }
		public bool RestartEveryIteration { get; private set; }
		public bool RestartAfterFault { get; private set; }
		public bool ClearAppDataOnFault { get; private set; }
		//		public int CommandTimout {get; private set; }
		public int DeviceRetryCount {get; private set; }


		public AndroidMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
			AndroidBridge.SetAdbPath(AdbPath);
			_cmdreciever = new CommandResultReceiver();
		}


		public string SafeExec(string cmd, bool failHard)
		{
			logger.Debug("Executing on device " + _dev.SerialNumber + " command: " +  cmd);
			try
			{
				//_dev.ExecuteShellCommand(cmd, _cmdreciever, CommandTimout);
				_dev.ExecuteShellCommand(cmd, _cmdreciever); //timout code is broken on the mad bee side
			}
			//!!FIXME!! could also fail with adb missing
			catch (Managed.Adb.Exceptions.DeviceNotFoundException)
			{
				string msg = "Device connection lost";
				if (failHard)
				{
					throw new PeachException(msg);
				}
				else
				{
					// more error handling
					// We can't actually do much more than this right now...
					try
					{
						devRestarted();
					}
					catch (Exception ex)
					{
						throw new SoftException(ex);
					}
				}
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
			return _cmdreciever.Result;
		}

		public byte [] BinSafeExec(string cmd)
		{
			CommandResultBinaryReceiver breciever = new CommandResultBinaryReceiver();;
			// need to merge this with SafeExec, handle different types
			logger.Debug("Executing on device " + _dev.SerialNumber + " command: " +  cmd);
			try
			{
				//_dev.ExecuteShellCommand(cmd, _cmdreciever, CommandTimout);
				_dev.ExecuteShellCommand(cmd, breciever); //timout code is broken on the mad bee side
			}
			//!!FIXME!! could also fail with adb missing
			catch (Managed.Adb.Exceptions.DeviceNotFoundException)
			{
				try
				{
					devRestarted();
				}
				catch (Exception ex)
				{
					throw new SoftException(ex);
				}
			}
			//catch (Managed.Adb.Exceptions.ShellCommandUnresponsiveException ex)
			catch (Managed.Adb.Exceptions.ShellCommandUnresponsiveException)
			{
				logger.Debug("Lost Connection to ADB, restarting");
				AndroidBridge.RestartADB();
				//throw new SoftException(ex);
			}
			return breciever.Result;
		}

		public string SafeExec(string cmd)
		{
			return SafeExec(cmd, false);
		}

		public void SafeExec(string cmd, Regex success, Regex failure, bool failHard)
		{
			var ExecException = (failHard ? typeof(PeachException) : typeof(SoftException));
			string response = SafeExec(cmd, failHard);
			if (!string.IsNullOrEmpty(response))
			{
				if (!success.Match(response).Success)
				{
					Match m = failure.Match(response);
					if (m.Success)
						throw (Exception)Activator.CreateInstance(ExecException, "Command `" + cmd + "` failed to execute: \n" + m);
					else
						throw (Exception)Activator.CreateInstance(ExecException, "Command output for `" + cmd + "` could not be parsed: \n" + response);
				}
			}
			else
				throw (Exception)Activator.CreateInstance(ExecException, "Shell command `" + cmd +"` had no output");
		}

		public void SafeExec(string cmd, Regex success, Regex failure)
		{
			SafeExec(cmd, success, failure, false);
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
			SafeExec("logcat -c");
		}

		private void CleanUI(string type)
		{
			string _cmd = "";
			if (type.Equals("dismiss"))
			{
				logger.Debug("Clearing UI running 2x " + _cmd);
				_cmd = "input keyevent 66"; //Enter
				SafeExec(_cmd);
				Thread.Sleep(250);
				// this can fail, add exception handling
				SafeExec(_cmd);
			}
			else if (type.Equals("unlock"))
			{
				_cmd = "input keyevent 82";
				// this can fail, add exception handling
				SafeExec(_cmd); //Menu
			}
		}

		private void StartApp()
		{
			//check _dev.State; to make sure it's online?
			string cmd = "am start -W -S -n " + ApplicationName;
			if (!string.IsNullOrEmpty(ActivityName))
			{
				cmd = cmd + "/" + ActivityName;
			}
			SafeExec(cmd, amProcessSuccess, amProcessFailure, true);
		}


		private bool devRestarted()
		{
			// what? This doesn't do what it says it does....
			bool restarted = false;
			int i = 0;
			while (i < DeviceRetryCount)
			{
				try
				{
					// this could be called from SafeExec so it can't be used in SafeExec
					// also, there's already error handling around this one
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
			// needs to check if the system is booted
			SafeExec("echo hello android..."); // if this works, the system is booted. I'd like a better check
			bool fault = _fault != null && (_fault.type == FaultType.Fault);
			bool restart = false;
			if (RestartEveryIteration || ( fault && RestartAfterFault))
			{
				restart = true;
			}

			if (fault && ClearAppDataOnFault)
			{
				SafeExec("pm clear " + ApplicationName, pmProcessSuccess, pmProcessFailure);
				SafeExec("bmgr wipe " + ApplicationName, bmgrProcessSuccess, bmgrProcessFailure);
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
				_fault.type = FaultType.Data;
				_fault.title = "Response";
				_fault.description = SafeExec("logcat -s -d AndroidRuntime:e ActivityManager:e *:f");

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


			// STEP 1: Get 1st Screenshot
			try
			{
				//Image img = _dev.Screenshot; // this is the faster way to do it, but we need a way to convert to an image in mono...
				//_dev.Screenshot.ToImage().Save("test.bmp");

				_fault.collectedData.Add(new Fault.Data("screenshot1.png", imageClean(BinSafeExec("screencap -p"))));
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
				if (_dev.CanSU())
				{
					logger.Debug("Getting tombstones");
					foreach (var tomb in _dev.FileListingService.GetChildren(_tombs, false, null))
					{
						// also, this can fail, add exception handling
						string tombstr = SafeExec("cat " + tomb.FullPath);

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
				_fault.collectedData.Add(new Fault.Data("screenshot2.png", imageClean(BinSafeExec("screencap - p"))));
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
