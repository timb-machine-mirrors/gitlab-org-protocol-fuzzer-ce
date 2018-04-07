using System;
using NDesk.DBus;
using Peach.Core;
using Peach.Pro.Core.Publishers.Bluetooth;

namespace Peach.Pro.Test.Bluetooth
{
	public class Program
	{
		static int Main(string[] args)
		{
			if (args.Length < 1 || args.Length > 2)
				Console.WriteLine("Usage:Bluetooth.exe <hci0> [remote_address]");

			Utilities.ConfigureLogging(2);

			var app = new GattApplication
			{
				Path = new ObjectPath("/com/peach/example"),
				Services =
				{
					new LocalService
					{
						UUID = "180a", // DeviceInformation
						Primary = true,
						Characteristics =
						{
							new LocalCharacteristic
							{
								UUID = "2A29", // Manufacturer Name String
								Descriptors = {},
								Flags = new[] { "read" },
								Read = (c,o) => Encoding.ASCII.GetBytes("Hello World")
							}
						}
					}
				}
			};

			using (var mgr = new Manager())
			{
				mgr.Dump();

				mgr.Adapter = args[0];
				mgr.Open();

				if (args.Length == 2)
				{
					mgr.Device = args[1];
					mgr.Connect(false, false);

					Console.WriteLine("Connected, press any key to continue");
				}
				else
				{
					mgr.Serve(app);
					Console.WriteLine("Registered, press any key to continue");
				}
			}

			Console.ReadLine();

			return 0;
		}
	}
}
