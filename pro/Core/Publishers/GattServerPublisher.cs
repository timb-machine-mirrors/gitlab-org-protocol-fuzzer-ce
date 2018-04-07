using System.Collections.Generic;
using NLog;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("GattServer")]
	[Parameter("ConnectTimeout", typeof(int), "Max seconds to wait for adb connection (default 5)", "5")]
	[Parameter("CommandTimeout", typeof(int), "Max seconds to wait for adb command to complete (default 10)", "10")]
	public class GattServerPublisher : Publisher
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		public GattServerPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}
	}
}
