using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Peach.Core;
using vxlapi_NET;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Publishers.CanDrivers
{
	[CanDriver("Vector XL")]
	public class VectorCanDriver : ICanDriver, ICanInterface, IDisposable
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private static VectorCanDriver _instance = null;
		public static VectorCanDriver Instance
		{
			get
			{
				if(_instance == null)
					_instance = new VectorCanDriver();

				return _instance;
			}
		}

		private string _appName = "PeachFuzzer";
		private bool _initialized = false;
		private readonly XLDriver _driver = new XLDriver();
		private int _portHandle = -1;

		// Driver configuration
		XLClass.xl_driver_config _driverConfig = new XLClass.xl_driver_config();

		// Variables required by XLDriver
		//XLDefine.XL_HardwareType _hwType = XLDefine.XL_HardwareType.XL_HWTYPE_NONE;
		//uint _hwIndex = 0;
		//uint _hwChannel = 0;
		private IntPtr _eventHandle = IntPtr.Zero;
		private UInt64 _accessMask = 0;
		private UInt64 _permissionMask = 0;
		private UInt64 _txMask = 0;
		//int _channelIndex = 0;

		private VectorCanDriver()
		{ }

		#region ICanDriver

		public string Name => "Vector XL";

		static readonly ParameterAttribute[] _parameterAttributes = {
			new ParameterAttribute("Foo", typeof(string), "Bar")
		};

		public IEnumerable<ParameterAttribute> Parameters { get { return _parameterAttributes; } }
		public ICanInterface CreateInterface(Dictionary<string, Variant> args)
		{
			Initialize();
			return this;
		}

		#endregion // ICanDriver

		protected void Initialize()
		{
			if (_initialized)
				return;

			Logger.Debug("VectorCanDriver.Initialize");

			var ret = _driver.XL_OpenDriver();
			if(ret != XLDefine.XL_Status.XL_SUCCESS)
			{
				Logger.Warn("VectorCanDriver.Initialize: Failed opening Vector XL Driver: {0}", ret);
				throw new ApplicationException("Failed opening Vector XL driver: "+ret);
			}

			try
			{
				_driver.XL_GetDriverConfig(ref _driverConfig);

				var channelId = 1;
				foreach (var channel in _driverConfig.channel)
				{
					_channels.Add(new VectorCanChannel(this)
					{
						Id = channelId,
						Config = channel
					});

					channelId++;
				}

				_initialized = true;
			}
			catch
			{
				_driver.XL_CloseDriver();
			}
		}

		#region ICanInterface

		private readonly List<VectorCanChannel> _channels = new List<VectorCanChannel>();

		public IEnumerable<ICanChannel> Channels => _channels;
		private static Thread rxThread;

		private readonly ConcurrentQueue<CanMessage> _rxFrameQueue = new ConcurrentQueue<CanMessage>();
		private ConcurrentQueue<Tuple<DateTime, string, Exception>> _rxLogQueue = new ConcurrentQueue<Tuple<DateTime,string, Exception>>();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int WaitForSingleObject(IntPtr handle, int timeOut);

		/// <summary>
		/// Lock to sync hardware api access
		/// </summary>
		private static object _hwLock = new object();

		public ICanDriver Driver => this;

		public bool IsOpen { get; protected set; }
		public void Open()
		{
			if (!_initialized)
				throw new ApplicationException("Error, not initialized");

			if (IsOpen)
				return;

			_accessMask = 0;
			var appChannelCount = -1;

			var activeChannels = _channels.Where(x => x.IsEnabled);
			foreach (var channel in activeChannels)
			{
				_accessMask |= _driver.XL_GetChannelMask(channel.Config.hwType, channel.Config.hwIndex, channel.Config.channelIndex);

				var ret = _driver.XL_SetApplConfig(_appName, (uint) ++appChannelCount, 
					XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 
					channel.Config.hwIndex, 
					channel.Config.channelIndex, 
					XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);

				if (ret != XLDefine.XL_Status.XL_SUCCESS)
				{
					throw new ApplicationException("XL_SetAppConfig(..., {0}, {1}, {2}, ...) failed for '{3}'".Fmt(
						appChannelCount, channel.Config.hwIndex, channel.Config.channelIndex, channel.Name));
				}
			}

			if (appChannelCount == -1)
			{
				throw new ApplicationException("Error, no active channels found for Vector XL driver.");
			}

			_permissionMask = _txMask = _accessMask;

			// Open port
			var status = _driver.XL_OpenPort(ref _portHandle, _appName, _accessMask, 
				ref _permissionMask, 
				1024, 
				XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, 
				XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
			if (status != XLDefine.XL_Status.XL_SUCCESS)
			{
				throw new ApplicationException("XL_OpenPort failed");
			}

			// Check port
			status = _driver.XL_CanRequestChipState(_portHandle, _accessMask);
			if (status != XLDefine.XL_Status.XL_SUCCESS) 
			{
				throw new ApplicationException("XL_CanRequestChipState failed");
			}

			// Activate channel
			status = _driver.XL_ActivateChannel(_portHandle, _accessMask, 
				XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, 
				XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
			if (status != XLDefine.XL_Status.XL_SUCCESS)
			{
				throw new ApplicationException("XL_ActivateChannel failed");
			}

			// Get RX event handle
			status = _driver.XL_SetNotification(_portHandle, ref _eventHandle, 1);
			if (status != XLDefine.XL_Status.XL_SUCCESS)
			{
				throw new ApplicationException("XL_SetNotification failed");
			}

			// Reset time stamp clock
			status = _driver.XL_ResetClock(_portHandle);
			if (status != XLDefine.XL_Status.XL_SUCCESS)
			{
				throw new ApplicationException("XL_ResetClock failed");
			}

			IsOpen = true;

			// Run Rx Thread
			rxThread = new Thread(RxThread);
			rxThread.Name = "VectorCanDriver.RxThread";
			rxThread.Start();
		}

		public void Close()
		{
			if (!_initialized)
				throw new ApplicationException("Error, not initialized");

			if (!IsOpen)
				return;

			rxThread.Abort();
			_driver.XL_ClosePort(_portHandle);

			IsOpen = false;
		}

		public Tuple<DateTime, string, Exception> GetLogMessage()
		{
			Tuple<DateTime, string, Exception> log;

			if (_rxLogQueue.TryDequeue(out log))
				return log;

			return null;
		}

		public CanMessage ReadMessage()
		{
			if (!_initialized)
				throw new ApplicationException("Error, not initialized");

			if (!IsOpen)
				throw new ApplicationException("Error, CAN interface not open");

			CanMessage msg;
			return _rxFrameQueue.TryDequeue(out msg) ? msg : null;
		}

		public void WriteMessage(CanMessage msg)
		{
			if (msg.Data.Length <= 8)
			{
				WriteMessageCan2(msg);
			}
			else
			{
				WriteMessageCanFd(msg);
			}
		}

		protected void WriteMessageCan2(CanMessage msg)
		{
			try
			{
				if (!_initialized)
					throw new ApplicationException("Error, not initialized");

				if (!IsOpen)
					throw new ApplicationException("Error, CAN interface not open");

				var frames = new XLClass.xl_event_collection(1);

				// Is extended can?
				var id = msg.Identifier;
				if (id > 0x7FF)
				{
					id += 0x80000000;
				}

				frames.xlEvent[0].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;
				frames.xlEvent[0].tagData.can_Msg.id = id;
				frames.xlEvent[0].tagData.can_Msg.dlc = GetDlcFromDataLength(msg.Data.Length, false);
				msg.Data.CopyTo(frames.xlEvent[0].tagData.can_Msg.data, 0);

				lock (_hwLock)
				{
					var ret = _driver.XL_CanTransmit(_portHandle, _txMask, frames);
					if (ret != XLDefine.XL_Status.XL_SUCCESS)
						throw new SoftException("Error sending CAN frame: " + ret.ToString());
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		protected void WriteMessageCanFd(CanMessage msg)
		{
			if (!_initialized)
				throw new ApplicationException("Error, not initialized");

			if (!IsOpen)
				throw new ApplicationException("Error, CAN interface not open");

			var frames = new XLClass.xl_canfd_event_collection(1);

			// Is extended can?
			var id = msg.Identifier;
			if (id > 0x7FF)
			{
				id += 0x80000000;
			}

			frames.xlCANFDEvent[0].tag = XLDefine.XL_CANFD_TX_EventTags.XL_CAN_EV_TAG_TX_MSG;
			frames.xlCANFDEvent[0].tagData.canId = id;
			frames.xlCANFDEvent[0].tagData.msgFlags = XLDefine.XL_CANFD_TX_MessageFlags.XL_CAN_TXMSG_FLAG_BRS | XLDefine.XL_CANFD_TX_MessageFlags.XL_CAN_TXMSG_FLAG_EDL;
			frames.xlCANFDEvent[0].tagData.dlc = (XLDefine.XL_CANFD_DLC)GetDlcFromDataLength(msg.Data.Length, true);
			msg.Data.CopyTo(frames.xlCANFDEvent[0].tagData.data, 0);

			lock (_hwLock)
			{
				uint sentCount = 0;
				var ret = _driver.XL_CanTransmitEx(_portHandle, _txMask, ref sentCount, frames);
				if (ret != XLDefine.XL_Status.XL_SUCCESS)
				{
					Logger.Debug("Error sending CAN frame: " + ret);
					//	throw new PeachException("Error sending CAN frame: " + ret);
				}
			}
		}

		#endregion

		private void RxThread()
		{
			try
			{
				Logger.Debug("VectorCanDriver.Initialize: {0} -Receive loop started", Name);

				_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(
					DateTime.Now, string.Format("{0} - Receive loop started", Name), null));

				// Create new object containing received data 
				var receivedEvent = new XLClass.xl_event();

				// Note: this thread will be destroyed by MAIN
				while (true)
				{
					// Wait for hardware events
					var waitResult = (XLDefine.WaitResults)WaitForSingleObject(_eventHandle, 1000);

					if (waitResult != XLDefine.WaitResults.WAIT_TIMEOUT)
					{
						lock(_hwLock)
						{ 
						var xlStatus = XLDefine.XL_Status.XL_SUCCESS;

						// while hw queue is not empty...
							while (xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY)
							{
								// ...receive data from hardware.
								xlStatus = _driver.XL_Receive(_portHandle, ref receivedEvent);

								// If not success, loop and try again
								if (xlStatus != XLDefine.XL_Status.XL_SUCCESS) continue;

								if ((receivedEvent.flags & XLDefine.XL_MessageFlags.XL_EVENT_FLAG_OVERRUN) != 0)
								{
									_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(
										DateTime.Now, "-- XL_EVENT_FLAG_OVERRUN --", null));
									continue;
								}

								// ...and data is a Rx msg...
								if (receivedEvent.tag != XLDefine.XL_EventTags.XL_RECEIVE_MSG) continue;

								if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0)
								{
									_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(
										DateTime.Now, "-- XL_CAN_MSG_FLAG_OVERRUN --", null));
									continue;
								}

								var msg = new CanMessage
								{
									Timestamp = DateTime.Now,
									Identifier = receivedEvent.tagData.can_Msg.id,
									Interface = this,
									Channel = _channels.FirstOrDefault(x => x.Config.channelIndex == receivedEvent.chanIndex),
									Data = new byte[GetDataSize(receivedEvent)],
									IsError = (receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
									          == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME,
									IsRemote = (receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
									           == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME
								};

								// Convert extended ID to regular ID
								if (msg.Identifier > 0x7FF)
								{
									msg.Identifier -= 0x80000000;
								}

								for (var cnt = 0; cnt < msg.Data.Length; cnt++)
								{
									msg.Data[cnt] = receivedEvent.tagData.can_Msg.data[cnt];
								}

								_rxFrameQueue.Enqueue(msg);
							} // while
						} // lock
					} // if(timeout)
				}// while(true)
			}
			catch (Exception e)
			{
				Logger.Error(e, "RX loop caught exception and exitted");
				_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(
					DateTime.Now, "RX loop caught exception and exitted", e));
			}
		}

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp*0.000001).ToLocalTime();
			return dtDateTime;
		}

		/// <summary>
		/// Get size of msg data in bytes
		/// </summary>
		/// <param name="xlEvent"></param>
		/// <returns></returns>
		private int GetDataSize(XLClass.xl_event xlEvent)
		{
			switch (xlEvent.tagData.can_Msg.dlc)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
					return xlEvent.tagData.can_Msg.dlc;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_12_BYTES:
					return 12;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_16_BYTES:
					return 16;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_20_BYTES:
					return 20;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_24_BYTES:
					return 24;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_32_BYTES:
					return 32;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_48_BYTES:
					return 48;

				case (byte)XLDefine.XL_CANFD_DLC.DLC_CANFD_64_BYTES:
					return 64;

				default:
					return 0;
			}
		}

		private byte GetDlcFromDataLength(int dataLength, bool canFd)
		{
			if (dataLength <= 8)
				return (byte)dataLength;

			if (canFd)
			{
				if (dataLength <= 12)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_12_BYTES;

				if (dataLength <= 16)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_16_BYTES;

				if (dataLength <= 20)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_20_BYTES;

				if (dataLength <= 24)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_24_BYTES;

				if (dataLength <= 32)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_32_BYTES;

				if (dataLength <= 48)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_48_BYTES;

				if (dataLength <= 64)
					return (byte) XLDefine.XL_CANFD_DLC.DLC_CANFD_64_BYTES;
			}

			throw new SoftException("Error, dataLength to large for CAN frame.");
		}

		public void Dispose()
		{
			if (!_initialized)
				return;

			Close();

			_driver.XL_CloseDriver();

			_initialized = false;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class VectorCanChannel : ICanChannel
	{
		public ICanDriver Driver { get; private set; }
		public ICanInterface Interface { get; private set; }
		public XLClass.xl_channel_config Config { get; set; }

		public int Id { get; set; }
		public string Name => Config.name;
		public bool IsEnabled { get; set; }
		public int BaudRate
		{
			get
			{
				return (int)Config.busParams.dataCan.bitrate;
			}

			set
			{
				Config.busParams.dataCan.bitrate = (uint)value;
			}
		}

		public int Samples { get; set; }
		public int Clock { get; set; }

		public VectorCanChannel(VectorCanDriver driver)
		{
			Driver = driver;
			Interface = driver;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
