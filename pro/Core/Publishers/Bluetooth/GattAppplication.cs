using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class GattApplication : ObjectManager
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static string PropertyIface = typeof(org.freedesktop.DBus.Properties)
			.GetCustomAttributes(false)
			.OfType<InterfaceAttribute>()
			.Select(x => x.Name)
			.First();

		public GattApplication()
		{
			Path = new ObjectPath("/com/peach");
			Services = new List<LocalService>();
			Advertisement = new LocalAdvertisement();
		}

		public ObjectPath Path { get; set; }

		public List<LocalService> Services { get; private set; }

		public LocalAdvertisement Advertisement { get; private set; }

		public string Introspect()
		{
			throw new NotImplementedException();
		}

		public IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> GetManagedObjects()
		{
			Logger.Trace("GetManagedObjects>");

			var ret = new Dictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>();

			foreach (var svc in Services)
			{
				ret.Add(svc.Path, GetProps(svc));

				foreach (var chr in svc.Characteristics)
				{
					ret.Add(chr.Path, GetProps(chr));

					foreach (var dsc in chr.Descriptors)
					{
						ret.Add(dsc.Path, GetProps(dsc));
					}
				}
			}

			foreach (var kv1 in ret)
			{
				Logger.Trace("{0}", kv1.Key);

				foreach (var kv2 in kv1.Value)
				{
					Logger.Trace("  {0}", kv2.Key);
					foreach (var kv3 in kv2.Value)
					{
						Logger.Trace("    {0}={1}", kv3.Key, kv3.Value);
					}
				}
			}

			return ret;
		}

		public event InterfacesAddedDelegate InterfacesAdded
		{
			add { }
			remove { }
		}

		public event InterfacesRemovedDelegate IntefacesRemoved
		{
			add { }
			remove { }
		}

		private IDictionary<string, IDictionary<string, object>> GetProps(IGattProperties obj)
		{
			var ifaceNames = new[] { PropertyIface, obj.InterfaceName };
			return ifaceNames.ToDictionary(propName => propName, obj.GetAll);
		}
	}

}
