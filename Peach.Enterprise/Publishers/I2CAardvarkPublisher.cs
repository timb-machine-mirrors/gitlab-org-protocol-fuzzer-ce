
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Peach.Core.Dom;
using TotalPhase;
using NLog;
using Peach.Core.IO;

namespace Peach.Core.Publishers
{
	[Publisher("I2C", true)]
	[Parameter("Port", typeof(int), "USB port connected to Aardvark device", "0")]
	[Parameter("Bitrate", typeof(int), "Bitrate supported by target device", "100")]
	[Parameter("Address", typeof(byte), "Address to write/read on target device")]
	[Parameter("SleepTime", typeof(int), "Time to sleep between actions", "100")]

	public class I2CAardvarkPublisher : Publisher
	{

		public int Port { get; set; }
		public int Bitrate { get; set; }
		public byte Address { get; set; }
		public int SleepTime { get; set; }

		public static int MaxRecvSize = 256; //TODO have this be variable?

		private MemoryStream _recvBuffer = null;
		protected int _handle = 0;
		private string excptext = "AardvarkI2CPublisher: ";

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public I2CAardvarkPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(_handle == 0);

			this._handle = AardvarkApi.aa_open(this.Port);
			if (this._handle <= 0)
			{
				throw new PeachException("Can't create an aardvark handle. Handle value is: " + ((int)this._handle).ToString());
			}
			if (AardvarkApi.aa_configure(this._handle, AardvarkConfig.AA_CONFIG_GPIO_I2C) != System.Convert.ToInt32(AardvarkConfig.AA_CONFIG_GPIO_I2C))
			{
				throw new PeachException(this.excptext + "can't set the GPIO I2C configuration!");
			}
			if (AardvarkApi.aa_gpio_direction(this._handle, System.Convert.ToByte(AardvarkGpioBits.AA_GPIO_MOSI | AardvarkGpioBits.AA_GPIO_MISO)) != System.Convert.ToInt32(AardvarkStatus.AA_OK))
			{
				throw new PeachException(this.excptext + "can't set the GPIO deriction bit mask AA_GPIO_MISO | AA_GPIO_MOSI!");
			}
			if (AardvarkApi.aa_gpio_set(this._handle, System.Convert.ToByte(AardvarkGpioBits.AA_GPIO_MOSI | AardvarkGpioBits.AA_GPIO_MISO)) != System.Convert.ToInt32(AardvarkStatus.AA_OK))
			{
				throw new PeachException(this.excptext + "can't set the GPIO bit mask AA_GPIO_MISO | AA_GPIO_MOSI!");
			}
			if (AardvarkApi.aa_i2c_pullup(this._handle, 3) != System.Convert.ToInt32((byte)3))
			{
				throw new PeachException(this.excptext + "can't set the pull up AA_I2C_PULLUP_BOTH!");
			}
			if (AardvarkApi.aa_target_power(this._handle, 3) != System.Convert.ToInt32((byte)3))
			{
				throw new PeachException(this.excptext + "can't set the power on");
			}
			//if (AardvarkApi.aa_i2c_bus_timeout(this._handle, this.timeout) != this.timeout)
			//{
			//	throw new PeachException(this.excptext + "timeout not set!");
			//}
			if (AardvarkApi.aa_i2c_bitrate(this._handle, this.Bitrate) != this.Bitrate)
			{
				throw new PeachException(this.excptext + "bitrate not set!");
			}
			if (AardvarkApi.aa_i2c_slave_enable(this._handle, this.Address, 0, 0) != System.Convert.ToInt32(AardvarkStatus.AA_OK))
			{
				throw new PeachException(this.excptext + "slave is not enable");
			}
			System.Threading.Thread.Sleep(50);

			if (_recvBuffer == null)
				_recvBuffer = new MemoryStream(MaxRecvSize); // TODO Maxbuffer size?
						
		}

		protected override void OnClose()
		{
			if (_handle > 0)
			{
				if (AardvarkApi.aa_i2c_slave_disable(this._handle) != System.Convert.ToInt32(AardvarkStatus.AA_OK))
				{
					throw new PeachException(this.excptext + "can't disable the connection to the slave device");
				}
				if (AardvarkApi.aa_target_power(this._handle, 0) != System.Convert.ToInt32((byte)0))
				{
					throw new PeachException(this.excptext + "can't set the power off");
				}
				if (AardvarkApi.aa_close(this._handle) != 1)
				{
					throw new PeachException("Can't close the connection to the aardvark");
				}
				_handle = 0;
			}
		}

		protected override void OnOutput(BitwiseStream data)
		{
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			data.Seek(0, SeekOrigin.Begin);

			// If data.Length > MaxRecvSize, send will be truncated
			var buf = new byte[MaxRecvSize];
			var size = data.Read(buf, 0, buf.Length);

			try
			{
				int res = AardvarkApi.aa_i2c_write(_handle, Address,
									   AardvarkI2cFlags.AA_I2C_NO_FLAGS,
									   (ushort)size, buf);

				if (res < 0)
				{
					throw new SoftException(System.String.Format("ERROR WRITING TO DEVICE: TODO"));
				}
				AardvarkApi.aa_sleep_ms((uint)SleepTime);				
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to send data to address {0}:{1}",
					Address, ex.Message);	
				throw new SoftException(ex);
			}
		}

		protected override void OnInput()
		{
			System.Diagnostics.Debug.Assert(_recvBuffer != null);
			try
			{
				_recvBuffer.Seek(0, SeekOrigin.Begin);
				_recvBuffer.SetLength(_recvBuffer.Capacity);


				byte[] buf = _recvBuffer.GetBuffer();
				int size = (int)_recvBuffer.Length;

				int res = AardvarkApi.aa_i2c_read(_handle, Address,
						   AardvarkI2cFlags.AA_I2C_NO_FLAGS,
						   (ushort)size, buf);
				if (res < 0)
				{
					throw new SoftException(System.String.Format("ERROR WRITING TO DEVICE: TODO"));
				}

				AardvarkApi.aa_sleep_ms((uint)SleepTime);

				_recvBuffer.SetLength(size);

				if (Logger.IsDebugEnabled)
					Logger.Debug("\n\n" + Utilities.HexDump(_recvBuffer));

				// Got valid data
				return;

			}
			catch (Exception ex)
			{
				Logger.Error("Unable to read data from address {0}:{1}",
					Address, ex.Message);			
				throw new SoftException(ex);
			}
		}

		#region Read Stream

		public override bool CanRead
		{
			get { return _recvBuffer.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _recvBuffer.CanSeek; }
		}

		public override long Length
		{
			get { return _recvBuffer.Length; }
		}

		public override long Position
		{
			get { return _recvBuffer.Position; }
			set { _recvBuffer.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _recvBuffer.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _recvBuffer.Read(buffer, offset, count);
		}

		#endregion

	}
}

// END
