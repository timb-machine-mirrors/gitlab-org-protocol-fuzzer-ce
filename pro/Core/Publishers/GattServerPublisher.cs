using System;
using System.Collections.Generic;
using System.Threading;
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

		private readonly object _mutex;
		private Thread _thread;
		private Manager _mgr;
		private bool _lastWasError;

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
			_mutex = new object();
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

			_thread = new Thread(IterateThread);
			_thread.Start();
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			// The state model doesn't require publishers to be open when
			// performing call actions, but we want to track errors on a
			// per iteration basis so ensure we are opened here

			open();

			throw new PeachException("Error, method '{0}' not supported by GattServer publisher".Fmt(method));
		}

		protected override void OnClose()
		{
			// If there was an error on the iteration
			// close everything up and try again

			if (_lastWasError)
			{
				lock (_mutex)
				{
					_mgr.Dispose();
					_mgr = null;
				}

				_thread.Join();
				_thread = null;
				_lastWasError = false;
			}
		}

		protected override void OnStop()
		{
			if (_mgr != null)
			{
				lock (_mutex)
				{
					_mgr.Dispose();
					_mgr = null;
				}

				_thread.Join();
				_thread = null;
			}
		}

		private void IterateThread()
		{
			Logger.Trace("IterateThread> Begin");

			try
			{
				while (true)
				{
					Manager mgr;

					lock (_mutex)
					{
						mgr = _mgr;
					}

					if (mgr == null)
						break;

					mgr.Iterate();
				}
			}
			catch (Exception ex)
			{
				Logger.Trace(ex);
			}

			Logger.Trace("IterateThread> End");
		}
	}
}
