﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using NLog;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("SerialPort")]
	[Parameter("PortName", typeof(string), "Com interface for the device to connect to")]
	[Parameter("Baudrate", typeof(int), "The serial baud rate.")]
	[Parameter("Parity", typeof(Parity), "The parity-checking protocol.")]
	[Parameter("DataBits", typeof(int), "Standard length of data bits per byte.")]
	[Parameter("StopBits", typeof(StopBits), "The standard number of stopbits per byte.")]
	[Parameter("Handshake", typeof(Handshake), "The handshaking protocol for serial port transmission of data.", "None")]
	[Parameter("DtrEnable", typeof(bool), "Enables the Data Terminal Ready (DTR) signal during serial communication.", "false")]
	[Parameter("RtsEnable", typeof(bool), "Enables the Request To Transmit (RTS) signal during serial communication.", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	public class SerialPortPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string PortName { get; protected set; }
		public int Baudrate { get; protected set; }
		public Parity Parity { get; protected set; }
		public int DataBits { get; protected set; }
		public StopBits StopBits { get; protected set; }
		public Handshake Handshake { get; protected set; }
		public bool DtrEnable { get; protected set; }
		public bool RtsEnable { get; protected set; }

		protected SerialPort _serial;

		public SerialPortPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			try
			{
				_serial = new SerialPort(PortName, Baudrate, Parity, DataBits, StopBits);
				_serial.Handshake = Handshake;
				_serial.DtrEnable = DtrEnable;
				_serial.RtsEnable = RtsEnable;

				// Set timeout values
				_serial.ReadTimeout = (Timeout >= 0 ? Timeout : SerialPort.InfiniteTimeout);
				_serial.WriteTimeout = (Timeout >= 0 ? Timeout : SerialPort.InfiniteTimeout);

				_serial.Open();
				_clientName = _serial.PortName;
				_client = _serial.BaseStream;
			}
			catch (Exception ex)
			{
				string msg = "Unable to open Serial Port {0}. {1}.".Fmt(PortName, ex.Message);
				Logger.Error(msg);
				throw new SoftException(msg, ex);
			}

			StartClient();
		}

		protected override void OnOutput(BitwiseStream data)
		{
			Stream client;
			lock (_clientLock)
			{
				client = _client;
			}

			if (client != null)
			{
				// The async API under mono doesn't seem to work with the SerialPort.
				// Since the SerialPort has support for timeouts, we can use the synchronous API here.
				data.CopyTo(client);
			}
		}
	}
}
