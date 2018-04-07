using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
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
	public interface IService : IUuid
	{
		bool Primary { get; }
	}

	[Interface("org.bluez.GattCharacteristic1")]
	public interface ICharacteristic : IUuid
	{
		byte[] ReadValue(Dictionary<string, object> options);
		void WriteValue(byte[] value, Dictionary<string, object> options);
		void StartNotify();
		void StopNotify();

		void AcquireNotify(Dictionary<string, object> options, out int fd, out ushort mtu);

		ObjectPath Service { get; }
		string[] Flags { get; }
	}

	[Interface("org.bluez.GattDescriptor1")]
	public interface IDescriptor : IUuid
	{
		byte[] ReadValue(Dictionary<string, object> options);
		void WriteValue(byte[] value, Dictionary<string, object> options);

		ObjectPath Characteristic { get; }
		string[] Flags { get; }
	}

	[Interface("org.bluez.LEAdvertisement1")]
	public interface IAdvertisement
	{
		void Release();

		string Type { get; }
		string LocalName { get; }
		string[] ServiceUUIDs { get; }
		Dictionary<ushort, object> ManufacturerData { get; }
		string[] SolicitUUIDs { get; }
		Dictionary<string, object> ServiceData { get; }
		bool IncludeTxPower { get; }
	}

	[Interface("org.bluez.GattManager1")]
	public interface IGattManager
	{
		void RegisterApplication(ObjectPath application, Dictionary<string, object> options);
		void UnregisterApplication(ObjectPath application);
	}

	[Interface("org.bluez.LEAdvertisingManager1")]
	public interface IAdvertisingManager
	{
		void RegisterAdvertisement(ObjectPath advertisement, Dictionary<string, object> options);
		void UnregisterAdvertisement(ObjectPath advertisement);
	}
}
