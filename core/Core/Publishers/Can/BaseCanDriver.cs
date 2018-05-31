using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Peach.Core.Publishers.Can
{
	/// <summary>
	/// Base class for CAN Drivers.  Provides some skafolding for
	/// patterns same across all drivers.
	/// </summary>
	public abstract class BaseCanDriver : ICanDriver, ICanInterface
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Store registered event handlers with a CAN ID filter
		/// </summary>
		private readonly ConcurrentDictionary<uint, HashSet<CanRxEventHandler>> _canFrameReceivedHandlers = new ConcurrentDictionary<uint, HashSet<CanRxEventHandler>>();

		private readonly BlockingCollection<CanFrame> _notifyQueue = new BlockingCollection<CanFrame>(new ConcurrentQueue<CanFrame>());
		private readonly ConcurrentQueue<CanFrame> _rxFrameQueue = new ConcurrentQueue<CanFrame>();
		private readonly ConcurrentQueue<Tuple<DateTime, string, Exception>> _rxLogQueue = new ConcurrentQueue<Tuple<DateTime,string, Exception>>();

		private int _openCount = 0;
		private Thread _notifyThread;
		private readonly Object _lock = new Object();

		#region ICanDriver

		public abstract string Name { get; }
		public abstract IEnumerable<ParameterAttribute> Parameters { get; }
		public abstract ICanInterface CreateInterface(Dictionary<string, Variant> args);

		#endregion

		#region ICanInterface

		public abstract ICanDriver Driver { get; }
		public abstract IEnumerable<ICanChannel> Channels { get; }
		public abstract bool IsOpen { get; protected set; }

		public void Open()
		{
			lock (_lock)
			{
				if (_notifyThread == null)
				{
					_notifyThread = new Thread(() =>
					{
						HashSet<CanRxEventHandler> handlers;

						while (true)
						{
							var msg = _notifyQueue.Take();

							if (!_canFrameReceivedHandlers.TryGetValue(msg.Identifier, out handlers))
								continue;

							lock (handlers)
							{
								handlers.ForEach(x => x(this, msg));
							}
						}
					});

					_notifyThread.Start();
				}

				if (_openCount < 0)
					_openCount = 0;

				if (_openCount == 0)
				{
					Logger.Trace("Opening can driver, open count is zero");
					OpenImpl();
				}
				else
				{
					Logger.Trace("Not opening can driver, open count is {0}", _openCount);
				}

				_openCount++;
			}
		}

		/// <summary>
		/// Children must implement this method instead of Open()
		/// </summary>
		protected abstract void OpenImpl();

		public void Close()
		{
			lock (_lock)
			{
				_openCount--;

				if (_openCount == 0)
				{
					Logger.Trace("Closing can driver, open count reached zero");
					CloseImpl();

					_notifyThread.Abort();
					_notifyThread = null;

					_canFrameReceivedHandlers.Clear();
				}
				else if (_openCount < 0)
				{
					Logger.Warn("Close called more than open.  Count is '{0}'.", _openCount);
				}
				else
				{
					Logger.Trace("Not closing can driver, open count is {0}", _openCount);
				}
			}
		}

		/// <summary>
		/// Children must implement this method instead of Close()
		/// </summary>
		protected abstract void CloseImpl();

		public CanFrame ReadMessage()
		{
			if (!IsOpen)
				throw new ApplicationException("Error, CAN interface not open");

			CanFrame msg;
			return _rxFrameQueue.TryDequeue(out msg) ? msg : null;
		}

		public abstract void WriteMessage(ICanChannel txChannel, CanFrame frame);

		public Tuple<DateTime, string, Exception> GetLogMessage()
		{
			Tuple<DateTime, string, Exception> log;

			if (_rxLogQueue.TryDequeue(out log))
				return log;

			return null;
		}

		#endregion

		/// <summary>
		/// Add a log message to the log message queue
		/// </summary>
		/// <param name="when">When message occured</param>
		/// <param name="msg">Log message</param>
		/// <param name="e">Optional exception</param>
		protected void AddLogMessage(DateTime when, string msg, Exception e = null)
		{
			_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(when, msg, e));
		}

		/// <summary>
		/// Notify any registered handlers for this specific CAN ID.
		/// Notifications occur in a Task to not block
		/// </summary>
		/// <param name="msg"></param>
		protected void NotifyCanFrameReceivedHandlers(CanFrame msg)
		{
			_notifyQueue.Add(msg);
			_rxFrameQueue.Enqueue(msg);
		}

		/// <inheritdoc />
		public void RegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler)
		{
			HashSet<CanRxEventHandler> handlers;
			lock (_canFrameReceivedHandlers)
			{
				if (!_canFrameReceivedHandlers.ContainsKey(id))
				{
					handlers = new HashSet<CanRxEventHandler>();
					_canFrameReceivedHandlers[id] = handlers;
				}
				else
				{
					handlers = _canFrameReceivedHandlers[id];
				}

				lock (handlers)
				{
					handlers.Add(handler);
				}
			}
		}

		/// <inheritdoc />
		public void RegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler)
		{
			foreach (var id in ids)
			{
				RegisterCanFrameReceiveHandler(id, handler);
			}
		}

		/// <inheritdoc />
		public void UnRegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler)
		{
			lock (_canFrameReceivedHandlers)
			{
				if (!_canFrameReceivedHandlers.ContainsKey(id))
					return;

				_canFrameReceivedHandlers[id].Remove(handler);
			}
		}

		/// <inheritdoc />
		public void UnRegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler)
		{
			foreach (var id in ids)
			{
				UnRegisterCanFrameReceiveHandler(id, handler);
			}
		}

		/// <summary>
		/// Convert unix timestamp to DateTime
		/// </summary>
		/// <param name="unixTimeStamp"></param>
		/// <returns></returns>
		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp*0.000001).ToLocalTime();
			return dtDateTime;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
