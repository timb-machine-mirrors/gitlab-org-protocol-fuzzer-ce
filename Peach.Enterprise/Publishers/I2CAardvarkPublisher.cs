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
	[Parameter("port", typeof(int), "USB port connected to Aardvark device", "0")]
	[Parameter("bitrate", typeof(int), "Bitrate supported by target device", "100")]
	[Parameter("address", typeof(byte), "Address to write/read on target device")]
	[Parameter("power", typeof(bool), "Supply power to target [true/false, default true]", "true")]
	[Parameter("pullup", typeof(bool), "Enable pullup resistors [true/false, default true]", "true")]
	[Parameter("sleepTime", typeof(int), "Time to sleep between actions [default 1000 ms]", "1000")]

	public class I2CAardvarkPublisher : Publisher
	{

		protected int handle = 0;
		public int port { get; set; }
		public int bitrate { get; set; }
		public byte address { get; set; }
		public bool power { get; set; }
		public bool pullup { get; set; }
		public int sleepTime { get; set; }

		protected byte[] _received = null;


		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public I2CAardvarkPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(handle == 0);

			handle = AardvarkApi.aa_open(port);

			if (handle <= 0)
			{
				throw new PeachException(System.String.Format("Unable to open Aardvark device on port {0}: {1}",
								  port, AardvarkApi.aa_status_string(handle)));
			}

			// Ensure that the I2C subsystem is enabled
			AardvarkApi.aa_configure(handle, AardvarkConfig.AA_CONFIG_SPI_I2C);

			// Enable the I2C bus pullup resistors (2.2k resistors).
			// This command is only effective on v2.0 hardware or greater.
			// The pullup resistors on the v1.02 hardware are enabled by default.
			if (pullup)
			{
				AardvarkApi.aa_i2c_pullup(handle, AardvarkApi.AA_I2C_PULLUP_BOTH);
			}
			// Power the board using the Aardvark adapter's power supply.
			// This command is only effective on v2.0 hardware or greater.
			// The power pins on the v1.02 hardware are not enabled by default.
			if (power)
			{
				AardvarkApi.aa_target_power(handle,
											 AardvarkApi.AA_TARGET_POWER_BOTH);
			}
			// Set the bitrate
			bitrate = AardvarkApi.aa_i2c_bitrate(handle, bitrate);

		}

		protected override void OnClose()
		{
			if (handle > 0)
			{
				AardvarkApi.aa_close(handle);
				handle = 0;
			}
		}

		protected override void OnOutput(byte[] buffer, int offset, int count)
		{

			byte[] dataSend = buffer.Skip(offset).Take(count).ToArray();
			int res = AardvarkApi.aa_i2c_write(handle, address,
									   AardvarkI2cFlags.AA_I2C_NO_FLAGS,
									   (ushort)count, dataSend);
			if (res < 0)
			{
				throw new SoftException(System.String.Format("ERROR WRITING TO DEVICE: TODO"));
			}
			AardvarkApi.aa_sleep_ms((uint)sleepTime);
		}

		protected override void OnInput()
		{
			int length = 8;//TODO where does this number come from?
			byte[] dataIn = new byte[length];
			int res = AardvarkApi.aa_i2c_read(handle, address,
									   AardvarkI2cFlags.AA_I2C_NO_FLAGS,
									   (ushort)length, dataIn);
			_received = dataIn;
		}

	}
}

// END
