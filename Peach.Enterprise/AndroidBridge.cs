using System;
using System.Collections.Generic;
using Managed.Adb;
using System.Linq;
using System.Text;
using Peach.Core;
using System.Diagnostics;
using NLog;
using System.IO;


namespace Peach.Enterprise
{
    static public class AndroidBridge
		{
			static NLog.Logger logger = LogManager.GetCurrentClassLogger();
			static private string _adbBinPath = null;

			// we only want to grab devices once per peach session
			static List<Managed.Adb.Device> _devices = null;
			static public List<Managed.Adb.Device> Devices {
				get { if (_devices == null)
						try
						{
							_devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress); 
						}
						catch (System.Net.Sockets.SocketException)
						{
						 logger.Debug("ADB connection could not be established, starting ADB.");
							StartADB();
							// set a recursion limit here? If ADB is broken this will fail horribly.
							return Devices;
						}
					return _devices; 
				}
				private set {}
			}
			
			public static Device GetDevice(string DeviceSerial)
			{
				if (string.IsNullOrEmpty(DeviceSerial))
					return GetFirstDevice();
				else
					return GetDeviceBySerial(DeviceSerial);
			}

			static Device GetFirstDevice()
			{
				try 
				{
					return Devices[0];
				} 
				catch (System.ArgumentOutOfRangeException)
				{
					throw new PeachException("No Devices conencted");
				}
			}
				
			static Device GetDeviceBySerial(string DeviceSerial)
			{
				try
				{
					logger.Debug("DeviceSerial is set to " + DeviceSerial);
					logger.Debug("Attempting to fetch device with serial " +  DeviceSerial);
					foreach (Device d in Devices)
						if (d.SerialNumber == DeviceSerial)
						{
							logger.Debug("Device found");
							return d;
						}
				throw new PeachException("Device " + DeviceSerial + "not found. Use `adb devices` to verify it's plugged in.");
				}
				catch (Exception ex)
				{
					throw new SoftException(ex);
				}

			}

			public static void SetAdbPath(string path)
			{
				var adb = Platform.GetOS() == Platform.OS.Windows ? "adb.exe" : "adb";

				if (path != null)
				{
					var fullPath = Path.Combine(path, adb);

					if (!File.Exists(fullPath))
						throw new PeachException("Error, unable to locate " + adb + "in provided AdbPath.");

					_adbBinPath = fullPath;
				}
				else
				{
					path = Environment.GetEnvironmentVariable("PATH");
					var dirs = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
					
					foreach (var dir in dirs)
					{
						var fullPath = Path.Combine(dir, adb);
						
						if (File.Exists(fullPath))
							_adbBinPath = fullPath;
					}
				}
			}

			static void controlAdb(string command)
			{
				Process p = new Process();
				p.StartInfo.FileName = _adbBinPath;
				p.StartInfo.Arguments = command;
				p.StartInfo.UseShellExecute = false;
				p.Start();
				p.WaitForExit();
			}
			
			public static void RestartADB()
			{
				StopADB();
				StartADB();
			}

			public static void StopADB()
			{
				// this can fail, add exception handling
				controlAdb("kill-server");
			}
			
			public static void StartADB()
			{
				// this can fail, add exception handling
				controlAdb("start-server");
			}
			
		}
}
