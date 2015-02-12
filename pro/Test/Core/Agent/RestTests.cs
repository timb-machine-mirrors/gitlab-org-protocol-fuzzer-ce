using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.IO;
using Peach.Core.Test;
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

		static string ToJsonString(Variant v)
		{
			// Encode a variant as a json message.
			// Use RouteResponse.AsJson so we run the same code as the agent client.
			var model = v.ToModel<CallResponse>();
			var response = RouteResponse.AsJson(model);
			var rdr = new StreamReader(response.Content);
			var str = rdr.ReadToEnd();

			return str;
		}

		static Variant FromJsonString(string s)
		{
			var sr = new StringReader(s);
			var model = sr.JsonDecode<CallResponse>();
			var ret = model.ToVariant();
			return ret;
		}

		[Test]
		public void TestVariants()
		{
			var s = ToJsonString(new Variant(100));
			Assert.AreEqual("{\"type\":\"integer\",\"value\":100}", s);
			var v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.Int, v.GetVariantType());
			Assert.AreEqual(100, (int)v);

			s = ToJsonString(new Variant(long.MinValue));
			Assert.AreEqual("{\"type\":\"integer\",\"value\":-9223372036854775808}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.Long, v.GetVariantType());
			Assert.AreEqual(long.MinValue, (long)v);

			s = ToJsonString(new Variant(long.MaxValue));
			Assert.AreEqual("{\"type\":\"integer\",\"value\":9223372036854775807}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.Long, v.GetVariantType());
			Assert.AreEqual(long.MaxValue, (long)v);

			s = ToJsonString(new Variant(ulong.MaxValue));
			Assert.AreEqual("{\"type\":\"integer\",\"value\":18446744073709551615}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.ULong, v.GetVariantType());
			Assert.AreEqual(ulong.MaxValue, (ulong)v);

			s = ToJsonString(new Variant(1.1));
			Assert.AreEqual("{\"type\":\"double\",\"value\":1.1}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.Double, v.GetVariantType());
			Assert.AreEqual(1.1, (double)v);

			s = ToJsonString(new Variant("hello"));
			Assert.AreEqual("{\"type\":\"string\",\"value\":\"hello\"}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.String, v.GetVariantType());
			Assert.AreEqual("hello", (string)v);

			s = ToJsonString(new Variant(Encoding.UTF8.GetBytes("byte array")));
			Assert.AreEqual("{\"type\":\"bytes\",\"value\":\"Ynl0ZSBhcnJheQ==\"}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.BitStream, v.GetVariantType());
			Assert.AreEqual(Encoding.ASCII.GetBytes("byte array"), ((BitwiseStream)v).ToArray());

			s = ToJsonString(new Variant(new BitStream(Encoding.UTF8.GetBytes("bitstream"))));
			Assert.AreEqual("{\"type\":\"bytes\",\"value\":\"Yml0c3RyZWFt\"}", s);
			v = FromJsonString(s);
			Assert.AreEqual(Variant.VariantType.BitStream, v.GetVariantType());
			Assert.AreEqual(Encoding.ASCII.GetBytes("bitstream"), ((BitwiseStream)v).ToArray());

			s = ToJsonString(null);
			Assert.AreEqual("null", s);
			v = FromJsonString(s);
			Assert.Null(v);

			v = FromJsonString("");
			Assert.Null(v);
		}

		[Test]
		public void CreatePublisher()
		{
			StartServer();

			var cli = new Client(null, _server.Uri.ToString(), null);

			cli.AgentConnect();

			var pub = cli.CreatePublisher("pub", "Null", new Dictionary<string, string>());

			try
			{
				pub.Open(100, false);
				pub.Close();
			}
			finally
			{
				pub.Dispose();
			}

			cli.AgentDisconnect();
		}

		[Test]
		public void PublisherWantBytes()
		{
			Func<Stream, string> asStr = strm => Encoding.ASCII.GetString(((MemoryStream)strm).ToArray());

			StartServer();

			var tmp = Path.GetTempFileName();

			try
			{
				File.WriteAllText(tmp, "Hello World");

				var cli = new Client(null, _server.Uri.ToString(), null);

				cli.AgentConnect();

				var pub = cli.CreatePublisher("pub", "File", new Dictionary<string, string>
				{
					{ "FileName", tmp },
					{ "Overwrite", "false" },
				});

				try
				{
					pub.Open(100, false);

					Assert.NotNull(pub.InputStream);
					Assert.AreEqual(0, pub.InputStream.Length);
					Assert.AreEqual(0, pub.InputStream.Position);

					// Input will consume all available bytes
					pub.Input();

					Assert.AreEqual(11, pub.InputStream.Length);
					Assert.AreEqual(0, pub.InputStream.Position);
					Assert.AreEqual("Hello World", asStr(pub.InputStream));

					// Input should not consume any more bytes
					pub.Input();

					Assert.AreEqual(11, pub.InputStream.Length);
					Assert.AreEqual(0, pub.InputStream.Position);
					Assert.AreEqual("Hello World", asStr(pub.InputStream));

					// Truncate the local copy
					pub.InputStream.SetLength(5);
					pub.InputStream.Seek(4, SeekOrigin.Begin);
					Assert.AreEqual("Hello", asStr(pub.InputStream));

					// Wanting 1 byte should not trigger getting anymore
					pub.WantBytes(1);

					Assert.AreEqual(5, pub.InputStream.Length);
					Assert.AreEqual(4, pub.InputStream.Position);
					Assert.AreEqual("Hello", asStr(pub.InputStream));

					// Wanting 2 bytes will cause us to get the rest of the data
					pub.WantBytes(2);

					Assert.AreEqual(11, pub.InputStream.Length);
					Assert.AreEqual(4, pub.InputStream.Position);
					Assert.AreEqual("Hello World", asStr(pub.InputStream));

					// Truncate the local copy
					pub.InputStream.SetLength(5);
					pub.InputStream.Seek(4, SeekOrigin.Begin);
					Assert.AreEqual("Hello", asStr(pub.InputStream));

					// Input should get the rest of the bytes and not update Position
					pub.Input();

					Assert.AreEqual(11, pub.InputStream.Length);
					Assert.AreEqual(4, pub.InputStream.Position);
					Assert.AreEqual("Hello World", asStr(pub.InputStream));

					pub.Close();
				}
				finally
				{
					pub.Dispose();
				}

				cli.AgentDisconnect();
			}
			finally
			{
				File.Delete(tmp);
			}
		}

		[Test]
		public void Output()
		{
			StartServer();

			var tmp = Path.GetTempFileName();

			try
			{
				var cli = new Client(null, _server.Uri.ToString(), null);

				cli.AgentConnect();

				var pub = cli.CreatePublisher("pub", "File", new Dictionary<string, string>
				{
					{ "FileName", tmp },
				});

				try
				{
					pub.Open(100, false);
					pub.Output(new BitStream(Encoding.ASCII.GetBytes("Hello")));
					pub.Close();

					Assert.AreEqual("Hello", File.ReadAllText(tmp));

					pub.Open(101, false);
					pub.Output(new BitStream(Encoding.ASCII.GetBytes("Hello")));
					pub.Output(new BitStream(Encoding.ASCII.GetBytes("World")));
					pub.Close();

					Assert.AreEqual("HelloWorld", File.ReadAllText(tmp));


					pub.Open(102, false);
					pub.Output(new BitStream(Encoding.ASCII.GetBytes("Hello")));
					pub.Close();

					Assert.AreEqual("Hello", File.ReadAllText(tmp));
				}
				finally
				{
					pub.Dispose();
				}

				cli.AgentDisconnect();
			}
			finally
			{
				File.Delete(tmp);
			}
		}


		[Test]
		public void Call()
		{
			StartServer();

			var tmp = Path.GetTempFileName();

			var args = new List<BitwiseStream>
			{
				new BitStream(Encoding.ASCII.GetBytes("Hello")) { Name = "One" },
				new BitStream(Encoding.ASCII.GetBytes("World")) { Name = "Two" },
			};

			try
			{
				var cli = new Client(null, _server.Uri.ToString(), null);

				cli.AgentConnect();

				var pub = cli.CreatePublisher("pub", "TestRemoteFile", new Dictionary<string, string>
				{
					{ "FileName", tmp },
				});

				try
				{
					pub.Open(100, false);

					var v1 = pub.Call("null", args);

					Assert.Null(v1, "Call method should return null");

					args.Add(new BitStream(Encoding.ASCII.GetBytes("!")) { Name = "Three" });

					var v2 = pub.Call("foo", args);

					Assert.NotNull(v2, "Call method should not return null");

					var asStr = v2.BitsToString();

					Assert.AreEqual("\x07Success", asStr);

					pub.Close();

				}
				finally
				{
					pub.Dispose();
				}

				cli.AgentDisconnect();

				var contents = File.ReadAllLines(tmp);

				var expected = new[] {
					"OnStart",
					"OnOpen",
					"Call: null",
					" Param 'One': Hello",
					" Param 'Two': World",
					"Call: foo",
					" Param 'One': Hello",
					" Param 'Two': World",
					" Param 'Three': !",
					"OnClose",
					"OnStop"
				};

				Assert.That(contents, Is.EqualTo(expected));

			}
			finally
			{
				File.Delete(tmp);
			}
		}
	}
}
