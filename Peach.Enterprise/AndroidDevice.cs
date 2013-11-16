using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using Peach.Core;
using Managed.Adb;
using Managed.Adb.Exceptions;

namespace Peach.Enterprise
{
	// shell dumpsys display
	//   mAllDisplayBlankStateFromPowerManager = 1 => Blanked
	//   mAllDisplayBlankStateFromPowerManager = 2 => Unblanked

	// shell dumpsys window
	//   mCurrentFocus=Window{40f0fb38 u0 Keyguard} => Locked
	//   mCurrentFocus=Window{40ea0750 u0 Application Error: com.android.development} Crash menu
	//     KEYCODE_ENTER twice!


	// shell input keyevent 26 KEYCODE_POWER
	// shell input keyevent 86 KEYCODE_MENU
	// shell input keyevent 86 KEYCODE_ENTER

	public class AndroidDevice : IDisposable
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		private bool disposed;
		private Device handle;
		private long readyTimeout;
		private long commandTimeout;

		private Device dev
		{
			get
			{
				lock (AndroidDebugBridge.GetLock())
				{
					return handle;
				}
			}
		}

		private AndroidDevice(Device dev, int readyTimeout, int commandTimeout)
		{
			// MUST be holding AndroidDebugBridge.GetLock() when this function is called!
			this.disposed = false;
			this.handle = dev;
			this.readyTimeout = readyTimeout * 1000;
			this.commandTimeout = commandTimeout * 1000;

			AndroidDebugBridge.Instance.DeviceConnected += OnDeviceConnected;

		}

		private void OnDeviceConnected(object sender, DeviceEventArgs e)
		{
			// ManagedAdb is holding AndroidDebugBridge.GetLock() when this function is called
			if (handle.SerialNumber == e.Device.SerialNumber)
			{
				logger.Debug("Device '{0}' reconnected, updating handle on object 0x{1:x}", e.Device.SerialNumber, GetHashCode());
				handle = e.Device as Device;
			}
		}

		public void Dispose()
		{
			if (!disposed)
			{
				lock (AndroidDebugBridge.GetLock())
				{
					AndroidDebugBridge.Instance.DeviceConnected -= OnDeviceConnected;
				}

				disposed = true;
			}
		}

		public static AndroidDevice Get(string serialNumber, int connectTimeout, int readyTimeout, int commandTimeout)
		{
			var remain = TimeSpan.FromSeconds(connectTimeout);
			connectTimeout *= 1000;

			while (remain > TimeSpan.Zero)
			{
				lock (AndroidDebugBridge.GetLock())
				{
					var dev = AndroidDebugBridge.Instance.Devices.Where(d => serialNumber == null || d.SerialNumber == serialNumber).FirstOrDefault();
					if (dev != null)
						return new AndroidDevice(dev, readyTimeout, commandTimeout);
				}

				remain -= TimeSpan.FromMilliseconds(100);
				Thread.Sleep(100);
			}

			if (serialNumber == null)
				throw new PeachException("No available android devices were found.");
			else
				throw new PeachException("Device '" + serialNumber + "' was not found. Use `adb devices` to verify it's plugged in.");
		}

		public string SerialNumber
		{
			get
			{
				return dev.SerialNumber;
			}
		}

		/// <summary>
		/// Waits until the device is booted, and ensures the screen is turned on
		/// and the lock screen is bypassed.
		/// </summary>
		/// <returns>Throws a PeachException if an error occurs</returns>
		public void WaitForReady()
		{
			logger.Debug("Waiting for device '{0}' to become ready", dev.SerialNumber);

			var sw = new Stopwatch();
			sw.Start();

			var remain = readyTimeout;
			while (remain > 0)
			{
				if (dev.IsOnline)
				{
					try
					{
						var res = RunShellCommand(NLog.LogLevel.Trace, "getprop init.svc.bootanim");
						if (res == "stopped")
							break;
					}
					catch (Exception ex)
					{
						logger.Trace("Exception waiting for device '{0}': {1}", dev.SerialNumber, ex.Message);
					}
				}

				remain = Math.Max(readyTimeout - sw.ElapsedMilliseconds, 0);

				long sleepTime = Math.Min(remain, 1000);
				Thread.Sleep((int)sleepTime);
			}

			sw.Stop();

			if (remain == 0)
				throw new PeachException("Error, timed out waiting for android device '" + dev.SerialNumber + "' to become ready.");

			try
			{
				// Check if we need to turn on the display
				var screenState = QueryShellCommand(NLog.LogLevel.Trace, "dumpsys display", rePower);
				if (screenState == null)
					throw new PeachException("Failed to query the current screen state.");

				if (screenState != "2")
					RunShellCommand(NLog.LogLevel.Trace, "input keyevent KEYCODE_POWER");
			}
			catch (Exception ex)
			{
				throw new PeachException("Unable to turn the display on. " + ex.Message, ex);
			}

			try
			{
				// Check if we need to unlock
				var currFocus = QueryShellCommand(NLog.LogLevel.Trace, "dumpsys window", reFocus);
				if (currFocus == null)
					throw new PeachException("Failed to query the current foreground window.");

				if (currFocus.Contains("Keyguard"))
					RunShellCommand(NLog.LogLevel.Trace, "input keyevent KEYCODE_MENU");
			}
			catch (Exception ex)
			{
				throw new PeachException("Unable to unlock the device. " + ex.Message, ex);
			}

			logger.Debug("Device '{0}' is now ready", dev.SerialNumber);
		}

		/// <summary>
		/// Reads the error log and returns it as a string.
		/// If an error occurs reading the log, the error message is returned instead.
		/// </summary>
		/// <returns>An empty string if no error occurs, otherwise a string containing the errors.</returns>
		public string CheckForErrors()
		{
			try
			{
				// Check for errors in the log, throws on error
				var log = RunShellCommand(NLog.LogLevel.Debug, "logcat -s -d AndroidRuntime:e ActivityManager:e *:f");

				// Filter out log lines added by logcat
				var filtered = reLogFilter.Replace(log, "");

				// If filtered is not empty, return the non-filtered log
				return string.IsNullOrEmpty(filtered) ? filtered : log;
			}
			catch (Exception ex)
			{
				// Error running logcat most likely means the device rebooted
				// We still call MakeFault, which will wait for the device to be ready
				// and grab logs and tombs
				return ex.Message;
			}
		}

		public void Input(string how, params string[] args)
		{
			// TODO: Escape "text" argument.
			// "input text foo bar" => "input text foo%sbar"
			// "input text foo%sbar" => "input text foo%" & "input text sbar"

			var cmd = "input " + how + " " + string.Join(" ", args);
			RunShellCommand(NLog.LogLevel.Debug, cmd);
		}

		public string ReadAllLogs()
		{
			return RunShellCommand(NLog.LogLevel.Debug, "logcat -d -v long");
		}

		private string RunShellCommand(NLog.LogLevel level, string cmd)
		{
			logger.Log(level, "Executing command on '{0}': {1}", dev.SerialNumber, cmd);

			lock (AndroidDebugBridge.GetLock())
			{
				if (dev.IsOffline)
					throw new DeviceNotFoundException(dev.SerialNumber);

				var rx = new CommandResultReceiver();
				AdbHelper.Instance.ExecuteRemoteCommand(AndroidDebugBridge.SocketAddress, cmd, dev, rx, int.MaxValue);
				return rx.Result ?? "";
			}
		}

		private string QueryShellCommand(NLog.LogLevel level, string cmd, Regex regex)
		{
			var s = RunShellCommand(level, cmd);
			var m = regex.Match(s);
			return m.Success ? m.Groups[1].Value : null;
		}

		public IEnumerable<FileEntry> CrashDumps()
		{
			try
			{
				var tombs = FileEntry.FindOrCreate(dev, tombPath);
				return dev.FileListingService.GetChildren(tombs, false, null);
			}
			catch (PermissionDeniedException)
			{
				// When permission denied, just return empty sequence
				return new FileEntry[0];
			}
		}

		public byte[] TakeScreenshot()
		{
			var ms = new MemoryStream();
			var image = dev.Screenshot.ToImage();
			image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			return ms.ToArray();
		}

		public void StartApp(string application, string activity)
		{
			string tgt = application;
			if (!string.IsNullOrEmpty(activity))
				tgt += "/" + activity;
			string cmd = "am start -W -S -n " + tgt;

			var ret = RunShellCommand(NLog.LogLevel.Debug, cmd);

			if (!amProcessSuccess.Match(ret).Success)
				throw new PeachException("Activity manager failed to start '{0}'. {1}".Fmt(tgt, ret));
		}

		public void ClearAppData(string application)
		{
			string ret;

			ret = RunShellCommand(NLog.LogLevel.Debug, "pm clear " + application);
			if (!pmProcessSuccess.Match(ret).Success)
				throw new PeachException("Package manager failed to delete data for '{0}'. {1}".Fmt(application, ret));

			ret = RunShellCommand(NLog.LogLevel.Debug, "bmgr wipe " + application);
			if (!bmgrProcessSuccess.Match(ret).Success)
				throw new PeachException("Backup manager failed to delete data for '{0}'. {1}".Fmt(application, ret));
		}

		public void ClearLogs()
		{
			foreach (var tomb in CrashDumps())
			{
				dev.FileSystem.Delete(tomb);
			}

			RunShellCommand(NLog.LogLevel.Debug, "logcat -c");
		}

		public void PullFile(FileEntry remote, string local)
		{
			dev.SyncService.PullFile(remote, local, SyncService.NullProgressMonitor);
		}

		static Regex reLogFilter = new Regex(@"^-+ beginning of [^\r\n]*(\r\n|\r|\n)?", RegexOptions.Multiline);
		static Regex rePower = new Regex("mAllDisplayBlankStateFromPowerManager=(\\d)\\r?$", RegexOptions.Multiline);
		static Regex reFocus = new Regex("mCurrentFocus=(.*?)\\r?$", RegexOptions.Multiline);
		static Regex amProcessSuccess = new Regex(@".*\n(Status: ok)\r?\n.*\nComplete(\r)?\n?", RegexOptions.Multiline | RegexOptions.Singleline);
		static Regex pmProcessSuccess = new Regex(@"^Success$\r?\n?");
		static Regex bmgrProcessSuccess = new Regex(@"^Wiped backup data for .*\r?\n?");

		static string tombPath = "/data/tombstones/";
	}
}
