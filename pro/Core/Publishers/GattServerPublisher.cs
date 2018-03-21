using System;
using System.Collections.Generic;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.Publishers.Bluetooth;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("GattServer")]
	[Parameter("Adapter", typeof(string), "Local adapter name (eg: hci0)")]
	[Parameter("ConnectTimeout", typeof(int), "How long to wait when connecting", "10000")]
	[Parameter("PairTimeout", typeof(int), "How long to wait when pairing", "10000")]
	public class GattServerPublisher : Publisher
	{
		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		private Manager _mgr;

		protected override NLog.Logger Logger
		{
			get { return ClassLogger; }
		}

		public string Adapter { get; set; }
		public int ConnectTimeout { get; set; }
		public int PairTimeout { get; set; }

		public GattServerPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			if (_mgr != null)
				return;

			var mgr = new Manager
			{
				Adapter = Adapter,
				ConnectTimeout = ConnectTimeout,
				PairTimeout = PairTimeout
			};

			try
			{
				mgr.Open();
			}
			catch (Exception ex)
			{
				mgr.Dispose();
				throw new SoftException(ex);
			}

			_mgr = mgr;
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			throw new PeachException("Error, method '{0}' not supported by GattServer publisher".Fmt(method));
		}

		protected override void OnStop()
		{
			if (_mgr != null)
			{
				_mgr.Dispose();
				_mgr = null;
			}
		}
	}
}
