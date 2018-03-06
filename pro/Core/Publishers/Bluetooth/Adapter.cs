using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class Adapter : IAdapter, IGattManager, IAdvertisingManager
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(IAdapter).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly IAdapter _adapter;
		private readonly IAdvertisingManager _advertMgr;
		private readonly IGattManager _mgr;
		private readonly Properties _props;
		private readonly Introspectable _info;

		public Adapter(Bus bus, ObjectPath path)
		{
			_adapter = bus.GetObject<IAdapter>(path);
			_advertMgr = bus.GetObject<IAdvertisingManager>(path);
			_mgr = bus.GetObject<IGattManager>(path);
			_props = bus.GetObject<Properties>(path);
			_info = bus.GetObject<Introspectable>(path);

			_props.PropertiesChangedEvent += OnPropertiesChanged;

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
			return _info.IntrospectPretty();
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

		public void RegisterApplication(ObjectPath application, Dictionary<string, object> options)
		{
			_mgr.RegisterApplication(application, options);
		}

		public void UnregisterApplication(ObjectPath application)
		{
			_mgr.UnregisterApplication(application);
		}

		public void RegisterAdvertisement(ObjectPath application, Dictionary<string, object> options)
		{
			_advertMgr.RegisterAdvertisement(application, options);
		}

		public void UnregisterAdvertisement(ObjectPath application)
		{
			_advertMgr.UnregisterAdvertisement(application);
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
}
