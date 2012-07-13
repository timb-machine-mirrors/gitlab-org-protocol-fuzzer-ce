﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace Peach.Core.Publishers
{
	[Publisher("Udp", true)]
	[ParameterAttribute("Host", typeof(string), "Hostname or IP address of remote host", true)]
	[ParameterAttribute("Port", typeof(int), "Destination port #", true)]
	[ParameterAttribute("Timeout", typeof(int), "How long to wait for data/connection (default 3 seconds)", false)]
	public class UdpPublisher : Publisher
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		protected string _host = null;
		protected int _port = 0;
		protected int _timeout = 3 * 1000;
		protected UdpClient _udp = null;
		protected MemoryStream _buffer = new MemoryStream();
		protected int _errors_send = 0;
		protected int _errors_recv = 0;
		protected static int MaxErrors = 10;

		public UdpPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_host = (string)args["Host"];
			_port = (int)args["Port"];

			if (args.ContainsKey("Timeout"))
				_timeout = (int)args["Timeout"];

			Dom.Action.Starting += new Dom.ActionStartingEventHandler(Action_Starting);
		}

		protected override void Dispose(bool disposing)
		{
			Dom.Action.Starting -= new Dom.ActionStartingEventHandler(Action_Starting);
			base.Dispose(disposing);
		}

		void Action_Starting(Dom.Action action)
		{
			if (action.type != Dom.ActionType.Input)
				return;

			OnInput(action);

			if (_udp == null)
				OpenSocket();

			try
			{
				IPEndPoint remoteEP = null;
				var asyncResult = _udp.BeginReceive(null, null);
				if (!asyncResult.AsyncWaitHandle.WaitOne(_timeout))
					throw new TimeoutException();
				byte[] buf = _udp.EndReceive(asyncResult, ref remoteEP);
				_buffer = new MemoryStream(buf);
				_errors_recv = 0;
			}
			catch (Exception ex)
			{
				if (ex is TimeoutException)
				{
					logger.Debug("UDP packet not received from {0}:{1} in {2}ms, timing out.",
						_host, _port, _timeout);
				}
				else
				{
					logger.Error("Unable to receive UDP packet from {0}:{1}. {2}",
						_host, _port, ex.Message);
				}

				if (_errors_recv++ == (MaxErrors-1))
					throw new PeachException("Failed to receive UDP packet after " + _errors_recv + " attempts.");

				throw new SoftException();
			}
		}

		protected void OpenSocket()
		{
			_udp = new UdpClient(_host, _port);
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <param name="action">Action calling publisher</param>
		/// <param name="data">Data to send/write</param>
		public override void output(Core.Dom.Action action, Variant data)
		{
			OnOutput(action, data);

			if (_udp == null)
				OpenSocket();

			try
			{
				byte[] buf = (byte[])data;
				var asyncResult = _udp.BeginSend(buf, buf.Length, null, null);
				if (!asyncResult.AsyncWaitHandle.WaitOne(_timeout))
					throw new TimeoutException();
				_udp.EndSend(asyncResult);
				_errors_send = 0;
			}
			catch (Exception ex)
			{
				if (ex is TimeoutException)
				{
					logger.Debug("UDP packet not sent to {0}:{1} in {2}ms, timing out.",
						_host, _port, _timeout);
				}
				else
				{
					logger.Error("Unable to send UDP packet to {0}:{1}. {2}",
						_host, _port, ex.Message);
				}

				if (_errors_send++ == MaxErrors)
					throw new PeachException("Failed to send UDP packet after " + _errors_send + " attempts.");

				throw new SoftException();
			}
		}

		/// <summary>
		/// Close a resource.  Will be called automatically when
		/// state model exists.  Can also be called explicitly when
		/// needed.
		/// </summary>
		/// <param name="action">Action calling publisher</param>
		public override void close(Core.Dom.Action action)
		{
			OnClose(action);

			if (_udp != null)
			{
				_udp.Close();
				_udp = null;
			}
		}

		public override long Length
		{
			get
			{
				return _buffer.Length;
			}
		}

		public override long Position
		{
			get
			{
				return _buffer.Position;
			}
			set
			{
				_buffer.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			OnInput(currentAction);

			return _buffer.Read(buffer, offset, count);
		}
	}
}
