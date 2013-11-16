using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Peach.Core.IO;
using Peach.Core.Dom;
using System.IO;
using Peach.Core;
using Peach.Enterprise;

namespace Peach.Enterprise.Publishers
{
	[Publisher("AndroidMonkey", true)]
	[Parameter("AdbPath", typeof(string), "Directory containing adb", "")]
	[Parameter("DeviceSerial", typeof(string), "The serial of the device to fuzz", "")]
	[Parameter("DeviceMonitor", typeof(string), "Android monitor to get device serial from", "")]
	[Parameter("ConnectTimeout", typeof(int), "Max seconds to wait for adb connection (default 5)", "5")]
	[Parameter("CommandTimeout", typeof(int), "Max seconds to wait for adb command to complete (default 5)", "5")]
	public class AndroidMonkeyPublisher : Publisher
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return logger; } }

		public int ConnectTimeout { get; protected set; }
		public int CommandTimeout { get; protected set; }
		public string AdbPath { get; protected set; }
		public string DeviceSerial { get; protected set; }
		public string DeviceMonitor { get; protected set; }

		AndroidDevice dev = null;
		bool adbInit = false;

		public AndroidMonkeyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			if (!(string.IsNullOrEmpty(DeviceSerial) ^ string.IsNullOrEmpty(DeviceMonitor)))
				throw new PeachException("Either DeviceSerial parameter or DeviceMonitor parameter is required.");
		}

		void SyncDevice()
		{
			// If the serial came from a monitor, it might change across iterations
			// so we need to make sure out dev member is always correct

			var serial = DeviceSerial;

			if (string.IsNullOrEmpty(serial))
			{
				var dom = this.Test.parent as Peach.Core.Dom.Dom;
				var val = dom.context.agentManager.Message(DeviceMonitor, new Variant("DeviceSerial"));
				if (val == null)
					throw new PeachException("Could not resolve device serial from monitor '" + DeviceMonitor + "'.");

				serial = (string)val;
			}

			if (dev != null && dev.SerialNumber == serial)
				return;

			if (dev == null && DeviceSerial == null)
				logger.Debug("Resolved device '{0}' from monitor '{1}'.", serial, DeviceMonitor);

			if (dev != null)
			{
				logger.Debug("Updating device from old serial '{0}' to new serial '{1}'.", dev.SerialNumber, serial);

				dev.Dispose();
				dev = null;
			}

			dev = AndroidDevice.Get(serial, ConnectTimeout, 0 /* ReadyTimeout */, CommandTimeout);
		}

		protected override void OnStart()
		{
			adbInit = true;
			AndroidBridge.Initialize(AdbPath);
		}

		protected override void OnStop()
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

		static string GetString(BitwiseStream bs)
		{
			return new BitReader(bs).ReadString(Encoding.ISOLatin1);
		}

		static uint GetUInt(BitwiseStream bs)
		{
			ulong bits;
			int len = bs.ReadBits(out bits, 32);
			return Endian.Little.GetUInt32(bits, len);
		}

		static T[] ParseArgs<T>(string method, int numParams, List<ActionParameter> args, Func<BitwiseStream, T> readFunc)
		{
			if (args.Count != numParams)
				throw new SoftException("Error, '{0}' method requires {1} DataModel parameter{2} but {3} {4} provided.".Fmt(
					method, numParams, numParams == 1 ? "s" : "", args.Count, numParams == 1 ? "was" : "were"));

			var ret = new T[numParams];

			for (int i = 0; i < args.Count; ++i)
			{
				var bs = args[i].dataModel.Value;
				bs.Seek(0, SeekOrigin.Begin);
				ret[i] = readFunc(bs);
			}

			return ret;
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			try
			{
				// Defer obtaining of device handle until needed.  This way
				// we support android monitors that use StartOnCall.

				if (method.Equals("tap"))
				{
					var items = ParseArgs<uint>("tap", 2, args, bs => GetUInt(bs));
					SyncDevice();
					dev.Input("tap", items[0].ToString(), items[1].ToString());
				}
				else if (method.Equals("keyevent"))
				{
					var items = ParseArgs<uint>("keyevent", 1, args, bs => GetUInt(bs));
					SyncDevice();
					dev.Input("keyevent", items[0].ToString());
				}
				else if (method.Equals("text"))
				{
					var items = ParseArgs<string>("text", 1, args, bs => GetString(bs));
					SyncDevice();
					dev.Input("text", items[0].ToString());
				}
			}
			catch (SoftException sex)
			{
				logger.Debug("SoftException: {0}", sex.Message);
				throw;
			}
			catch (Exception ex)
			{
				// Can fail if adb doesn't like the values we give it, or
				// if something causes the device to crash.  Treat all exceptions
				// as SoftExceptions and let the engine decide what to do.
				logger.Debug("Exception: {0}", ex.Message);
				throw new SoftException(ex);
			}

			return null;
		}
	}
}
