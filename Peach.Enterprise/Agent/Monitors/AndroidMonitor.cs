using System;
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
	[Parameter("AdbPath", typeof(string), "Directory Path to Adb", "")]
	[Parameter("ActivityName", typeof(string), "Application Activity", "")]
	[Parameter("RestartEveryIteration", typeof(bool), "Restart Application on Every Iteration (defaults to true)", "true")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart Application after Faults (defaults to true)", "true")]
	
	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static Regex logFilter = new Regex(@"^-+ beginning of (/\w+)+(\r\n|\r|\n)?", RegexOptions.Multiline);

		private Fault _fault = null;
		private Device _dev = null;
		private FileEntry _tombs = null;
		private int _retries = 200;
		private bool _muststop = false;
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

			string p = Environment.GetEnvironmentVariable("PATH");
			p += Path.PathSeparator;
			p += "C:\\Users\\seth\\Downloads\\adt-bundle-windows-x86_64-20130717\\sdk\\platform-tools";
			Environment.SetEnvironmentVariable("PATH", p);

			AdbPath = FindAdb(AdbPath);
		}

		static string FindAdb(string path)
		{
			var adb = Platform.GetOS() == Platform.OS.Windows ? "adb.exe" : "adb";

			if (path != null)
			{
				var fullPath = Path.Combine(path, adb);

				if (!File.Exists(fullPath))
					throw new PeachException("Error, uunable to locate " + adb + "in provided AdbPath.");

				return fullPath;
			}

			path = Environment.GetEnvironmentVariable("PATH");
			var dirs = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var dir in dirs)
			{
				var fullPath = Path.Combine(dir, adb);

				if (File.Exists(fullPath))
					return fullPath;
			}

			throw new PeachException("Error, unable to locate " + adb + ", please specify using 'AdbPath' parameter.");
		}

		//TODO Duplicate method in Publisher
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


		private bool devRestarted()
		{
			bool restarted = false;
			int i = 0;
			while (i < _retries)
			{
				CommandResultReceiver creciever = new CommandResultReceiver();
				try
				{
					_dev.ExecuteShellCommand("getprop init.svc.bootanim", creciever);
				}
				catch
				{
					try
					{
						restartAdb();
						grabDevice();
					}
					catch
					{
					}
					continue;
				}

				if (!creciever.Result.Equals("running"))
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

		private void controlAdb(string command)
		{
			Process p = new Process();
			p.StartInfo.FileName = AdbPath;
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

				// Filter out lines that look like:
				// --------- beginning of /dev/log/main
				var filtered = logFilter.Replace(_fault.description, "");

				if (!string.IsNullOrEmpty(filtered))
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
					if (!restarted)
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
				logger.Warn(ex.Message);
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
			return _muststop;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
	}
}
