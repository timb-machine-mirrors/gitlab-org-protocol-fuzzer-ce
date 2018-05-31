using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Core.Publishers.Can
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class CanDriverAttribute : Attribute
	{
		/// <summary>
		/// Mark CAN bus drivers
		/// </summary>
		/// <param name="name">Display name of driver</param>
		public CanDriverAttribute(string name)
		{
			Name = name;
		}
		public string Name { get; set; }
	}

	/// <summary>
	/// Helper class for finding CAN drivers
	/// </summary>
	public class CanDrivers
	{
		private static Tuple<string, ICanDriver>[] _drivers;

		public static IEnumerable<Tuple<string, ICanDriver>> Drivers
		{
			get
			{
				if (_drivers != null) return _drivers;

				var types = ClassLoader.GetAllByAttribute<CanDriverAttribute>();

				_drivers = types.Select(
					t => new Tuple<string, ICanDriver>(
						t.Key.Name,
						(ICanDriver)t.Value.GetProperty("Instance").GetValue(null))
						).ToArray();

				return _drivers;
			}
		}
	}

	/// <summary>
	/// Driver for a specific can interface.
	/// Implemented by drivers to support different vendors stuff.
	/// </summary>
	public interface ICanDriver
	{
		/// <summary>
		/// Name of driver. Must match CanDriver attribute.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Parameters for this driver
		/// </summary>
		IEnumerable<ParameterAttribute> Parameters { get; }

		/// <summary>
		/// Create an interface using supplied arguments
		/// </summary>
		/// <remarks>
		/// Same format as publisher contructors.
		/// Must provide all required arguments as defined by Parameters property.
		/// </remarks>
		/// <param name="args">Arguments dictionary</param>
		/// <returns></returns>
		ICanInterface CreateInterface(Dictionary<string, Variant> args);
	}

	/// <summary>
	/// CAN channel.  Interfaces have one or more channel.
	/// </summary>
	public interface ICanChannel
	{
		/// <summary>
		/// Integer identifier starting at 1 for channel.
		/// </summary>
		int Id { get; }

		/// <summary>
		/// Human display name for channel.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Enable or disable channel.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Baudrate for channel
		/// </summary>
		int BaudRate { get; set; }

		int Samples { get; set; }
		int Clock { get; set; }

		ICanInterface Interface { get; }
		ICanDriver Driver { get; }
	}

	/// <summary>
	/// Deligate for handling received CAN frames
	/// </summary>
	/// <param name="sender">Instance of ICanInterface receiving frame</param>
	/// <param name="frame">Frame received</param>
	public delegate void CanRxEventHandler(ICanInterface sender, CanFrame frame);

	/// <summary>
	/// CAN interface
	/// </summary>
	public interface ICanInterface
	{
		ICanDriver Driver { get; }

		/// <summary>
		/// Channels available through interface
		/// </summary>
		IEnumerable<ICanChannel> Channels { get; }

		/// <summary>
		/// Is interface in open state?  
		/// </summary>
		bool IsOpen { get; }

		/// <summary>
		/// Open interface
		/// </summary>
		void Open();

		/// <summary>
		/// Close interface
		/// </summary>
		void Close();

		/// <summary>
		/// Register a CAN Frame Received handler with an CAN Frame ID filter.
		/// </summary>
		/// <param name="id">CAN Frame IDs</param>
		/// <param name="handler">Handler method</param>
		void RegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler);

		/// <summary>
		/// Register a CAN Frame Received handler with an CAN Frame ID filter.
		/// </summary>
		/// <param name="ids">One or more CAN Frame IDs</param>
		/// <param name="handler">Handler method</param>
		void RegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler);

		/// <summary>
		/// Un-register a CAN Frame Received handler with an CAN Frame ID filter.
		/// </summary>
		/// <param name="id">CAN Frame IDs</param>
		/// <param name="handler">Handler method</param>
		void UnRegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler);

		/// <summary>
		/// Unregister a CAN Frame Received handler with an CAN Frame ID filter.
		/// </summary>
		/// <param name="ids">One or more CAN Frame IDs</param>
		/// <param name="handler">Handler method</param>
		void UnRegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler);

		/// <summary>
		/// Read single CAN frame
		/// </summary>
		/// <returns>Frame or null if no frames are available.</returns>
		CanFrame ReadMessage();

		/// <summary>
		/// Write CAN frame to bus
		/// </summary>
		/// <param name="txChannel">Channel to transmit frame on</param>
		/// <param name="frame">Frame instance to send</param>
		void WriteMessage(ICanChannel txChannel, CanFrame frame);

		/// <summary>
		/// Get a log frame (if supported).
		/// </summary>
		/// <remarks>
		/// This allows the driver to provide messages/errors
		/// </remarks>
		/// <returns></returns>
		Tuple<DateTime, string, Exception> GetLogMessage();
	}

	/// <summary>
	/// CAN frame that was read or can be sent by a driver
	/// </summary>
	public class CanFrame
	{
		/// <summary>
		/// ID for can frame
		/// </summary>
		public uint Identifier;

		/// <summary>
		/// Timestamp frame was received
		/// </summary>
		public DateTime Timestamp;

		/// <summary>
		/// Interface frame was received on
		/// </summary>
		public ICanInterface Interface;

		/// <summary>
		/// Channel frame was received on
		/// </summary>
		public ICanChannel Channel;

		/// <summary>
		/// Is Remote
		/// </summary>
		public bool IsRemote;

		/// <summary>
		/// Is Error
		/// </summary>
		public bool IsError;

		/// <summary>
		/// Data portion of can frame.  Excludes ID, flags, etc.
		/// </summary>
		public byte[] Data;

		public CanFrame()
		{ }

		public CanFrame(uint identifier, bool isRemote, byte[] data)
		{
			Identifier = identifier;
			IsRemote = isRemote;
			Data = data;
		}
	}
}
