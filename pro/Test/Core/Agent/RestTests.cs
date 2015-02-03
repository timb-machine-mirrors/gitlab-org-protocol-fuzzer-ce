using System;
using System.Collections.Generic;
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
		[Test]
		public void ServerRuns()
		{
			Uri uri = null;
			Exception error = null;

			var evt = new AutoResetEvent(false);
			var server = new Server();

			server.Started += (s, e) =>
			{
				uri = server.Uri;
				evt.Set();
			};

			var th = new Thread(() =>
			{
				try
				{
					server.Run(10000, 10100);
				}
				catch (Exception ex)
				{
					error = ex;
					evt.Set();
				}
			});

			th.Start();

			evt.WaitOne();
			evt.Dispose();
			server.Stop();
			th.Join();

			if (error != null)
				throw new PeachException(error.Message, error);

			Assert.NotNull(uri);
		}

		[Test]
		public void ClientConnect()
		{
			Uri uri = null;
			Exception error = null;

			var evt = new AutoResetEvent(false);
			var server = new Server();

			server.Started += (s, e) =>
			{
				uri = server.Uri;
				evt.Set();
			};

			var th = new Thread(() =>
			{
				try
				{
					server.Run(10000, 10100);
				}
				catch (Exception ex)
				{
					error = ex;
					evt.Set();
				}
			});

			th.Start();

			evt.WaitOne();

			if (error != null)
			{
				server.Stop();
				th.Join();
				evt.Dispose();
				throw new PeachException(error.Message, error);
			}

			Assert.NotNull(uri);

			try
			{
				var cli = new Client(null, uri.ToString(), null);

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
			finally
			{
				server.Stop();
				th.Join();
				evt.Dispose();
			}
		}
	}
}
