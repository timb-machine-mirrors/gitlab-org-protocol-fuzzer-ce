//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Ayzenberg (mick@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Peach.Core.Dom;
using TotalPhase;
using NLog;

namespace Peach.Core.Publishers
{
	[Publisher("I2C", true)]
	[Parameter("Port", typeof(int), "USB port connected to Aardvark device", "0")]
	[Parameter("Bitrate", typeof(int), "Bitrate supported by target device", "100")]
	[Parameter("Address", typeof(byte), "Address to write/read on target device")]
	[Parameter("Power", typeof(bool), "Supply power to target [true/false, default true]", "true")]
	[Parameter("Pullup", typeof(bool), "Enable pullup resistors [true/false, default true]", "true")]
	[Parameter("SleepTime", typeof(int), "Time to sleep between actions [default 1000 ms]", "1000")]
	[Parameter("FrameSize", typeof(int), "Size of frames to read and write [default is 1 byte]", "1")]

	public class I2CAardvarkPublisher : Publisher
	{

		public int Port { get; set; }
		public int Bitrate { get; set; }
		public byte Address { get; set; }
		public bool Power { get; set; }
		public bool Pullup { get; set; }
		public int SleepTime { get; set; }
		public int FrameSize { get; set; }

		public static int MaxSendSize = 128; //TODO, also its in bytes

		private MemoryStream _recvBuffer = null;
		protected int _handle = 0;

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public I2CAardvarkPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(_handle == 0);

			_handle = AardvarkApi.aa_open(Port);

			if (_handle <= 0)
			{
				throw new PeachException(System.String.Format("Unable to open Aardvark device on port {0}: {1}",
								  Port, AardvarkApi.aa_status_string(_handle)));
			}

			// Ensure that the I2C subsystem is enabled
			AardvarkApi.aa_configure(_handle, AardvarkConfig.AA_CONFIG_SPI_I2C);

			// Enable the I2C bus pullup resistors (2.2k resistors).
			// This command is only effective on v2.0 hardware or greater.
			// The pullup resistors on the v1.02 hardware are enabled by default.
			if (Pullup)
			{
				AardvarkApi.aa_i2c_pullup(_handle, AardvarkApi.AA_I2C_PULLUP_BOTH);
			}
			// Power the board using the Aardvark adapter's power supply.
			// This command is only effective on v2.0 hardware or greater.
			// The power pins on the v1.02 hardware are not enabled by default.
			if (Power)
			{
				AardvarkApi.aa_target_power(_handle,
											 AardvarkApi.AA_TARGET_POWER_BOTH);
			}
			// Set the bitrate
			Bitrate = AardvarkApi.aa_i2c_bitrate(_handle, Bitrate);

			if (_recvBuffer == null)
				_recvBuffer = new MemoryStream(FrameSize); // TODO Maxbuffer size?
		}

		protected override void OnClose()
		{
			if (_handle > 0)
			{
				AardvarkApi.aa_close(_handle);
				_handle = 0;
			}
		}

		protected override void OnOutput(byte[] buffer, int offset, int count)
		{
			int size = count;
			if (size > MaxSendSize)
			{
				// This will be logged below as a truncated send
				size = MaxSendSize;
			}
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(buffer, offset, count));

			try
			{
				for (int i = offset; i<size+offset; i += FrameSize){
					int arraySize = FrameSize > size ? size : FrameSize;
					byte[] dataSend = buffer.Skip(i).Take(arraySize).ToArray();
					int res = AardvarkApi.aa_i2c_write(_handle, Address,
										   AardvarkI2cFlags.AA_I2C_NO_FLAGS,
										   (ushort)arraySize, dataSend);
					if (res < 0)
					{
						throw new SoftException(System.String.Format("ERROR WRITING TO DEVICE: TODO"));
					}
					AardvarkApi.aa_sleep_ms((uint)SleepTime);
				}
				
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
				int offset = (int)_recvBuffer.Position;
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
