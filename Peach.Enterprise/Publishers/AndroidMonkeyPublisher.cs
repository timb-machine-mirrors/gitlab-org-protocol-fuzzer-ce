using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Peach.Core.IO;
using Peach.Core.Dom;
using Managed.Adb;
using System.IO;
using Peach.Core;
using Peach.Enterprise;

namespace Peach.Enterprise.Publishers
{
	[Publisher("AndroidMonkey", true)]
	[Parameter("Target", typeof(string), "Name of Android Application to Fuzz")] 
	[Parameter("DeviceSerial", typeof(string), "The Serial of the device to fuzz", "")]
	[Parameter("NumActions", typeof(int), "How many actions Monkey should preform on this iteration (default 10)", "10")]
	[Parameter("Sleep", typeof(int), "How much sleep time should be given after an iteration (default 3s)", "3")]
	public class AndroidMonkeyPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string Target { get; set; }
		public string DeviceSerial { get; set; }
		public int NumActions { get; set; }
		public int Sleep { get; set; }
		private Device _dev = null;
		private ConsoleOutputReceiver _creciever = null;
		private uint _x = 0;
		private uint _y = 0;
		private uint _keycode = 0;

		public AndroidMonkeyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_creciever = new ConsoleOutputReceiver();
			_dev = AndroidBridge.GetDevice(DeviceSerial);
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			//need to check if screen is locked
			string _cmd = "";
			try
			{
				if (method.Equals("tap"))
				{
					_cmd = "input tap " + _x.ToString() + " " + _y.ToString();
					logger.Debug("Executing \"tap\" command " + _cmd );
					_dev.ExecuteShellCommand(_cmd,  _creciever);
				}

				else if (method.Equals("keyevent"))
				{
					_cmd = "input keyevent " + _keycode.ToString();
					logger.Debug("Executing \"keyevent\" command " + _cmd);
					_dev.ExecuteShellCommand(_cmd, _creciever);
				}
				else if (method.Equals("text"))
				{
					if (args.Count != 1)
						throw new SoftException("Invalid Pit, 'text' method takes one DataModel as an argument.");

					var bs = args[0].dataModel[0].Value;
					bs.Seek(0, SeekOrigin.Begin);
					var val = new BitReader(bs).ReadString(Peach.Core.Encoding.ISOLatin1);

					var escaped = val.Replace("\"", "\\\"");
					_cmd = "input text \"" + escaped + "\"";
					logger.Debug("Sending text with command " + _cmd);
					_dev.ExecuteShellCommand(_cmd, _creciever);
				}
				else if (method.Equals("monkey"))
				{
					if (IsControlIteration)
					{
						return null;
					}
					if (args.Count != 1)
						throw new SoftException("Invalid Pit, monkey method takes one DataModel as an argument.");

					int seed = (int)args[0].dataModel[0].InternalValue;
					_dev.ExecuteShellCommand("monkey -s " + seed.ToString() + " -p " + Target + " " + NumActions.ToString() + " && sleep " + Sleep.ToString(), _creciever);
				}
			}
			catch (Exception ex)
			{
				// Why would any of these fail? Lets make sure ADB is still running
				try 
				{
					AdbHelper.Instance.GetAdbVersion(AndroidDebugBridge.SocketAddress); 
				}
				catch (System.Net.Sockets.SocketException)
				{
					//adb is down
					AndroidBridge.StartADB();
				}
				throw new SoftException(ex);
			}
			return null;
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			System.Diagnostics.Debug.Assert(value.GetVariantType() == Variant.VariantType.BitStream);
			var bs = (BitwiseStream)value;
			bs.SeekBits(0, SeekOrigin.Begin);
			ulong bits;
			int len = bs.ReadBits(out bits, 32);
			uint prop = Endian.Little.GetUInt32(bits, len);

			if (property.Equals("x"))
			{
				_x = prop;
			}
			else if (property.Equals("y"))
			{
				_y = prop;
			}
			else if (property.Equals("keycode"))
			{
				_keycode = prop;
			}
		}
	}
}
