using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalCharacteristic : ICharacteristic, IGattProperties
	{
		public delegate byte[] ReadHandler(LocalCharacteristic chr, Dictionary<string, object> options);
		public delegate void WriteHandler(LocalCharacteristic chr, byte[] value, Dictionary<string, object> options);

		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(ICharacteristic).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public LocalCharacteristic()
		{
			Descriptors = new List<LocalDescriptor>();
			Read = (c, o) => new byte[0];
			Write = (c, v, o) => { };
		}

		public ObjectPath Path { get; set; }
		public List<LocalDescriptor> Descriptors { get; private set; }

		public ReadHandler Read { get; set; }
		public WriteHandler Write { get; set; }

		#region ICharacteristic

		public string UUID { get; set; }
		public ObjectPath Service { get; set; }
		public string[] Flags { get; set; }

		public byte[] ReadValue(Dictionary<string, object> options)
		{
			Logger.Debug("ReadValue>");
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			return Read(this, options);
		}

		public void WriteValue(byte[] value, Dictionary<string, object> options)
		{
			Logger.Debug("WriteValue> Length: {0}", value.Length);
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			Write(this, value, options);
		}

		public void StartNotify()
		{
			Logger.Debug("StartNotify>");
		}

		public void StopNotify()
		{
			Logger.Debug("StopNotify>");
		}

		public void AcquireNotify(Dictionary<string, object> options, out int fd, out ushort mtu)
		{
			Logger.Debug("AcquireNotify>");
			fd = 0;
			mtu = 0;
		}

		#endregion

		#region IGattProperties

		public string InterfaceName { get { return Attr.Name; } }

		public object Get(string @interface, string propname)
		{
			Logger.Trace("Get> {0} {1}", @interface, propname);
			return null;
		}

		public void Set(string @interface, string propname, object value)
		{
			Logger.Trace("Set> {0} {1}={2}", @interface, propname, value);
		}

		public IDictionary<string, object> GetAll(string @interface)
		{
			Logger.Trace("GetAll> {0}", @interface);

			if (@interface != InterfaceName)
				return new Dictionary<string, object>();

			return new Dictionary<string, object>
			{
				{"Service", Service },
				{"UUID", UUID},
				{"Flags", Flags},
			};
		}

		public event PropertiesChangedHandler PropertiesChangedEvent
		{
			add { }
			remove { }
		}

		#endregion
	}
}
