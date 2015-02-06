using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.Agent.Channels.Rest;

namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	class RestTests
	{
		/*
		 * 1) Ensure proper cleanup happens if StartMonitor & SessionStarting throw!
		 * 2) Ensure IterationStarting is always called when previous iteration had a fault!
		 *    This is needed to clean up /pa/file/{id} resources from GetMonitorData.
		 */

		private void StartServer()
		{
			_event = new AutoResetEvent(false);

			_server = new Server();

			_server.Started += (s, e) => _event.Set();

			_thread = new Thread(() =>
			{
				try
				{
					_server.Run(10000, 10100);
				}
				catch (Exception ex)
				{
					_error = ex;
					_event.Set();
				}
			});

			_thread.Start();
			_event.WaitOne();

			// Trigger faulire if we couldn't start
			if (_error != null)
				TearDown();

			Assert.NotNull(_server.Uri);
		}

		private Exception _error;
		private AutoResetEvent _event;
		private Thread _thread;
		private Server _server;

		[TearDown]
		public void TearDown()
		{
			if (_server != null)
			{
				_server.Stop();
				_server = null;
			}

			if (_thread != null)
			{
				_thread.Join();
				_thread = null;
			}

			if (_event != null)
			{
				_event.Dispose();
				_event = null;
			}

			var err = _error;
			_error = null;

			if (err != null)
				throw new PeachException(err.Message, err);
		}


		[Test]
		public void ServerRuns()
		{
			StartServer();

			Assert.NotNull(_server);
			Assert.NotNull(_server.Uri);
		}

		[Test]
		public void ClientConnect()
		{
			StartServer();

			var cli = new Client(null, _server.Uri.ToString(), null);

			cli.AgentConnect();
			cli.StartMonitor("mon", "TcpPort", new Dictionary<string, string>
			{
				{"Host", "localhost" },
				{"Port", "1" },
				{"WaitOnCall", "MyWaitMessage" },
				{"When", "OnCall" },
			});
			cli.SessionStarting();
			cli.SessionFinished();
			cli.StopAllMonitors();
			cli.AgentDisconnect();
		}

		[Test]
		public void GetMonitorData()
		{
			StartServer();

			var tmp = Path.GetTempFileName();
			var name = Path.GetFileName(tmp);

			Assert.NotNull(name);

			File.WriteAllText(tmp, "Hello World");

			try
			{
				var cli = new Client("cli", _server.Uri.ToString(), null);

				cli.AgentConnect();
				cli.StartMonitor("mon", "SaveFile", new Dictionary<string, string>
				{
					{"Filename", tmp },
				});
				cli.SessionStarting();

				var f = cli.GetMonitorData().ToList();

				Assert.AreEqual(1, f.Count);
				Assert.AreEqual("SaveFile", f[0].DetectionSource);
				Assert.AreEqual("mon", f[0].MonitorName);
				Assert.AreEqual("cli", f[0].AgentName);
				Assert.Null(f[0].Fault);
				Assert.NotNull(f[0].Data);
				Assert.True(f[0].Data.ContainsKey(name));
				Assert.AreEqual("Hello World", Encoding.UTF8.GetString(f[0].Data[name]));

				cli.SessionFinished();
				cli.StopAllMonitors();
				cli.AgentDisconnect();
			}
			finally
			{
				File.Delete(tmp);
			}
		}
	}
}
