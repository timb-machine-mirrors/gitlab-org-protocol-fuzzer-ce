using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;
using Peach.Core;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalDescriptor : IDescriptor, IGattProperties
	{
		public delegate byte[] ReadHandler(LocalDescriptor chr, IDictionary<string, object> options);
		public delegate void WriteHandler(LocalDescriptor chr, byte[] value, IDictionary<string, object> options);

		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(IDescriptor).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public LocalDescriptor()
		{
			Read = (c, o) => new byte[0];
			Write = (c, v, o) => { };
		}

		public ObjectPath Path { get; set; }

		public ReadHandler Read { get; set; }
		public WriteHandler Write { get; set; }

		#region IDescriptor

		public string UUID { get; set; }
		public ObjectPath Characteristic { get; set; }
		public string[] Flags { get; set; }

		public byte[] ReadValue(IDictionary<string, object> options)
		{
			Logger.Debug("ReadValue>");
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			return Read(this, options);
		}

		public void WriteValue(byte[] value, IDictionary<string, object> options)
		{
			Logger.Debug("WriteValue> Length: {0}", value.Length);
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			Write(this, value, options);
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
				{"Characteristic", Characteristic },
				{"UUID", UUID},
				{"Flags", Flags},
			};
		}

		public event PropertiesChangedHandler PropertiesChanged
		{
			add { }
			remove { }
		}

		#endregion
	}
}
