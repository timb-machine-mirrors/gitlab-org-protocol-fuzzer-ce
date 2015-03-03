using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core.Agent.Channels.Rest;
using NLog;
using NLog.Config;
using NLog.Targets;

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
				Assert.AreEqual("Hello World", f[0].Data[name].AsString());

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

		[Test]
		public void NotSupportedOutput()
		{
			// Ensure a nice not supported error comes across for publishers
			// that don't support remote output.

			StartServer();

			var tmp = Path.GetTempFileName();

			try
			{
				var cli = new Client(null, _server.Uri.ToString(), null);

				cli.AgentConnect();

				var pub = cli.CreatePublisher("pub", "Zip", new Dictionary<string, string>
				{
					{ "FileName", tmp },
				});

				try
				{
					pub.Open(100, false);

					var ex = Assert.Throws<PeachException>(() =>
						pub.Output(new BitStream(Encoding.ASCII.GetBytes("Hello"))));

					Assert.AreEqual("The Zip publisher does not support output actions when run on remote agents.", ex.Message);
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
		public void NotSupportedCall()
		{
			// Ensure a nice not supported error comes across for publishers
			// that don't support remote call.

			StartServer();

			var cli = new Client(null, _server.Uri.ToString(), null);

			cli.AgentConnect();

			var pub = cli.CreatePublisher("pub", "Rest", new Dictionary<string, string>());

			try
			{
				pub.Open(100, false);

				const string method = "GET http://foo.com/{0}";
				var args = new List<BitwiseStream>
				{
					new BitStream(Encoding.ASCII.GetBytes("Hello"))
				};

				var ex = Assert.Throws<PeachException>(() => pub.Call(method, args));

				Assert.AreEqual("The Rest publisher does not support call actions when run on remote agents.", ex.Message);
			}
			finally
			{
				pub.Dispose();
			}

			cli.AgentDisconnect();
		}

		[Test]
		public void FastReconnect()
		{
			// If an error happens during fuzzing and the agent server goes away.
			// The agent client should automatically reconnect on the
			// next IterationStarting()

			StartServer();

			var tmp = Path.GetTempFileName();

			try
			{
				var cli = new Client(null, _server.Uri.ToString(), null);

				cli.AgentConnect();
				cli.StartMonitor("mon1", "Null", new Dictionary<string, string>
				{
					{ "LogFile", tmp },
				});
				cli.StartMonitor("mon2", "Null", new Dictionary<string, string>
				{
					{ "LogFile", tmp },
				});
				cli.SessionStarting();

				cli.IterationStarting(new IterationStartingArgs());

				var actual = File.ReadAllLines(tmp);
				var expected = new List<string>
				{
					"mon1.StartMonitor",
					"mon2.StartMonitor",
					"mon1.SessionStarting",
					"mon2.SessionStarting",
					"mon1.IterationStarting False False",
					"mon2.IterationStarting False False",
				};

				Assert.That(actual, Is.EqualTo(expected));

				// Simulate disconnect that occurs when the target
				// crashes during fuzzing.
				// This will gracefully shut down the remote monitors
				// But leave the client in a state that simulates a disconnect

				cli.SimulateDisconnect();

				actual = File.ReadAllLines(tmp);
				expected.AddRange(new[]
				{
					"mon2.StopMonitor",
					"mon1.StopMonitor"
				});

				Assert.That(actual, Is.EqualTo(expected));

				var ex = Assert.Throws<WebException>(cli.IterationFinished);
				StringAssert.Contains("(404) Not Found", ex.Message);

				Assert.False(cli.DetectedFault(), "Should not detect a fault");
				var data1 = cli.GetMonitorData();
				Assert.AreEqual(0, data1.Count(), "Should not have monitor data");

				// DetectedFault and GetMonitorData will not get remoted
				actual = File.ReadAllLines(tmp);
				Assert.That(actual, Is.EqualTo(expected));

				// The calls to IterationFinished/DetectedFault
				// don't do antything, but the next call
				// to iteration starting will trigger a reconnect

				cli.IterationStarting(new IterationStartingArgs());
				cli.IterationFinished();
				Assert.False(cli.DetectedFault(), "Should not detect a fault");
				var data2 = cli.GetMonitorData();
				Assert.AreEqual(0, data2.Count(), "Should not have monitor data");

				actual = File.ReadAllLines(tmp);
				expected.AddRange(new[]
				{
					"mon1.StartMonitor",
					"mon2.StartMonitor",
					"mon1.SessionStarting",
					"mon2.SessionStarting",
					"mon1.IterationStarting False False",
					"mon2.IterationStarting False False",
					"mon2.IterationFinished",
					"mon1.IterationFinished",
					"mon1.DetectedFault",
					"mon2.DetectedFault",
					"mon1.GetMonitorData",
					"mon2.GetMonitorData",
				});

				Assert.That(actual, Is.EqualTo(expected));

				// Simulate an agent disconnect that can occur if a fault
				// is detected and a virtual machine target is restarted
				// This presents itself as a 404 in IterationStarting

				cli.SimulateDisconnect();

				actual = File.ReadAllLines(tmp);
				expected.AddRange(new[]
				{
					"mon2.StopMonitor",
					"mon1.StopMonitor"
				});

				Assert.That(actual, Is.EqualTo(expected));

				cli.IterationStarting(new IterationStartingArgs());
				cli.IterationFinished();
				cli.SessionFinished();
				cli.StopAllMonitors();
				cli.AgentDisconnect();

				actual = File.ReadAllLines(tmp);
				expected.AddRange(new[]
				{
					"mon1.StartMonitor",
					"mon2.StartMonitor",
					"mon1.SessionStarting",
					"mon2.SessionStarting",
					"mon1.IterationStarting False False",
					"mon2.IterationStarting False False",
					"mon2.IterationFinished",
					"mon1.IterationFinished",
					"mon2.SessionFinished",
					"mon1.SessionFinished",
					"mon2.StopMonitor",
					"mon1.StopMonitor"
				});

				Assert.That(actual, Is.EqualTo(expected));
			}
			finally
			{
				File.Delete(tmp);
			}
		}

		[Test]
		public void PublisherError()
		{
			// Ensure a nice not supported error comes across for publishers
			// that can't be constructed

			StartServer();

			var cli = new Client(null, _server.Uri.ToString(), null);

			cli.AgentConnect();

			var ex = Assert.Throws<PeachException>(() =>
				cli.CreatePublisher("pub", "Tcp", new Dictionary<string, string>
				{
					{ "Host", "localost" },
					{ "Port", "badport" }
				}));

			StringAssert.IsMatch("Could not start publisher \"Tcp\".", ex.Message);
			StringAssert.IsMatch("Publisher 'Tcp' could not set parameter 'Port'.", ex.Message);

			cli.AgentDisconnect();
		}

		[Test]
		public void TestSessionFinished()
		{
			// Behaivor of peach 3.4 is depth first init and breadth first shutdown
			// Ensure rest agent works this way

			var agent2 = new RestTests();
			var tmp = Path.GetTempFileName();

			try
			{
				StartServer();
				agent2.StartServer();

				var mgr = new AgentManager(new RunContext());

				mgr.Connect(new Peach.Core.Dom.Agent
				{
					Name = "agent1",
					location = _server.Uri.ToString(),
					monitors = new NamedCollection<Peach.Core.Dom.Monitor>
					{
						new Peach.Core.Dom.Monitor
						{
							Name = "a1mon1",
							cls = "Null",
							parameters = new Dictionary<string,Variant>
							{
								{ "LogFile", new Variant(tmp) },
							}
						},
						new Peach.Core.Dom.Monitor
						{
							Name = "a1mon2",
							cls = "Null",
							parameters = new Dictionary<string,Variant>
							{
								{ "LogFile", new Variant(tmp) },
							}
						},
					}
				});

				mgr.Connect(new Peach.Core.Dom.Agent
				{
					Name = "agent2",
					location = agent2._server.Uri.ToString(),
					monitors = new NamedCollection<Peach.Core.Dom.Monitor>
					{
						new Peach.Core.Dom.Monitor
						{
							Name = "a2mon1",
							cls = "Null",
							parameters = new Dictionary<string,Variant>
							{
								{ "LogFile", new Variant(tmp) },
							}
						},
						new Peach.Core.Dom.Monitor
						{
							Name = "a2mon2",
							cls = "Null",
							parameters = new Dictionary<string,Variant>
							{
								{ "LogFile", new Variant(tmp) },
							}
						},
					}
				});

				mgr.IterationStarting(false, false);

				mgr.IterationFinished();

				mgr.Dispose();

				var actual = File.ReadAllLines(tmp);
				var expected = new[]
				{
					"a1mon1.StartMonitor",
					"a1mon2.StartMonitor",
					"a1mon1.SessionStarting",
					"a1mon2.SessionStarting",

					"a2mon1.StartMonitor",
					"a2mon2.StartMonitor",
					"a2mon1.SessionStarting",
					"a2mon2.SessionStarting",

					"a1mon1.IterationStarting False False",
					"a1mon2.IterationStarting False False",
					"a2mon1.IterationStarting False False",
					"a2mon2.IterationStarting False False",
					"a2mon2.IterationFinished",
					"a2mon1.IterationFinished",
					"a1mon2.IterationFinished",
					"a1mon1.IterationFinished",

					"a2mon2.SessionFinished",
					"a2mon1.SessionFinished",
					"a1mon2.SessionFinished",
					"a1mon1.SessionFinished",

					"a2mon2.StopMonitor",
					"a2mon1.StopMonitor",
					"a1mon2.StopMonitor",
					"a1mon1.StopMonitor"
				};

				Assert.That(actual, Is.EqualTo(expected));

			}
			finally
			{
				File.Delete(tmp);
				agent2.TearDown();
			}
		}

		[Test]
		public void TestLogging()
		{
			var target = new TestLogTarget();
			var config = LogManager.Configuration;
			var rule = new LoggingRule("Agent.Agent1", LogLevel.Info, target);

			config.AddTarget("TestLog", target);
			config.LoggingRules.Add(rule);
			LogManager.ReconfigExistingLoggers();

			StartServer();

			var cli = new Client("Agent1", _server.Uri.ToString(), null);
			cli.AgentConnect();
			cli.StartMonitor("mon1", "Null", new Dictionary<string, string>
			{
				{ "UseNLog", "true" },
			});
			cli.SessionStarting();
			cli.IterationStarting(new IterationStartingArgs());
			cli.IterationFinished();
			cli.SessionFinished();
			cli.StopAllMonitors();
			cli.AgentDisconnect();

			var expected = new[]
			{
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.StartMonitor",
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.SessionStarting",
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.IterationStarting False False",
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.IterationFinished",
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.SessionFinished",
				"Info|[Agent1] Peach.Pro.Core.Agent.Monitors.NullMonitor|mon1.StopMonitor",
			};
			Assert.That(target.Messages, Is.EqualTo(expected));
		}

		class TestLogTarget : TargetWithLayout
		{
			public List<string> Messages { get; private set; }

			public TestLogTarget()
			{
				Messages = new List<string>();
				Layout = "${level}|${logger}|${message}";
			}

			protected override void Write(LogEventInfo logEvent)
			{
				var message = Layout.Render(logEvent);
				Messages.Add(message);
			}
		}
	}
}
