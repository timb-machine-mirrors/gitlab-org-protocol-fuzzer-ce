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
using Managed.Adb.Logs;

namespace Peach.Enterprise
{
	// shell dumpsys power android 4.1
	//   mPowerState=!0 => Powered on
	//   mPowerState=0 => Powered off
	// shell dumpsys power android 4.3
	//   mScreenOn=true => Powered on
	//   mScreenOn=false => Powered off

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
		private int commandTimeout;
		private Thread thread;
		private List<LogEntry> logs;

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
			this.logs = new List<LogEntry>();

			AndroidBridge.DeviceConnected += OnDeviceConnected;

			thread = new Thread(GetLogs);
			thread.Start();
		}

		public class LogEntry
		{
			public enum LogLevel : byte
			{
				U = 0, // Unknown
				T = 1, // Default/Trace
				V = 2, // Verbose
				D = 3, // Debug
				I = 4, // Info
				W = 5, // Warn
				E = 6, // Error
				F = 7, // Assert/Fatal
				S = 8, // Silent
			}

			public LogLevel Level { get; set; }
			public int ProcessId { get; set; }
			public int ThreadId { get; set; }
			public DateTime TimeStamp { get; set; }
			public int NanoSeconds { get; set; }
			public string Name { get; set; }
			public string Message { get; set; }

			public override string ToString()
			{
				return string.Format("{0}/{1}({2}): {3}", Level, Name, ProcessId, Message);
			}

			public string ToStringLong()
			{
				return string.Format("[ {0}.{1} {2}: {3} {4}/{5} ]{6}{7}",
					TimeStamp.ToUniversalTime().ToString("M-dd HH:mm:ss"),
					NanoSeconds / 1000000,
					ProcessId,
					ThreadId,
					Level,
					Name,
					Environment.NewLine,
					Message);
			}
		}

		class LogListener : ILogListener
		{
			AndroidDevice dev;
			string prefix;

			public LogListener(AndroidDevice dev, string prefix)
			{
				this.dev = dev;
				this.prefix = prefix;
			}

			public void NewEntry(Managed.Adb.Logs.LogEntry entry)
			{
				if (entry.Data.Length == 0)
					return;

				var level = (LogEntry.LogLevel)entry.Data[0];
				var line = entry.Data.GetString(1, entry.Data.Length - 1, AdbHelper.DEFAULT_ENCODING);
				var parts = line.Split('\0');

				if (parts.Length != 3 || parts[2] != "")
					parts = new[] { "", line };

				var msg = new LogEntry()
				{
					 Level = level,
					 ProcessId = entry.ProcessId,
					 ThreadId = entry.ThreadId,
					 TimeStamp = entry.TimeStamp,
					 NanoSeconds = entry.NanoSeconds,
					 Name = parts[0],
					 Message = parts[1],
				};

				if (logger.IsTraceEnabled)
					logger.Trace("{0}: {1}", prefix, msg.ToString());

				lock (dev)
				{
					dev.logs.Add(msg);
				}
			}

			public void  NewData(byte[] data, int offset, int length)
			{
			}
		}

		private void GetLogs()
		{
			while (true)
			{
				Device safeDev = dev;

				try
				{
					safeDev.RunLogService("main", new LogReceiver(new LogListener(this, safeDev.SerialNumber)));
				}
				catch
				{
				}

				Thread.Sleep(1000);
			}
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
				if (thread != null)
				{
					thread.Abort();
					thread.Join();
					thread = null;
				}

				lock (AndroidDebugBridge.GetLock())
				{
					AndroidBridge.DeviceConnected -= OnDeviceConnected;
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
				throw new SoftException("No available android devices were found.");
			else
				throw new SoftException("Device '" + serialNumber + "' was not found. Use `adb devices` to verify it's plugged in.");
		}

		public string SerialNumber
		{
			get
			{
				return dev.SerialNumber;
			}
		}

		public void Reboot()
		{
			logger.Debug("Attempting to reboot device '{0}'", dev.SerialNumber);

			var sw = new Stopwatch();
			sw.Start();

			var reboot = true;
			var remain = readyTimeout;

			while (remain > 0)
			{
				try
				{
					if (reboot)
					{
						dev.Reboot();
						reboot = false;
					}
					else if (dev.IsOnline)
					{
						// Wait for the boot animation to start up again
						var res = RunShellCommand(NLog.LogLevel.Trace, "getprop init.svc.bootanim");
						if (res != "stopped")
							break;
					}
				}
				catch (Exception ex)
				{
					logger.Trace("Exception rebooting device '{0}': {1}", dev.SerialNumber, ex.Message);
				}

				remain = Math.Max(readyTimeout - sw.ElapsedMilliseconds, 0);

				long sleepTime = Math.Min(remain, 1000);
				Thread.Sleep((int)sleepTime);
			}

			if (remain == 0)
				throw new SoftException("Error, timed out trying to reboot android device '" + dev.SerialNumber + "'.");
		}

		/// <summary>
		/// Waits until the device is booted, and ensures the screen is turned on
		/// and the lock screen is bypassed.
		/// </summary>
		/// <returns>Throws a SoftException if an error occurs</returns>
		public void WaitForReady()
		{
			logger.Debug("Waiting for device '{0}' to become ready", dev.SerialNumber);

			var sw = new Stopwatch();
			sw.Start();

			var remain = readyTimeout;
			var restart = false;

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
					catch (AdbException ex)
					{
						logger.Trace("AdbException waiting for device '{0}': {1}", dev.SerialNumber, ex.Message);

						if (ex.Message.Contains("sad result from adb") || ex is ShellCommandUnresponsiveException)
						{
							logger.Trace("Attempting to restart adb server.");
							AndroidBridge.Restart();
							restart = true;
						}
					}
					catch (Exception ex)
					{
						logger.Trace("Exception waiting for device '{0}': {1}", dev.SerialNumber, ex.Message);
					}
				}
				else
				{
					logger.Trace("Device '{0}' is offline, can't run adb commands.", dev.SerialNumber);
				}

				remain = Math.Max(readyTimeout - sw.ElapsedMilliseconds, 0);

				// If the device is not online for half the wait time, bounce adb server
				if (!restart && !dev.IsOnline && remain < (readyTimeout / 2))
				{
					restart = true;
					logger.Debug("Device '{0}' not online after {1}ms, restarting adb server.", dev.SerialNumber, sw.ElapsedMilliseconds);
					AndroidBridge.Restart();
				}

				long sleepTime = Math.Min(remain, 1000);
				Thread.Sleep((int)sleepTime);
			}

			sw.Stop();

			if (remain == 0)
				throw new SoftException("Error, timed out waiting for android device '" + dev.SerialNumber + "' to become ready.");

			try
			{
				// Check if we need to turn on the display
				var screenState = QueryShellCommand(NLog.LogLevel.Trace, "dumpsys power", rePower);
				if (screenState == null)
					throw new SoftException("Failed to query the current screen state.");

				if (screenState == "0" || screenState == "false")
					RunShellCommand(NLog.LogLevel.Trace, "input keyevent KEYCODE_POWER");
			}
			catch (Exception ex)
			{
				throw new SoftException("Unable to turn the display on. " + ex.Message, ex);
			}

			try
			{
				// Check if we need to unlock
				var currFocus = QueryShellCommand(NLog.LogLevel.Trace, "dumpsys window", reFocus);
				if (currFocus == null)
					throw new SoftException("Failed to query the current foreground window.");

				if (currFocus.Contains("Keyguard"))
					RunShellCommand(NLog.LogLevel.Trace, "input keyevent KEYCODE_MENU");
			}
			catch (Exception ex)
			{
				throw new SoftException("Unable to unlock the device. " + ex.Message, ex);
			}

			logger.Debug("Device '{0}' is now ready", dev.SerialNumber);
		}

		public List<LogEntry> GetAndClearLogs()
		{
			lock (this)
			{
				var ret = logs;
				logs = new List<LogEntry>();
				return ret;
			}
		}

		private static string replaceSlash(Match m)
		{
			string s = m.ToString();

			switch (s)
			{
				case "&": return "\\&";
				case ";": return "\\;";
				case "'": return "\\'";
				case "\"": return "\\\"";
				case "~": return "\\~";
				case "`": return "\\`";
				case "(": return "\\(";
				case ")": return "\\)";
				case "\\": return "\\\\";
				case "<": return "\\<";
				case ">": return "\\>";
				case "|": return "\\|";
				default: return s;
			}
		}

		private string EscapeCommandLineChars(string cmd)
		{
			Regex _escapeSlash = new Regex("\\\\|&|;|'|~|`|\\(|\\)|<|>|\\\"|\\|");
			cmd = _escapeSlash.Replace(cmd, new MatchEvaluator(replaceSlash));
			return cmd;
		}

		public void Input(string how, params string[] args)
		{
			if (how.Equals("text"))
			{
				var input = EscapeCommandLineChars(string.Join(" ", args));

				var tokens = input.Split(new string[] { "%s" }, StringSplitOptions.None);
				for (int i = 0; i < tokens.Length; i++)
				{
					RunShellCommand(NLog.LogLevel.Debug, "input text " + tokens[i].Replace(" ", "%s"));
					if (i + 1 != tokens.Length)
					{
						RunShellCommand(NLog.LogLevel.Debug, "input text " + "%");
						RunShellCommand(NLog.LogLevel.Debug, "input text " + "s");
					}
				}
				
			}
			else
			{
				var cmd = "input " + how + " " + string.Join(" ", args);
				RunShellCommand(NLog.LogLevel.Debug, cmd);
			}
		}

		private string RunShellCommand(NLog.LogLevel level, string cmd)
		{
			logger.Log(level, "Executing command on '{0}': {1}", dev.SerialNumber, cmd);

			lock (AndroidDebugBridge.GetLock())
			{
				if (dev.IsOffline)
					throw new DeviceNotFoundException(dev.SerialNumber);

				var rx = new CommandResultReceiver();
				AdbHelper.Instance.ExecuteRemoteCommand(AndroidDebugBridge.SocketAddress, cmd, dev, rx, commandTimeout);
				var ret = rx.Result ?? "";
				if (logger.IsTraceEnabled)
					logger.Trace("Command result: {0}", ret);
				return ret;
			}
		}

		private string QueryShellCommand(NLog.LogLevel level, string cmd, Regex regex)
		{
			var s = RunShellCommand(level, cmd);
			var m = regex.Match(s);
			return m.Success ? m.Groups["Query"].Value : null;
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

		static byte[] PixelConvTable(int len)
		{
			var ret = new byte[len];

			for (int i = 0; i < len; ++i)
				ret[i] = (byte)(Math.Floor(i * 255.0 / (len - 1) + 0.5));

			return ret;
		}

		static byte[] pixels_5 = PixelConvTable(32);
		static byte[] pixels_6 = PixelConvTable(64);

		public byte[] TakeScreenshot()
		{
			logger.Debug("Taking screenshot of device '{0}'.", dev.SerialNumber);

			var ms = new MemoryStream();
			var rawImage = dev.Screenshot;

			if (rawImage.Bpp == 16)
			{
				// Mono doesn't support 16bit RGB565 bitmaps, so convert to 24bit RGB
				var src = rawImage.Data;
				int cnt = src.Length / 2;
				var dst = new byte[3 * cnt];

				for (int i = 0; i < cnt; ++i)
				{
					int pixel = src[2 * i] | (src[2 * i + 1] << 8);

					dst[3 * i + 2] = pixels_5[(pixel >> 11) & 0x1f];
					dst[3 * i + 1] = pixels_6[(pixel >> 5) & 0x3f];
					dst[3 * i + 0] = pixels_5[(pixel >> 0) & 0x1f];
				}

				rawImage.Bpp = 24;
				rawImage.Size = dst.Length;
				rawImage.Data = dst;

				var img = rawImage.ToImage(System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			}
			else
			{
				rawImage.ToImage().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			}

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
				throw new SoftException("Activity manager failed to start '{0}'. {1}".Fmt(tgt, ret));
		}

		public void ClearAppData(string application)
		{
			string ret;

			ret = RunShellCommand(NLog.LogLevel.Debug, "pm clear " + application);
			if (!pmProcessSuccess.Match(ret).Success)
				throw new SoftException("Package manager failed to delete data for '{0}'. {1}".Fmt(application, ret));

			ret = RunShellCommand(NLog.LogLevel.Debug, "bmgr wipe " + application);
			if (!bmgrProcessSuccess.Match(ret).Success)
				ret = RunShellCommand(NLog.LogLevel.Debug, "bmgr wipe transport " + application);
				if (!bmgrProcessSuccess.Match(ret).Success)
					throw new SoftException("Backup manager failed to delete data for '{0}'. {1}".Fmt(application, ret));
		}

		public void DeleteFile(FileEntry entry)
		{
			dev.FileSystem.Delete(entry);
		}

		public void PullFile(FileEntry remote, string local)
		{
			dev.SyncService.PullFile(remote, local, SyncService.NullProgressMonitor);
		}

		static Regex rePower = new Regex("(?:mPowerState=(?<Query>\\d+))|(?:mScreenOn=(?<Query>\\w+))", RegexOptions.Multiline);
		static Regex reFocus = new Regex("mCurrentFocus=(?<Query>.*?)\\r?$", RegexOptions.Multiline);
		static Regex amProcessSuccess = new Regex(@".*\n(Status: ok)\r?\n.*\nComplete(\r)?\n?", RegexOptions.Multiline | RegexOptions.Singleline);
		static Regex pmProcessSuccess = new Regex(@"^Success$\r?\n?");
		static Regex bmgrProcessSuccess = new Regex(@"^Wiped backup data for .*\r?\n?");

		static string tombPath = "/data/tombstones/";
	}
}
