using System.IO.Ports;

namespace Peach.Pro.Core.OS.Windows
{
	internal class Pal : IPal
	{
		public ISerialStream OpenSerial(
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
			return new WinSerialStream(
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
