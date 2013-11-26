using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Peach.Core.Agent;
using Peach.Enterprise;
using Peach.Core;
using System.Text.RegularExpressions;
using NLog;
using System.IO;

namespace Peach.Enterprise.Agent.Monitors
{
	[Monitor("Android", true)]
	[Parameter("ApplicationName", typeof(string), "Android Application")]
	[Parameter("ActivityName", typeof(string), "Application Activity", "")]
	[Parameter("AdbPath", typeof(string), "Directory Path to Adb", "")]
	[Parameter("DeviceSerial", typeof(string), "The serial of the device to monitor", "")]
	[Parameter("DeviceMonitor", typeof(string), "Android monitor to get device serial from", "")]
	[Parameter("RestartEveryIteration", typeof(bool), "Restart Application on Every Iteration", "false")]
	[Parameter("ClearAppData", typeof(bool), "Remove Application data and cache on every iteration", "false")]
	[Parameter("ClearAppDataOnFault", typeof(bool), "Remove Application data and cache on fault iterations", "false")]
	[Parameter("StartOnCall", typeof(string), "Start the application when notified by the state machine", "")]
	[Parameter("ConnectTimeout", typeof(int), "Max seconds to wait for adb connection (default 5)", "5")]
	[Parameter("ReadyTimeout", typeof(int), "Max seconds to wait for device to be ready (default 600)", "600")]
	[Parameter("CommandTimeout", typeof(int), "Max seconds to wait for adb command to complete (default 10)", "10")]
	[Parameter("FaultWaitTime", typeof(int), "Milliseconds to wait when checking for a fault (default 0)", "0")]
	public class AndroidMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static Regex reHash = new Regex(@"\[\s+\d+-\d+\s+[^EF]+\s+([EF])/([^\]\s]+)");

		bool firstRun = false;
		bool adbInit = false;
		Fault fault = null;
		AndroidDevice dev = null;

		public string ApplicationName { get; protected set; }
		public string AdbPath { get; protected set; }
		public string ActivityName { get; protected set; }
		public string DeviceSerial { get; protected set; }
		public string DeviceMonitor { get; protected set; }
		public string StartOnCall { get; protected set; }
		public bool RestartEveryIteration { get; protected set; }
		public bool ClearAppDataOnFault { get; protected set; }
		public bool ClearAppData { get; protected set; }
		public int ConnectTimeout { get; protected set; }
		public int ReadyTimeout { get; protected set; }
		public int CommandTimeout { get; protected set; }
		public int FaultWaitTime { get; protected set; }

		public AndroidMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			if (!string.IsNullOrEmpty(DeviceSerial) && !string.IsNullOrEmpty(DeviceMonitor))
				throw new PeachException("Can't specify both DeviceSerial parameter and DeviceMonitor parameter.");
		}

		Fault MakeFault(string errorLog)
		{
			var sb = new System.Text.StringBuilder();

			Action<string, Action> guard = (what, action) =>
			{
				try
				{
					logger.Trace("Making fault '{0}': {1}", dev.SerialNumber, what);
					action();
				}
				catch (Exception ex)
				{
					var msg = "Unable to {0}:{1}{2}".Fmt(what, Environment.NewLine, ex);
					logger.Debug(msg);
					sb.AppendLine(msg);
				}
			};

			var ret = new Fault()
			{
				title = "Device '{0}' Log".Fmt(DeviceSerial ?? DeviceMonitor),
				description = errorLog ?? "",
				detectionSource = "AndroidMonitor",
				folderName = "AndroidMonitor",
				type = string.IsNullOrEmpty(errorLog) ? FaultType.Data : FaultType.Fault,
			};

			// Step 1: Wait for device to be ready
			guard("wait for device ready", () =>
			{
				dev.WaitForReady();
			});

			// Step 2: Try and get a creenshot
			guard("capture screenshot", () =>
			{
				var bytes = dev.TakeScreenshot();
				ret.collectedData.Add(new Fault.Data("{0}_screenshot.png".Fmt(DeviceMonitor ?? dev.SerialNumber), bytes));
			});

			// Step 3: Grab full logcat
			guard("capture device logs", () =>
			{
				var log = dev.ReadAllLogs();
				var bytes = Encoding.UTF8.GetBytes(log);
				ret.collectedData.Add(new Fault.Data("{0}_logcat".Fmt(DeviceMonitor ?? dev.SerialNumber), bytes));
			});

			// Step 4: Grab tombs
			guard("capture tombs", () =>
			{
				var tmp = Path.GetTempFileName();

				try
				{
					foreach (var tomb in dev.CrashDumps())
					{
						dev.PullFile(tomb, tmp);
						ret.collectedData.Add(new Fault.Data("{0}_{1}".Fmt(DeviceMonitor ?? dev.SerialNumber, tomb.Name), System.IO.File.ReadAllBytes(tmp)));
					}
				}
				finally
				{
					File.Delete(tmp);
				}
			});

			// Step 5: Clear device logs on fault
			if (ret.type == FaultType.Fault)
			{
				guard("clear device logs", () =>
				{
					dev.ClearLogs();
				});
			}

			// Step 6: Save any exceptions that might have occured
			if (sb.Length > 0)
			{
				var errors = sb.ToString();
				var bytes = Encoding.UTF8.GetBytes(errors);
				ret.collectedData.Add(new Fault.Data("{0}_exceptions".Fmt(DeviceMonitor ?? dev.SerialNumber), bytes));
			}

			// Step 7: Update bucketing information
			if (errorLog != null)
			{
				var match = reHash.Match(errorLog);

				if (match.Success)
				{
					if (match.Groups[1].Value == "E")
						ret.exploitability = "Error";
					else if (match.Groups[1].Value == "F")
						ret.exploitability = "Fatal";

					ret.majorHash = match.Groups[2].Value;
					ret.minorHash = "";
					ret.folderName = string.Format("{0}_{1}", ret.exploitability, ret.majorHash);
				}
			}

			return ret;
		}

		void SyncDevice()
		{
			// If the serial came from a monitor, it might change across iterations
			// so we need to make sure out dev member is always correct

			var serial = DeviceSerial;

			if (!string.IsNullOrEmpty(DeviceMonitor))
			{
				serial = Agent.QueryMonitors(DeviceMonitor + ".DeviceSerial") as string;
				if (serial == null)
					throw new PeachException("Could not resolve device serial from monitor '" + DeviceMonitor + "'.");
			}

			if (dev != null && (serial == null || dev.SerialNumber == serial))
				return;

			if (dev == null && DeviceMonitor != null)
				logger.Debug("Resolved device '{0}' from monitor '{1}'.", serial, DeviceMonitor);

			if (dev != null)
			{
				logger.Debug("Updating device from old serial '{0}' to new serial '{1}'.", dev.SerialNumber, serial);

				dev.Dispose();
				dev = null;
			}

			dev = AndroidDevice.Get(serial, ConnectTimeout, ReadyTimeout, CommandTimeout);
		}

		public override object ProcessQueryMonitors(string query)
		{
			if (query == Name + ".DeviceSerial" && dev != null)
				return dev.SerialNumber;

			return null;
		}

		public override void SessionStarting()
		{
			adbInit = true;
			firstRun = true;

			// Initialize adb stuff
			AndroidBridge.Initialize(AdbPath);

			// Start the application if we are not running it every iteration
			if (StartOnCall == null && !RestartEveryIteration)
				LaunchApp(false);
		}

		public override void SessionFinished()
		{
			if (dev != null)
			{
				dev.Dispose();
				dev = null;
			}

			if (adbInit)
			{
				adbInit = false;
				AndroidBridge.Terminate();
			}
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			bool hasFault = fault != null;

			fault = null;

			if (StartOnCall != null)
				return;

			// If we just faulted, or are supposed to start the app every iteration
			// make sure the device is ready before continuing
			if (hasFault || RestartEveryIteration)
				LaunchApp(hasFault);
		}

		private void LaunchApp(bool hasFault)
		{
			// The device can change across iterations, especially if we are using multiple emulators
			SyncDevice();

			dev.WaitForReady();

			if (ClearAppData || (hasFault && ClearAppDataOnFault))
				dev.ClearAppData(ApplicationName);
				
			// If this is the first time, clear the device logs
			if (firstRun)
			{
				dev.ClearLogs();
				firstRun = false;
			}

			dev.StartApp(ApplicationName, ActivityName);
		}

		public override bool IterationFinished()
		{
			return true;
		}

		public override bool DetectedFault()
		{
			System.Diagnostics.Debug.Assert(fault == null);

			if (FaultWaitTime > 0)
				Thread.Sleep(FaultWaitTime);

			string errors = dev.CheckForErrors();

			if (string.IsNullOrEmpty(errors))
				return false;

			logger.Debug("Detected errors on device '{0}', building fault record", dev.SerialNumber);

			// Error log contains messages, capture device state
			// Note: MakeFault does not throw an exception
			fault = MakeFault(errors);

			System.Diagnostics.Debug.Assert(fault != null);
			System.Diagnostics.Debug.Assert(fault.type == FaultType.Fault);

			logger.Debug("Fault detected!");

			return true;
		}

		public override Fault GetMonitorData()
		{
			// If fault is null, make a data fault that contains full logcat and screenshots
			if (fault == null)
				fault = MakeFault(null);

			return fault;
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			if (this.Name == name && (string)data == "DeviceSerial")
				return new Variant(dev.SerialNumber);

			if (name == "Action.Call" && ((string)data) == StartOnCall)
			{
				LaunchApp(false);
			}

			return null;
		}
	}
}
