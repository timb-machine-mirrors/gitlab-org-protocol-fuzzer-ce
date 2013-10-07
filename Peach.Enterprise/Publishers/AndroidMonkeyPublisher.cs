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

namespace Peach.Enterprise.Publishers
{
	[Publisher("AndroidMonkey", true)]
	[Parameter("Target", typeof(string), "Name of Android Application to Fuzz")]
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
		}

		private void GrabDevice()
		{
			try
			{
				logger.Debug("DeviceSerial is set to " + DeviceSerial);
				List<Managed.Adb.Device> _devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);
				if (DeviceSerial == null || (_devices.Count == 1))
				{
					logger.Debug("Attempting to fetch first adb device");
					_dev = _devices[0];
				}
				else
				{
					logger.Debug("Attempting to fetch device with serial " +  DeviceSerial);
					//should this be a LINQ statement?
					foreach (Device d in _devices)
						if (d.SerialNumber == DeviceSerial)
						{
							logger.Debug("Device found");
							_dev = d;
							break;
						}
				}
				if ( _dev == null)
					throw new PeachException("Device " + DeviceSerial + "not found");
				else
					logger.Debug("Using device with serial " + _dev.SerialNumber);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			GrabDevice();
			try
			{
				if (method.Equals("tap"))
				{
					_dev.ExecuteShellCommand("input tap " + _x.ToString() + " " + _y.ToString(), _creciever);
				}

				else if (method.Equals("keyevent"))
				{
					_dev.ExecuteShellCommand("input keyevent " + _keycode.ToString(), _creciever);
				}
				else if (method.Equals("text"))
				{
					if (args.Count != 1)
						throw new SoftException("Invalid Pit, 'text' method takes one DataModel as an argument.");

					var bs = args[0].dataModel[0].Value;
					bs.Seek(0, SeekOrigin.Begin);
					var val = new BitReader(bs).ReadString(Peach.Core.Encoding.ISOLatin1);

					var escaped = val.Replace("\"", "\\\"");
					_dev.ExecuteShellCommand("input text \"" + escaped + "\"", _creciever);
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
