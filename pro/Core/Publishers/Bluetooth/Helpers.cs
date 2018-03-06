using System.Collections.ObjectModel;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public interface IUuid
	{
		string UUID { get; }
	}

	public interface IGattProperties : Properties
	{
		string InterfaceName { get; }
	}

	public class ByUuid<T> : KeyedCollection<string, T> where T : IUuid
	{
		protected override string GetKeyForItem(T item)
		{
			return item.UUID;
		}
	}
}
