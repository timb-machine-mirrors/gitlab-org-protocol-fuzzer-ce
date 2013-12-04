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
	static public class AndroidBridge
	{
		#region Private Members

		static string adb = Platform.GetOS() == Platform.OS.Windows ? "adb.exe" : "adb";

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		static LogHook adbLogger = new LogHook();

		static int refCount = 0;

		#endregion

		#region ManagedAdb Log Hook

		class LogHook : ILogOutput
		{
			StringBuilder sb = new StringBuilder();

			public string GetLastError()
			{
				lock (sb)
				{
					var ret = sb.ToString();
					sb.Clear();
					return ret;
				}
			}

			public void Write(LogLevel.LogLevelInfo logLevel, string tag, string message)
			{
				if (logLevel.Priority >= LogLevel.Error.Priority)
				{
					lock (sb)
					{
						sb.AppendLine(message);
					}
				}

				logger.Trace(message);
			}

			public void WriteAndPromptLog(LogLevel.LogLevelInfo logLevel, string tag, string message)
			{
				logger.Trace(message);
			}

			public void OnDeviceDisconnected(object sender, DeviceEventArgs e)
			{
				logger.Trace("Device Disconnected: {0}", e.Device.SerialNumber);
			}

			public void OnDeviceConnected(object sender, DeviceEventArgs e)
			{
				logger.Trace("Device Connected: {0}", e.Device.SerialNumber);
			}

			public void OnDeviceChanged(object sender, DeviceEventArgs e)
			{
				logger.Trace("Device Changed: {0}", e.Device.SerialNumber);
			}

		}

		#endregion

		static AndroidBridge()
		{
			Log.LogOutput = adbLogger;
			Log.Level = LogLevel.Verbose;
		}

		static void RunBridge(string fullAdbPath)
		{
			// Disable ClientSupport, we are not using JDWP
			AndroidDebugBridge.Initialize(false);

			// Always force the creation of a new bridge
			AndroidDebugBridge.CreateBridge(fullAdbPath, true);

			AndroidDebugBridge.Instance.BridgeChanged += OnBridgechanged;
			AndroidDebugBridge.Instance.DeviceChanged += OnDeviceChanged;
			AndroidDebugBridge.Instance.DeviceConnected += OnDeviceConnected;
			AndroidDebugBridge.Instance.DeviceDisconnected += OnDeviceDisconnected;
			AndroidDebugBridge.Instance.ClientChanged += OnClientChanged;

			// Make sure the device monitor is running
			if (AndroidDebugBridge.Instance.DeviceMonitor == null)
			{
				var errors = adbLogger.GetLastError();

				if (string.IsNullOrEmpty(errors))
					errors = "Error, could not start android device monitor.";

				throw new PeachException(errors);
			}
		}

		static void OnBridgechanged(object sender, AndroidDebugBridgeEventArgs e)
		{
			if (BridgeChanged != null)
				BridgeChanged(sender, e);
		}

		static void OnDeviceChanged(object sender, DeviceEventArgs e)
		{
			if (DeviceChanged != null)
				DeviceChanged(sender, e);
		}

		static void OnDeviceConnected(object sender, DeviceEventArgs e)
		{
			if (DeviceConnected != null)
				DeviceConnected(sender, e);
		}

		static void OnDeviceDisconnected(object sender, DeviceEventArgs e)
		{
			if (DeviceDisconnected != null)
				DeviceDisconnected(sender, e);
		}

		static void OnClientChanged(object sender, ClientEventArgs e)
		{
			if (ClientChanged != null)
				ClientChanged(sender, e);
		}

		/// <summary>
		/// Occurs when [bridge changed].
		/// </summary>
		public static event EventHandler<AndroidDebugBridgeEventArgs> BridgeChanged;
		/// <summary>
		/// Occurs when [device changed].
		/// </summary>
		public static event EventHandler<DeviceEventArgs> DeviceChanged;
		/// <summary>
		/// Occurs when [device connected].
		/// </summary>
		public static event EventHandler<DeviceEventArgs> DeviceConnected;
		/// <summary>
		/// Occurs when [device disconnected].
		/// </summary>
		public static event EventHandler<DeviceEventArgs> DeviceDisconnected;
		/// <summary>
		/// Occurs when [client changed].
		/// </summary>
		public static event EventHandler<ClientEventArgs> ClientChanged;

		public static void Initialize(string adbPath)
		{
			if (0 == refCount++)
			{
				logger.Debug("Initializing android debug bridge.");

				var paths = adbPath ?? Environment.GetEnvironmentVariable("PATH");
				var dirs = paths.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
				var file = dirs.Select(d => Path.Combine(d, adb)).Where(f => File.Exists(f)).FirstOrDefault();

				if (file == null)
					throw new PeachException("Error, unable to locate {0}{1} 'AdbPath' parameter.".Fmt(
						adb, adbPath != null ? " in specified" : ", please specify using"));

				DeviceChanged += adbLogger.OnDeviceChanged;
				DeviceConnected += adbLogger.OnDeviceConnected;
				DeviceDisconnected += adbLogger.OnDeviceDisconnected;

				RunBridge(file);

				logger.Debug("Android debug bridge initialized.");
			}
		}

		public static void Restart()
		{
			if (0 == refCount)
				throw new NotSupportedException("Bridge has not been initialized.");

			lock (AndroidDebugBridge.GetLock())
			{
				AndroidDebugBridge.Instance.BridgeChanged -= OnBridgechanged;
				AndroidDebugBridge.Instance.DeviceChanged -= OnDeviceChanged;
				AndroidDebugBridge.Instance.DeviceConnected -= OnDeviceConnected;
				AndroidDebugBridge.Instance.DeviceDisconnected -= OnDeviceDisconnected;
				AndroidDebugBridge.Instance.ClientChanged -= OnClientChanged;
			}

			RunBridge(AndroidDebugBridge.AdbOsLocation);
		}

		public static void Terminate()
		{
			if (refCount == 0)
				return;
			
			if (refCount == 1)
			{
				logger.Debug("Terminating android debug bridge.");
				AndroidDebugBridge.Instance.Stop();
				AndroidDebugBridge.Terminate();
			}

			--refCount;
		}
	}
}
