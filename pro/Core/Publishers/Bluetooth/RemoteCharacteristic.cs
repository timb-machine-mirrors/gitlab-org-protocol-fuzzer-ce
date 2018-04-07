using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class RemoteCharacteristic : ICharacteristic
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(ICharacteristic).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly ICharacteristic _char;
		private readonly Properties _props;
		private readonly Introspectable _info;

		public RemoteCharacteristic(Bus bus, ObjectPath path)
		{
			_char = bus.GetObject<ICharacteristic>(path);
			_props = bus.GetObject<Properties>(path);
			_props.PropertiesChangedEvent += OnPropertiesChanged;
			_info = bus.GetObject<Introspectable>(path);

			Path = path;
			Descriptors = new ByUuid<RemoteDescriptor>();
		}

		public ObjectPath Path
		{
			get;
			private set;
		}

		public ByUuid<RemoteDescriptor> Descriptors
		{
			get;
			private set;
		}

		public IDictionary<string, object> Properties
		{
			get { return _props.GetAll(Attr.Name); }
		}

		public string Introspect()
		{
			return _info.IntrospectPretty();
		}

		public byte[] ReadValue(Dictionary<string, object> options)
		{
			return _char.ReadValue(options);
		}

		public void WriteValue(byte[] value, Dictionary<string, object> options)
		{
			_char.WriteValue(value, options);
		}

		public void StartNotify()
		{
			_char.StartNotify();
		}

		public void StopNotify()
		{
			_char.StopNotify();
		}

		public void AcquireNotify(Dictionary<string, object> options, out int fd, out ushort mtu)
		{
			_char.AcquireNotify(options, out fd, out mtu);
		}

		private void OnPropertiesChanged(string s, Dictionary<string, object> d, string[] a)
		{
			Logger.Debug("OnPropertiesChanged> {0} ({1})", s, string.Join(",", a));

			foreach (var kv in d)
				Logger.Debug("OnPropertiesChanged>  {0}={1}", kv.Key, kv.Value);

		}

		private T Get<T>(string name)
		{
			return (T)_props.Get(Attr.Name, name);
		}

		public string UUID { get { return Get<string>("UUID"); } }
		public ObjectPath Service { get { return Get<ObjectPath>("Service"); } }
		public byte[] Value { get { return Get<byte[]>("Value"); } }
		public bool Notifying { get { return Get<bool>("Notifying"); } }
		public string[] Flags { get { return Get<string[]>("Flags"); } }
	}
}
