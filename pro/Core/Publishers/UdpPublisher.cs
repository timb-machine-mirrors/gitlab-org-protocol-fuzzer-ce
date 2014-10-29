
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using NLog;

using Peach.Core.IO;

namespace Peach.Core.Publishers
{
	[Publisher("Udp", true)]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Port", typeof(ushort), "Destination port number", "0")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("SrcPort", typeof(ushort), "Source port number", "0")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class UdpPublisher : SocketPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }
		private IPEndPoint _remote;

		public UdpPublisher(Dictionary<string, Variant> args)
			: base("Udp", args)
		{
		}

		protected override bool AddressFamilySupported(AddressFamily af)
		{
			return (af == AddressFamily.InterNetwork) || (af == AddressFamily.InterNetworkV6);
		}

		protected override Socket OpenSocket(EndPoint remote)
		{
			_remote = (IPEndPoint)remote;
			Socket s = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			return s;
		}

		protected override void FilterOutput(byte[] buffer, int offset, int count)
		{
			base.FilterOutput(buffer, offset, count);

			if (_remote.Port == 0)
				throw new PeachException("Error sending a Udp packet to " + _remote.Address + ", the port was not specified.");
		}

		protected override Variant OnGetProperty(string property)
		{
			switch(property)
			{
				case "Port":
					return new Variant(Port);
				case "SrcPort":
					return new Variant(SrcPort);
			}

			return base.OnGetProperty(property);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			ushort newPort;

			switch(property)
			{
				case "Port":
					newPort = UShortFromVariant(value);
					Logger.Debug("Changing Port from {0} to {1}.\n", Port, newPort);

					Port = newPort;
					OnStop();
					OnStart();
					return;

				case "SrcPort":
					newPort = UShortFromVariant(value);
					Logger.Debug("Changing SrcPort from {0} to {1}.\n", SrcPort, newPort);

					SrcPort = newPort;
					OnStop();
					OnStart();
					return;
			}

			base.OnSetProperty(property, value);
		}

		public ushort UShortFromVariant(Variant value)
		{
			ushort ret = 0;

			if (value.GetVariantType() == Variant.VariantType.BitStream)
			{
				var bs = (BitwiseStream)value;
				bs.SeekBits(0, SeekOrigin.Begin);
				ulong bits;
				int len = bs.ReadBits(out bits, 16);
				ret = Endian.Little.GetUInt16(bits, len);
			}
			else if (value.GetVariantType() == Variant.VariantType.ByteString)
			{
				byte[] buf = (byte[])value;
				int len = Math.Min(buf.Length * 8, 16);
				ret = Endian.Little.GetUInt16(buf, len);
			}
			else
			{
				try
				{
					ret = ushort.Parse((string)value);
				}
				catch
				{
					throw new SoftException("Can't convert to int, 'value' is an unsupported type.");
				}
			}

			return ret;
		}
	}
}
