using System;
using System.IO.Ports;
using Peach.Core;

namespace Peach.Pro.Core.OS
{
	public interface IPal
	{
		ISerialStream OpenSerial(
			string portName,
			int baudRate,
			int dataBits,
			Parity parity,
			StopBits stopBits,
			bool dtrEnable,
			bool rtsEnable,
			Handshake handshake, 
			int readTimeout, 
			int writeTimeout,
			int readBufferSize,
			int writeBufferSize);
	}

	public static class Pal
	{
		public static IPal Instance { get; private set; }

		static Pal()
		{
			switch (Platform.GetOS())
			{
				case Platform.OS.Windows:
					Instance = new Windows.Pal();
					break;
				case Platform.OS.Linux:
					Instance = new Linux.Pal();
					break;
				case Platform.OS.OSX:
					Instance = new OSX.Pal();
					break;
				default:
					throw new NotSupportedException();
			}
		}

		public static ISerialStream OpenSerial(
			string portName,
			int baudRate,
			int dataBits,
			Parity parity,
			StopBits stopBits,
			bool dtrEnable,
			bool rtsEnable,
			Handshake handshake,
			int readTimeout,
			int writeTimeout,
			int readBufferSize,
			int writeBufferSize)
		{
			return Instance.OpenSerial(
				portName,
				baudRate,
				dataBits,
				parity,
				stopBits,
				dtrEnable,
				rtsEnable,
				handshake,
				readTimeout,
				writeTimeout,
				readBufferSize,
				writeBufferSize);
		}
	}
}
