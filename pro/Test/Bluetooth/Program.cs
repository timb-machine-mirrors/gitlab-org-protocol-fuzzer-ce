using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Test.Bluetooth
{
	[Interface("org.bluez.Device1")]
	public interface IDevice
	{
		void Connect();
		void Disconnect();
		void Pair();
		void CancelPairing();
	}

	[Interface("org.bluez.Adapter1")]
	public interface IAdapter
	{
		void StartDiscovery();
		void SetDiscoveryFilter(Dictionary<string, object> filter);
		void StopDiscovery();
		void RemoveDevice(object device);
	}

	[Interface("org.bluez.GattService1")]
	public interface IService
	{
	}

	[Interface("org.bluez.GattCharacteristic1")]
	public interface ICharacteristic
	{
		byte[] ReadValue(Dictionary<string, object> options);
		void WriteValue(byte[] value, Dictionary<string, object> options);
		void StartNotify();
		void StopNotify();
	}

	[Interface("org.bluez.GattDescriptor1")]
	public interface IDescriptor
	{
		byte[] ReadValue(Dictionary<string, object> options);
		void WriteValue(byte[] value, Dictionary<string, object> options);
	}

	public interface IUuid
	{
		string UUID { get; }
	}

	public class ByUuid<T> : KeyedCollection<string, T> where T : IUuid
	{
		protected override string GetKeyForItem(T item)
		{
			return item.UUID;
		}
	}

	public static class DBusExtensions
	{
		public static ObjectPath Parent(this ObjectPath item)
		{
			var value = item.ToString();
			if (value == ObjectPath.Root.ToString())
				return null;
			var str = value.Substring(0, value.LastIndexOf('/'));
			if (str == string.Empty)
				str = "/";
			return new ObjectPath(str);
		}

		public static IEnumerable<ObjectPath> Iter<T>(this IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objs)
		{
			var iface = typeof(T).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					yield return kv.Key;
				}
			}
		}

		public static IEnumerable<ObjectPath> Iter<T>(this IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objs, Func<IDictionary<string, object>, bool> pred)
		{
			var iface = typeof(T).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					if (pred(item.Value))
						yield return kv.Key;
				}
			}
		}
	}

	public class BluetoothPub : IDisposable
	{
		public class BluetoothAdapter : IAdapter
		{
			private static readonly InterfaceAttribute Attr =
				typeof(IAdapter).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			private readonly IAdapter _adapter;
			private readonly Properties _props;
			private readonly Introspectable _info;

			public BluetoothAdapter(Bus bus, ObjectPath path)
			{
				_adapter = bus.GetObject<IAdapter>(BusName, path);
				_props = bus.GetObject<Properties>(BusName, path);
				_info = bus.GetObject<Introspectable>(BusName, path);

				Path = path;
			}

			public ObjectPath Path
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
				return PrettyXml(_info.Introspect());
			}

			public void StartDiscovery()
			{
				_adapter.StartDiscovery();
			}

			public void SetDiscoveryFilter(Dictionary<string, object> filter)
			{
				_adapter.SetDiscoveryFilter(filter);
			}

			public void StopDiscovery()
			{
				_adapter.StopDiscovery();
			}

			public void RemoveDevice(object device)
			{
				_adapter.RemoveDevice(device);
			}

			private T Get<T>(string name)
			{
				return (T)_props.Get(Attr.Name, name);
			}

			private void Set<T>(string name, T value)
			{
				_props.Set(Attr.Name, name, value);
			}

			public string Address
			{
				get { return Get<string>("Address"); }
			}

			public string Name
			{
				get { return Get<string>("Name"); }
			}

			public string Alias
			{
				get { return Get<string>("Alias"); }
				set { Set("Alias", value); }
			}

			public uint Class
			{
				get { return Get<uint>("Class"); } 
			}

			public bool Powered
			{
				get { return Get<bool>("Powered"); }
				set { Set("Powered", value); }
			}

			public bool Discoverable
			{
				get { return Get<bool>("Discoverable"); }
				set { Set("Discoverable", value); }
			}

			public uint DiscoverableTimeout
			{
				get { return Get<uint>("DiscoverableTimeout"); }
				set { Set("DiscoverableTimeout", value); }
			}

			public bool Pairable
			{
				get { return Get<bool>("Pairable"); }
				set { Set("Pairable", value); }
			}

			public uint PairableTimeout
			{
				get { return Get<uint>("PairableTimeout"); }
				set { Set("PairableTimeout", value); }
			}

			public bool Discovering
			{
				get { return Get<bool>("Discovering"); } 
			}

			public string[] UUIDs
			{
				get { return Get<string[]>("UUIDs"); } 
			}

			public string Modalias
			{
				get { return Get<string>("Modalias"); } 
			}
		}

		public class BluetoothDevice : IDevice
		{
			private static readonly InterfaceAttribute Attr =
				typeof(IDevice).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			private readonly IDevice _dev;
			private readonly Properties _props;
			private readonly Introspectable _info;

			public BluetoothDevice(Bus bus, ObjectPath path)
			{
				_dev = bus.GetObject<IDevice>(BusName, path);
				_props = bus.GetObject<Properties>(BusName, path);
				_info = bus.GetObject<Introspectable>(BusName, path);

				Path = path;
				Services = new ByUuid<BluetoothService>();
			}

			public ByUuid<BluetoothService> Services
			{
				get;
				private set;
			}

			public ObjectPath Path
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
				return PrettyXml(_info.Introspect());
			}

			public void CancelPairing()
			{
				_dev.CancelPairing();
			}

			public void Connect()
			{
				_dev.Connect();
			}

			public void Disconnect()
			{
				_dev.Disconnect();
			}

			public void Pair()
			{
				_dev.Pair();
			}

			private T Get<T>(string name)
			{
				return (T) _props.Get(Attr.Name, name);
			}

			private void Set<T>(string name, T value)
			{
				_props.Set(Attr.Name, name, value);
			}

			public string Address { get { return Get<string>("Address"); } }
			public string Name { get { return Get<string>("Name"); } }
			public string Alias
			{
				get { return Get<string>("Alias"); }
				set { Set("Alias", value); }
			}
			public uint Class { get { return Get<uint>("Class"); } }
			public ushort Appearance { get { return Get<ushort>("Appearance"); } }
			public string Icon { get { return Get<string>("Icon"); } }
			public bool Paired { get { return Get<bool>("Paired"); } }
			public bool Trusted
			{
				get { return Get<bool>("Trusted"); }
				set { Set("Trusted", value); }
			}
			public bool Blocked
			{
				get { return Get<bool>("Blocked"); }
				set { Set("Blocked", value); }
			}
			public bool LegacyPairing { get { return Get<bool>("LegacyPairing"); } }
			public short RSSI { get { return Get<short>("RSSI"); } }
			public bool Connected { get { return Get<bool>("Connected"); } }
			public string[] UUIDs { get { return Get<string[]>("UUIDs"); } }
			public string Modalias { get { return Get<string>("Modalias"); } }
			public ObjectPath Adapter { get { return Get<ObjectPath>("Adapter"); } }
			public Dictionary<ushort,object> ManufacturerData { get { return Get<Dictionary<ushort, object>>("ManufacturerData"); } }
			public Dictionary<string,object> ServiceData { get { return Get<Dictionary<string, object>>("ServiceData"); } }
			public short TxPower { get { return Get<short>("TxPower"); } }
			public bool ServicesResolved { get { return Get<bool>("ServicesResolved"); } }
		}

		public class BluetoothService : IService, IUuid
		{
			private static readonly InterfaceAttribute Attr =
				typeof(IService).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			private readonly Properties _props;
			private readonly Introspectable _info;

			public BluetoothService(Bus bus, ObjectPath path)
			{
				_props = bus.GetObject<Properties>(BusName, path);
				_info = bus.GetObject<Introspectable>(BusName, path);

				Path = path;
				Characteristics = new ByUuid<BluetoothCharacteristic>();
			}

			public ObjectPath Path
			{
				get;
				private set;
			}

			public ByUuid<BluetoothCharacteristic> Characteristics
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
				return PrettyXml(_info.Introspect());
			}

			private T Get<T>(string name)
			{
				return (T)_props.Get(Attr.Name, name);
			}

			public string UUID { get { return Get<string>("UUID"); } }
			public ObjectPath Device { get { return Get<ObjectPath>("Device"); } }
			public bool Primary { get { return Get<bool>("Primary"); } }
			public ObjectPath[] Includes { get { return Get<ObjectPath[]>("Includes"); } }
		}

		public class BluetoothCharacteristic : ICharacteristic, IUuid
		{
			private static readonly InterfaceAttribute Attr =
				typeof(ICharacteristic).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			private readonly ICharacteristic _char;
			private readonly Properties _props;
			private readonly Introspectable _info;

			public BluetoothCharacteristic(Bus bus, ObjectPath path)
			{
				_char = bus.GetObject<ICharacteristic>(BusName, path);
				_props = bus.GetObject<Properties>(BusName, path);
				_info = bus.GetObject<Introspectable>(BusName, path);

				Path = path;
				Descriptors = new ByUuid<BluetoothDescriptor>();
			}

			public ObjectPath Path
			{
				get;
				private set;
			}

			public ByUuid<BluetoothDescriptor> Descriptors
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
				return PrettyXml(_info.Introspect());
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

			private T Get<T>(string name)
			{
				return (T)_props.Get(Attr.Name, name);
			}

			public string UUID { get { return Get<string>("UUID"); } }
			public ObjectPath Service { get { return Get<ObjectPath>("Service"); } }
			public byte[] Value { get { return Get<byte[]>("Vaue"); } }
			public bool Notifying { get { return Get<bool>("Notifying"); } }
			public string[] Flags { get { return Get<string[]>("Flags"); } }
		}

		public class BluetoothDescriptor : IDescriptor, IUuid
		{
			private static readonly InterfaceAttribute Attr =
				typeof(IDescriptor).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

			private readonly IDescriptor _desc;
			private readonly Properties _props;
			private readonly Introspectable _info;

			public BluetoothDescriptor(Bus bus, ObjectPath path)
			{
				_desc = bus.GetObject<IDescriptor>(BusName, path);
				_props = bus.GetObject<Properties>(BusName, path);
				_info = bus.GetObject<Introspectable>(BusName, path);

				Path = path;
			}

			public ObjectPath Path
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
				return PrettyXml(_info.Introspect());
			}

			public byte[] ReadValue(Dictionary<string, object> options)
			{
				return _desc.ReadValue(options);
			}

			public void WriteValue(byte[] value, Dictionary<string, object> options)
			{
				_desc.WriteValue(value, options);
			}

			private T Get<T>(string name)
			{
				return (T)_props.Get(Attr.Name, name);
			}

			public string UUID { get { return Get<string>("UUID"); } }
			public ObjectPath Characteristic { get { return Get<ObjectPath>("Characteristic"); } }
			public byte[] Value { get { return Get<byte[]>("Vaue"); } }
		}

		private const string BusName = "org.bluez";
		private readonly object _mutex = new object();

		private readonly Bus _bus;
		private readonly ObjectManager _mgr;

		public BluetoothPub()
		{
			_bus = Bus.Open(Address.System);

			_mgr = _bus.GetObject<ObjectManager>(BusName, ObjectPath.Root);

			_mgr.InterfacesAdded += InterfacesAdded;
			_mgr.IntefacesRemoved += IntefacesRemoved;
		}

		public string Adapter { get; set; }
		public string Device { get; set; }

		public int DiscoverTimeout { get; set; } = 10000;
		public int ConnectTimeout { get; set; } = 10000;
		public int PairTimeout { get; set; } = 10000;

		private void IntefacesRemoved(ObjectPath path, string[] interfaces)
		{
			lock (_mutex)
			{
				Console.WriteLine("IntefacesRemoved> {0}", path);
				Monitor.Pulse(_mutex);
			}
		}

		private void InterfacesAdded(ObjectPath path, IDictionary<string, IDictionary<string, object>> interfaces)
		{
			lock (_mutex)
			{
				Console.WriteLine("InterfacesAdded> {0}", path);
				Monitor.Pulse(_mutex);
			}
		}

		public void Open()
		{
			var adapterPath = FindAdapter();

			Console.WriteLine("Found Adapter: {0}", adapterPath);

			var adapter = new BluetoothAdapter(_bus, adapterPath);

			foreach (var kv in adapter.Properties)
			{
				Console.WriteLine("  {0}={1}", kv.Key, kv.Value);
			}

			var devicePath = FindDevice(adapterPath);

			if (devicePath == null)
			{
				Console.WriteLine("Discovering...");

				lock (_mutex)
				{
					adapter.StartDiscovery();

					var sw = Stopwatch.StartNew();
					var remain = DiscoverTimeout;

					while (remain >= 0)
					{
						if (!Monitor.Wait(_mutex, remain))
							break;

						devicePath = FindDevice(adapter.Path);
						if (devicePath != null)
							break;

						remain = DiscoverTimeout - (int)sw.ElapsedMilliseconds;
					}

					adapter.StopDiscovery();
				}

				if (devicePath == null)
					throw new ApplicationException(string.Format("Couldn't locate device {0}", Device));
			}

			Console.WriteLine("Found Device: {0}", devicePath);

			var dev = new BluetoothDevice(_bus, devicePath);

			{
				var sw = Stopwatch.StartNew();
				var remain = ConnectTimeout;

				if (!dev.Connected)
				{
					Console.WriteLine("Connecting...");
					dev.Connect();

					while (remain >= 0)
					{
						Thread.Sleep(Math.Min(remain, 100));

						if (dev.Connected)
							break;

						remain = ConnectTimeout - (int) sw.ElapsedMilliseconds;
					}

					if (!dev.Connected)
						throw new ApplicationException(string.Format("Timed out connecting to device {0}", Device));
				}

				if (!dev.ServicesResolved)
				{
					while (remain >= 0)
					{
						Thread.Sleep(Math.Min(remain, 100));

						if (dev.ServicesResolved)
							break;

						remain = ConnectTimeout - (int)sw.ElapsedMilliseconds;
					}

					if (!dev.ServicesResolved)
						throw new ApplicationException(string.Format("Timed out resolving services for device {0}", Device));
				}
			}

			if (!dev.Paired)
			{
				Console.WriteLine("Pairing...");

				var sw = Stopwatch.StartNew();
				var remain = PairTimeout;

				dev.Pair();

				while (remain >= 0)
				{
					Thread.Sleep(Math.Min(remain, 100));

					if (dev.Paired)
						break;

					remain = PairTimeout - (int)sw.ElapsedMilliseconds;
				}

				if (!dev.Paired)
				{
					dev.CancelPairing();
					throw new ApplicationException(string.Format("Timed out pairing to device {0}", Device));
				}
			}

			foreach (var kv in dev.Properties)
			{
				Console.WriteLine("  {0}={1}", kv.Key, kv.Value);
			}

			Console.WriteLine("Discovered Services:");

			foreach (var svc in EnumerateServices(dev.Path))
			{
				dev.Services.Add(svc);

				Console.WriteLine("  Service: {0}, Primary: {1}", svc.UUID, svc.Primary);

				foreach (var c in svc.Characteristics)
				{
					var v = c.Flags.Contains("read")
						? ", Value: " + string.Join("", c.ReadValue(new Dictionary<string, object>()).Select(x => x.ToString("X2")))
						: "";

					Console.WriteLine("    Char: {0}, Flags: {1}{2}", c.UUID, string.Join("|", c.Flags), v);

					foreach (var d in c.Descriptors)
					{
						var v2 = c.Flags.Contains("read")
							? ", Value: " + string.Join("", d.ReadValue(new Dictionary<string, object>()).Select(x => x.ToString("X2")))
							: "";

						Console.WriteLine("     Descriptor: {0}{1}", d.UUID, v2);
					}
				}
			}

		}

		private IEnumerable<BluetoothService> EnumerateServices(ObjectPath devicePath)
		{
			var objs = _mgr.GetManagedObjects();

			foreach (var svcPath in objs.Iter<IService>(p => devicePath.Equals(p["Device"])))
			{
				var svc = new BluetoothService(_bus, svcPath);

				foreach (var chrPath in objs.Iter<ICharacteristic>(p => svcPath.Equals(p["Service"])))
				{
					var chr = new BluetoothCharacteristic(_bus, chrPath);

					foreach (var dscPath in objs.Iter<IDescriptor>(p => chrPath.Equals(p["Characteristic"])))
					{
						chr.Descriptors.Add(new BluetoothDescriptor(_bus, dscPath));
					}

					svc.Characteristics.Add(chr);
				}

				yield return svc;
			}
		}

		private ObjectPath FindAdapter()
		{
			var iface = typeof(IAdapter).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();
			var objs = _mgr.GetManagedObjects();

			ObjectPath ret = null;

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					//Console.WriteLine("FindAdapter: {0} {1}", kv.Key, item.Key);

					//foreach (var props in item.Value)
					//	Console.WriteLine("  {0}={1}", props.Key, props.Value);

					if (AddressMatch(item.Value["Address"]) || IfaceMatch(kv.Key))
						ret = kv.Key;
				}
			}

			return ret;
		}

		private ObjectPath FindDevice(ObjectPath adapter)
		{
			var iface = typeof(IDevice).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();
			var objs = _mgr.GetManagedObjects();

			ObjectPath ret = null;

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					//Console.WriteLine("FindDevice: {0} {1}", kv.Key, item.Key);

					//foreach (var props in item.Value)
					//	Console.WriteLine("  {0}={1}", props.Key, props.Value);

					if (DeviceMatch(item.Value["Address"]) && item.Value["Adapter"].ToString() == adapter.ToString())
						ret = kv.Key;
				}
			}

			return ret;
		}

		private bool IfaceMatch(ObjectPath path)
		{
			var asStr = path.ToString();
			var lastSeg = asStr.Substring(asStr.LastIndexOf('/') + 1);
			return 0 == string.Compare(lastSeg, Adapter, StringComparison.OrdinalIgnoreCase);
		}

		private bool DeviceMatch(object addr)
		{
			return 0 == string.Compare(addr.ToString(), Device, StringComparison.OrdinalIgnoreCase);
		}

		private bool AddressMatch(object addr)
		{
			return 0 == string.Compare(addr.ToString(), Adapter, StringComparison.OrdinalIgnoreCase);
		}

		public void Introspect(ObjectPath path)
		{
			var str = _bus.GetObject<Introspectable>(BusName, path).Introspect();
			Console.WriteLine(PrettyXml(str));
		}

		public void Dispose()
		{
			_bus.Close();
		}

		public static string PrettyXml(string xml)
		{
			var stream = new StringWriter();
			var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
			var doc = new XmlDocument();

			doc.LoadXml(xml);
			doc.Save(writer);

			return stream.ToString();
		}
	}

	public class Program
	{
		static int Main(string[] args)
		{
			using (var mgr = new BluetoothPub())
			{
				mgr.Adapter = args[0];
				mgr.Device = args[1];

				mgr.Open();
			}

			return 0;
		}
	}
}
