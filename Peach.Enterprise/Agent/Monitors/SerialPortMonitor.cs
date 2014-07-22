using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Ports;
using System.Threading;

using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;

using NLog;

namespace Peach.Enterprise.Agent.Monitors
{
	/// <summary>
	/// Monitor serial input. Multiple SerialPortMonitors can operate on the same serial port.
	/// </summary>
	/// <remarks>
	/// Map of primary serial port monitor instances. Each serial port will have a single
	/// primary serial port monitor, along with any number of secondary monitors that
	/// receive data from the primary monitor.
	/// 
	/// The use of a primary vs. secondary monitor allows for using multiple SerialPort monitors
	/// that will perform actions based on When conditions.
	/// </remarks>
	[Monitor("SerialPort", true)]
	[Description("Serial port monitoring and fault detection")]
	[Parameter("Port", typeof(string), "The port to use (for example, COM1)")]
	[Parameter("BaudRate", typeof(int), "The baud rate (standard only)", "115200")]
	[Parameter("DataBits", typeof(int), "The data bits value", "8")]
	[Parameter("Parity", typeof(Parity), "Specifies the parity bit. One of: Even, Mark, None, Odd, Space", "None")]
	[Parameter("StopBits", typeof(StopBits), "Specifies the number of stop bits used. One of: None, One, OnePointFive, Two", "None")]
	[Parameter("Handshake", typeof(Handshake), "Specifies the control protocol used in establishing a serial port communication. One of: None, RequestToSend, RequestToSendXOnXOff, XOnXOff", "None")]
	[Parameter("FaultRegex", typeof(string), "Fault when regular expression matches", "")]
	[Parameter("WaitForRegex", typeof(string), "Wait until regex matches received data", "")]
	[Parameter("WaitForWhen", typeof(When), "When to wait", "OnStart")]
	public class SerialPortMonitor : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Map of primary serial port monitor instances. Each serial port will have a single
		/// primary serial port monitor, along with any number of secondary monitors that
		/// receive data from the primary monitor.
		/// </summary>
		/// <remarks>
		/// The use of a primary vs. secondary monitor allows for using multiple SerialPort monitors
		/// that will perform actions based on When conditions.
		/// </remarks>
		static Dictionary<string, SerialPortMonitor> monitors = new Dictionary<string, SerialPortMonitor>();

		public enum When { OnCall, OnStart, OnEnd, OnIterationStart, OnIterationEnd, OnFault, OnIterationStartAfterFault };

		public string Port { get; set; }
		public int BaudRate { get; set; }
		public int DataBits { get; set; }
		public Parity Parity { get; set; }
		public StopBits StopBits { get; set; }
		public Handshake Handshake { get; set; }
		public string FaultRegex { get; set; }
		public string WaitForRegex { get; set; }
		public When WaitForWhen { get; set; }

		public delegate void DataReceivedEventHandler(SerialPortMonitor sender, byte[] data, int length);
		public delegate void IOExceptionEventHandler(SerialPortMonitor sender, IOException exception);

		public MemoryStream Data = new MemoryStream();
		public event DataReceivedEventHandler DataReceived;
		public event IOExceptionEventHandler IOException;

		PeachException _workerException = null;
		Thread _worker = null;
		bool _exit = false;
		Regex _faultRegex = null;
		Regex _waitForRegex = null;
		Fault _fault = null;
		bool _isPrimary = true;
		SerialPortMonitor _primary = null;
		bool _lastWasFault = false;

		public SerialPortMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			if(!string.IsNullOrEmpty(FaultRegex))
				_faultRegex = new Regex(FaultRegex);
			
			if (!string.IsNullOrEmpty(WaitForRegex))
				_waitForRegex = new Regex(WaitForRegex);
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		void OnDataReceived(byte[] data, int length)
		{
			if (DataReceived != null)
				DataReceived(this, data, length);
		}

		void OnIOException(IOException exception)
		{
			if(IOException != null)
				IOException(this, exception);
		}

		public override void SessionStarting()
		{
			if (monitors.ContainsKey(Port))
			{
				_isPrimary = false;
				_primary = monitors[Port];
				Data = _primary.Data;
			}
			else
			{
				monitors[Port] = this;

				_worker = new Thread(new ThreadStart(SerialPortWorker));
				_worker.Start();
			}

			if (!string.IsNullOrEmpty(WaitForRegex) && WaitForWhen == When.OnStart)
				WaitForRegexMatch();
		}

		void WaitForRegexMatch()
		{
			byte[] buff;

			while (true)
			{
				buff = new byte[Data.Length];

				lock (Data)
				{
					Data.Position = 0;
					Data.Read(buff, 0, (int)Data.Length);
				}

				var str = System.Text.UnicodeEncoding.UTF8.GetString(buff);

				if (_waitForRegex.IsMatch(str))
					break;
			}
		}

		public void SerialPortWorker()
		{
			try
			{
				var port = new SerialPort(Port, BaudRate, Parity, DataBits, StopBits);

				try
				{
					port.Handshake = Handshake;
					port.Open();
				}
				catch (Exception ex)
				{
					try
					{
						port.Dispose();
					}
					catch
					{
					}

					_workerException = new PeachException("Error opening serial port '" + Port + "'.", ex);
				}

				try
				{
					int blockLimit = 2048;
					byte[] buffer = new byte[blockLimit];

					System.Action kickoffRead = null;
					
					kickoffRead = delegate
					{
						port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult ar)
						{
							try
							{
								int actualLength = port.BaseStream.EndRead(ar);

								lock (Data)
								{
									Data.Write(buffer, 0, actualLength);
								}

								OnDataReceived(buffer, actualLength);
							}
							catch (IOException exc)
							{
								logger.Warn("Exception reading from port '" + Port + "': " + exc.Message);
								OnIOException(exc);
								
								return;
							}

							lock (_worker)
							{
								if (_exit)
									return;
							}

							kickoffRead();
						}, null);
					};

					kickoffRead();
				}
				catch (Exception ex)
				{
					try
					{
						port.Dispose();
					}
					catch
					{
					}

					_workerException = new PeachException("Error reading from serial port '" + Port + "'.", ex);
				}
			}
			catch (Exception exx)
			{
				_workerException = new PeachException("Error opening serial port '" + Port + "'.", exx);
			}
		}

		public override void SessionFinished()
		{
			if (!string.IsNullOrEmpty(WaitForRegex) && WaitForWhen == When.OnEnd)
				WaitForRegexMatch();

			lock (_worker)
			{
				_exit = true;
			}

			_worker.Join();
			_worker = null;
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			_fault = null;

			bool lastWasFault = _lastWasFault;
			_lastWasFault = false;

			if (!string.IsNullOrEmpty(WaitForRegex) && (WaitForWhen == When.OnIterationStart ||
				(lastWasFault && WaitForWhen == When.OnIterationStartAfterFault)))
			{
				WaitForRegexMatch();
			}

			// Check for exception from worker thread
			if (_workerException != null)
			{
				_exit = true;
				_worker.Join(0);
				_worker = null;

				throw _workerException;
			}

			if (_isPrimary && (_worker == null || _worker.Join(0) || !_worker.IsAlive))
			{
				_worker = new Thread(new ThreadStart(SerialPortWorker));
				_worker.Start();
			}
		}

		public override bool IterationFinished()
		{
			if (!string.IsNullOrEmpty(WaitForRegex) && WaitForWhen == When.OnIterationEnd)
				WaitForRegexMatch();

			return false;
		}

		public override bool DetectedFault()
		{
			if (string.IsNullOrEmpty(FaultRegex))
				return false;

			byte [] buff = new byte[Data.Length];

			lock(Data)
			{
				Data.Position = 0;
				Data.Read(buff, 0, (int)Data.Length);
			}

			_fault = new Fault();
			var str = System.Text.UnicodeEncoding.UTF8.GetString(buff);

			if (_faultRegex.IsMatch(str))
			{
				_fault.type = FaultType.Fault;
				_fault.exploitability = "Unknown";
				_fault.title = "FaultRegex matched serial data";
				_fault.description = "The regular expression \"" + FaultRegex + "\" matched the serial data.";

				return true;
			}
			else
			{
				_fault.type = FaultType.Data;
				_fault.exploitability = "Unknown";
				_fault.title = "SerialPort '"+Port+"' data";
				_fault.description = "Data collected from serial port '"+Port+"'.";
			}

			_fault.collectedData.Add(new Fault.Data("SerialPort-" + Name + "-Data.txt", buff));

			return false;
		}

		public override Fault GetMonitorData()
		{
			_lastWasFault = true;

			if (!string.IsNullOrEmpty(WaitForRegex) && WaitForWhen == When.OnFault)
				WaitForRegexMatch();

			if (_fault != null)
				return _fault;

			if (!_isPrimary)
				return null;

			byte[] buff = new byte[Data.Length];

			lock (Data)
			{
				Data.Position = 0;
				Data.Read(buff, 0, (int)Data.Length);
			}

			var fault = new Fault();
			fault.type = FaultType.Data;
			fault.exploitability = "Unknown";
			fault.title = "SerialPort '" + Port + "' data";
			fault.description = "Data collected from serial port '" + Port + "'.";
			fault.collectedData.Add(new Fault.Data("SerialPort-" + Name + "-Data.txt", buff));

			return fault;
		}

		public override bool MustStop()
		{
			return _workerException != null;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
	}
}

// end
