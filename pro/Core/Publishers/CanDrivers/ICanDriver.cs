using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Peach.Core;

namespace Peach.Pro.Core.Publishers.CanDrivers
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
		/// Is interface in open state
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
		/// Read CAN message
		/// </summary>
		/// <returns></returns>
		CanMessage ReadMessage();

		/// <summary>
		/// Write CAN message to buss
		/// </summary>
		/// <param name="message"></param>
		void WriteMessage(CanMessage message);

		/// <summary>
		/// Get a log message (if supported).
		/// </summary>
		/// <remarks>
		/// This allows the driver to provide messages/errors
		/// </remarks>
		/// <returns></returns>
		Tuple<DateTime, string, Exception> GetLogMessage();
	}

	/// <summary>
	/// CAN message
	/// </summary>
	public class CanMessage
	{
		public uint Identifier;
		public DateTime Timestamp;
		public ICanInterface Interface;
		public ICanChannel Channel;
		public bool IsRemote;
		public bool IsError;
		public byte[] Data;

		public CanMessage()
		{ }

		public CanMessage(uint identifier, bool isRemote, byte[] data)
		{
			Identifier = identifier;
			IsRemote = isRemote;
			Data = data;
		}
	}
}
