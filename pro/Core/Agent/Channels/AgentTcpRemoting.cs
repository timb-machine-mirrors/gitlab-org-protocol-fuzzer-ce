
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.Core.Agent.Channels
{
	#region TCP Agent Client

	[Agent("tcp")]
	public class AgentClientTcpRemoting : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			#region Serialization Helpers

			private static byte[] ToBytes<T>(T t) where T: class
			{
				if (t == null)
					return null;

				var ms = new MemoryStream();
				var fmt = new BinaryFormatter();
				fmt.Serialize(ms, t);
				return ms.ToArray();
			}

			private static T FromBytes<T>(byte[] bytes) where T: class
			{
				if (bytes == null)
					return null;

				var ms = new MemoryStream(bytes);
				var fmt = new BinaryFormatter();
				var obj = fmt.Deserialize(ms);
				return (T)obj;
			}

			#endregion

			#region Run Action On Proxy

			private static void Exec(string what, System.Action action)
			{
				Exception remotingException = null;

				var th = new Thread(delegate()
				{
					try
					{
						action();
					}
					catch (RemotingException ex)
					{
						logger.Trace("Ignoring remoting exception during {0}", what);
						logger.Trace("\n{0}", ex);
					}
					catch (PeachException ex)
					{
						remotingException = new PeachException(ex.Message, ex);
					}
					catch (SoftException ex)
					{
						remotingException = new SoftException(ex.Message, ex);
					}
					catch (Exception ex)
					{
						remotingException = new AgentException(ex.Message, ex);
					}
				});

				th.Start();

				if (!th.Join(remotingWaitTime))
				{
					th.Abort();
					th.Join();

					logger.Trace("Ignoring remoting timeout during {0}", what);
				}

				if (remotingException != null)
					throw remotingException;
			}

			private static T Exec<T>(string what, System.Func<T> action)
			{
				T t = default(T);

				Exec(what, () => { t = action(); });

				return t;
			}

			#endregion

			#region Private Members

			PublisherTcpRemote remotePub;
			MemoryStream stream;

			#endregion

			#region Public Members

			public string name { get; private set; }

			public string cls { get; private set; }

			public List<KeyValuePair<string, string>> args { get; private set; }

			public PublisherTcpRemote proxy
			{
				get
				{
					return remotePub;
				}
				set
				{
					// Proxy initialized or changed due to reconnect.
					// Don't have to Exec() this in a worker thread
					// since its called from a worker thread inside
					// of AgentClientTcpRemoting
					remotePub = value;

					// Reset the stream since we have a new remote publisher
					stream.Seek(0, SeekOrigin.Begin);
					stream.SetLength(0);
				}
			}

			#endregion

			#region Constructor

			public PublisherProxy(string name, string cls, Dictionary<string, string> args)
			{
				this.name = name;
				this.cls = cls;
				this.args = args.ToList();
				this.stream = new MemoryStream();

			}

			#endregion

			#region IPublisher

			public Stream InputStream
			{
				get { return stream; }
			}

			public void Dispose()
			{
				Exec("Stop", () => { proxy.Dispose(); });
			}

			public void Open(uint iteration, bool isControlIteration)
			{
				Exec("Open", () => { proxy.Open(iteration, isControlIteration); });
			}

			public void Close()
			{
				Exec("Close", () => { proxy.Close(); });
			}

			public void Accept()
			{
				Exec("Accept", () => { proxy.Accept(); });
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				throw new NotSupportedException();
			}

			public void SetProperty(string property, Variant value)
			{
				var bytes = ToBytes(value);

				Exec("SetProperty", () => { proxy.SetProperty(property, bytes); });
			}

			public Variant GetProperty(string property)
			{
				var bytes = Exec("GetProperty", () => { return proxy.GetProperty(property); });

				return FromBytes<Variant>(bytes);
			}

			public void Output(BitwiseStream data)
			{
				Exec("BeginOutput", () => { proxy.BeginOutput(); });

				var total = data.Length;
				var len = Math.Min(total - data.Position, 1024 * 1024);

				while (len > 0)
				{
					var buf = new byte[len];
					data.Read(buf, 0, buf.Length);

					Exec("Output", () => { proxy.Output(buf); });

					len = Math.Min(total - data.Position, 1024 * 1024);
				}

				Exec("EndOutput", () => { proxy.EndOutput(); });
			}

			public void Input()
			{
				var reset = Exec("Input", () => { return proxy.Input(); });

				if (reset)
				{
					// If remote reset the input position back to zero
					// we need to do the same. This reset happens on
					// datagram publishers like Udp and RawV4.
					stream.Seek(0, SeekOrigin.Begin);
					stream.SetLength(0);
				}

				ReadAllBytes();
			}

			public void WantBytes(long count)
			{
				count -= (InputStream.Length - InputStream.Position);
				if (count <= 0)
					return;

				Exec("WantBytes", () => { proxy.WantBytes(count); });

				ReadAllBytes();
			}

			#endregion

			#region Read Input Bytes

			private void ReadAllBytes()
			{
				var pos = stream.Position;
				var buf = ReadBytes();

				stream.Seek(0, SeekOrigin.End);

				while (buf.Length > 0)
				{
					stream.Write(buf, 0, buf.Length);
					buf = ReadBytes();
				}

				stream.Seek(pos, SeekOrigin.Begin);
			}

			private byte[] ReadBytes()
			{
				return Exec("ReadBytes", () => { return proxy.ReadBytes(); });
			}

			#endregion
		}

		#endregion

		#region Private Members

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		class MonitorInfo
		{
			public string Name { get; set; }
			public string Class { get; set; }
			public List<KeyValuePair<string, string>> Args { get; set; }
		}

		List<MonitorInfo> monitors = new List<MonitorInfo>();
		List<PublisherProxy> publishers = new List<PublisherProxy>();

		static int remotingWaitTime = 1000 * 60 * 1;

		TcpClientChannel channel;
		AgentTcpRemote proxy;
		string serviceUrl;

		#endregion

		#region Constructor

		public AgentClientTcpRemoting(string name, string url, string password)
			: base(name, url, password)
		{
			var uri = new Uri(new Uri(url), "/PeachAgent");
			if (uri.IsDefaultPort)
				uri = new Uri("{0}://{1}:{2}{3}".Fmt(uri.Scheme, uri.Host, AgentServerTcpRemoting.DefaultPort, uri.PathAndQuery));
			serviceUrl = uri.ToString();
		}

		#endregion

		#region Run Action Proxy

		private static void Exec(System.Action action)
		{
			Exception remotingException = null;

			var th = new Thread(delegate()
			{
				try
				{
					action();
				}
				catch (PeachException ex)
				{
					remotingException = new PeachException(ex.Message, ex);
				}
				catch (SoftException ex)
				{
					remotingException = new SoftException(ex.Message, ex);
				}
				catch (RemotingException ex)
				{
					remotingException = new RemotingException(ex.Message, ex);
				}
				catch (Exception ex)
				{
					remotingException = new AgentException(ex.Message, ex);
				}
			});

			th.Start();

			if (!th.Join(remotingWaitTime))
			{
				th.Abort();
				th.Join();
				remotingException = new RemotingException("Remoting call timed out.");
			}

			if (remotingException != null)
				throw remotingException;
		}

		private static T Exec<T>(System.Func<T> action)
		{
			T t = default(T);

			Exec(() => { t = action(); });

			return t;
		}

		#endregion

		#region Remote Channel Control

		private void CreateProxy()
		{
			// Perform server activation
			var server = (AgentServiceTcpRemote)Activator.GetObject(typeof(AgentServiceTcpRemote), serviceUrl);

			if (server == null)
				throw new PeachException("Error, unable to create proxy for remote agent '" + serviceUrl + "'.");

			// Activate the proxy on the client side
			Exec(() => { proxy = server.GetProxy(); });
		}

		private void RemoveProxy()
		{
			proxy = null;
		}

		private void CreateChannel()
		{
			var props = (IDictionary)new Hashtable();
			props["timeout"] = (uint)remotingWaitTime;
			props["connectionTimeout"] = (uint)remotingWaitTime;

#if !MONO
			if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
#endif

			var clientProvider = new BinaryClientFormatterSinkProvider();
			channel = new TcpClientChannel(props, clientProvider);

			try
			{
				ChannelServices.RegisterChannel(channel, false); // Disable security for speed
			}
			catch
			{
				channel = null;
				throw;
			}
		}

		private void RemoveChannel()
		{
			if (channel != null)
			{
				try
				{
					ChannelServices.UnregisterChannel(channel);
				}
				finally
				{
					channel = null;
				}
			}
		}

		private void ReconnectProxy(IterationStartingArgs args)
		{
			logger.Debug("ReconnectProxy: Attempting to reconnect");

			CreateProxy();

			Exec(() =>
			{
				proxy.AgentConnect();

				foreach (var item in monitors)
					proxy.StartMonitor(item.Name, item.Class, item.Args);

				proxy.SessionStarting();
				proxy.IterationStarting(args.IsReproduction, args.LastWasFault);

				// ReconnectProxy is only called via IterationStart()
				// IterationStart is called on the agents before the current
				// Iteration/IsControlIteration is set on the publishers
				// Therefore we just need to recreate the publisher proxy
				foreach (var item in publishers)
					item.proxy = proxy.CreatePublisher(item.name, item.cls, item.args);
			});
		}

		#endregion

		#region AgentClient Overrides

		public override void AgentConnect()
		{
			System.Diagnostics.Debug.Assert(channel == null);

			try
			{
				CreateChannel();
				CreateProxy();

				try
				{
					Exec(() => proxy.AgentConnect());
				}
				catch (Exception ex)
				{
					throw new PeachException("Error, unable to connect to remote agent '{0}'. {1}".Fmt(serviceUrl, ex.Message), ex);
				}
			}
			catch
			{
				// If this throws, OnAgentDisconnect will not be called
				// so cleanup the proxt and channel

				RemoveProxy();
				RemoveChannel();

				throw;
			}
		}

		public override void AgentDisconnect()
		{
			try
			{
				Exec(() => proxy.AgentDisconnect());
			}
			finally
			{
				RemoveProxy();
				RemoveChannel();
			}
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			var pub = new PublisherProxy(pubName, cls, args);

			publishers.Add(pub);

			Exec(() => { pub.proxy = proxy.CreatePublisher(pub.name, pub.cls, pub.args); });

			return pub;
		}

		public override void StartMonitor(string monName, string cls, Dictionary<string, string> args)
		{
			// Remote 'args' as a List to support mono/microsoft interoperability
			var asList = args.ToList();

			// Keep track of monitor info so we can recreate them if the proxy disappears
			monitors.Add(new MonitorInfo { Name = monName, Class = cls, Args = asList });

			Exec(() => proxy.StartMonitor(monName, cls, asList));
		}

		public override void StopAllMonitors()
		{
			Exec(() => proxy.StopAllMonitors());
		}

		public override void SessionStarting()
		{
			Exec(() => proxy.SessionStarting());
		}

		public override void SessionFinished()
		{
			Exec(() => proxy.SessionFinished());
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			try
			{
				Exec(() => proxy.IterationStarting(args.IsReproduction, args.LastWasFault));
			}
			catch (RemotingException ex)
			{
				logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(args);
			}
			catch (SocketException ex)
			{
				logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(args);
			}
		}

		public override void IterationFinished()
		{
			Exec(() => proxy.IterationFinished());
		}

		public override bool DetectedFault()
		{
			return Exec(() => proxy.DetectedFault());
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			return Exec(() => proxy.GetMonitorData().Select(FromRemoteData));
		}

		public override void Message(string msg)
		{
			Exec(() => proxy.Message(msg));
		}

		#endregion

		private MonitorData FromRemoteData(RemoteData data)
		{
			var ret = new MonitorData
			{
				AgentName = Name,
				MonitorName = data.MonitorName,
				DetectionSource = data.DetectionSource,
				Title = data.Title,
				Data = data.Data.ToDictionary(i => i.Key, i => i.Value),
			};

			if (data.Fault != null)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = data.Fault.Description,
					MajorHash = data.Fault.MajorHash,
					MinorHash = data.Fault.MinorHash,
					Risk = data.Fault.Risk,
					MustStop = data.Fault.MustStop,
				};
			}

			return ret;
		}
	}

	#endregion

	#region Remoting Objects

	#region Publisher Remoting Object

	internal class PublisherTcpRemote  : MarshalByRefObject, IDisposable
	{
		#region Serialization Helpers

		private static byte[] ToBytes<T>(T t) where T : class
		{
			if (t == null)
				return null;

			var ms = new MemoryStream();
			var fmt = new BinaryFormatter();
			fmt.Serialize(ms, t);
			return ms.ToArray();
		}

		private static T FromBytes<T>(byte[] bytes) where T : class
		{
			if (bytes == null)
				return null;

			var ms = new MemoryStream(bytes);
			var fmt = new BinaryFormatter();
			var obj = fmt.Deserialize(ms);
			return (T)obj;
		}

		#endregion

		BitStreamList data;
		Publisher pub;

		public PublisherTcpRemote(Publisher pub)
		{
			this.pub = pub;

			this.pub.start();
		}

		public void Dispose()
		{
			pub.stop();
			pub = null;
		}

		public void Open(uint iteration, bool isControlIteration)
		{
			pub.Iteration = iteration;
			pub.IsControlIteration = isControlIteration;
			pub.open();
		}

		public void Close()
		{
			pub.close();
		}

		public void Accept()
		{
			pub.accept();
		}

		public bool Input()
		{
			pub.input();

			return pub.Position == 0;
		}

		public byte[] ReadBytes()
		{
			var len = Math.Min(pub.Length - pub.Position, 1024 * 1024);
			var buf = new byte[len];

			pub.Read(buf, 0, buf.Length);

			return buf;
		}

		public void WantBytes(long count)
		{
			pub.WantBytes(count);
		}

		public void BeginOutput()
		{
			data = new BitStreamList();
		}

		public void Output(byte[] buf)
		{
			data.Add(new BitStream(buf));
		}

		public void EndOutput()
		{
			pub.output(data);

			data.Dispose();
			data = null;
		}

		public void SetProperty(string property, byte[] bytes)
		{
			var value = FromBytes<Variant>(bytes);
			pub.setProperty(property, value);
		}

		public byte[] GetProperty(string property)
		{
			var value = pub.getProperty(property);
			return ToBytes(value);
		}
	}

	#endregion

	#region Agent Remoting Object

	[Serializable]
	internal class RemoteData
	{
		[Serializable]
		internal class Info
		{
			public string Description { get; set; }
			public string MajorHash { get; set; }
			public string MinorHash { get; set; }
			public string Risk { get; set; }
			public bool MustStop { get; set; }
		}

		public string MonitorName { get; set; }
		public string DetectionSource { get; set; }
		public string Title { get; set; }
		public Info Fault { get; set; }
		public List<KeyValuePair<string, byte[]>> Data { get; set; }
	}

	/// <summary>
	/// Implement agent service running over .NET TCP remoting
	/// </summary>
	internal class AgentTcpRemote : MarshalByRefObject
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Agent _agent = new Agent();

		public PublisherTcpRemote CreatePublisher(string name, string cls, IEnumerable<KeyValuePair<string, string>> args)
		{
			logger.Trace("CreatePublisher: {0}", cls);
			return new PublisherTcpRemote(_agent.CreatePublisher(name, cls, args.ToDictionary(i => i.Key, i => i.Value)));
		}

		public void AgentConnect()
		{
			logger.Trace("AgentConnect");
			_agent.AgentConnect();
		}

		public void AgentDisconnect()
		{
			logger.Trace("AgentDisconnect");
			_agent.AgentDisconnect();
		}

		public void StartMonitor(string name, string cls, IEnumerable<KeyValuePair<string, string>> args)
		{
			logger.Trace("StartMonitor: {0} {1}", name, cls);
			_agent.StartMonitor(name, cls, args.ToDictionary(i => i.Key, i => i.Value));
		}

		public void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");
			_agent.StopAllMonitors();
		}

		public void SessionStarting()
		{
			logger.Trace("SessionStarting");
			_agent.SessionStarting();
		}

		public void SessionFinished()
		{
			logger.Trace("SessionFinished");
			_agent.SessionFinished();
		}

		public void IterationStarting(bool isReproduction, bool lastWasFault)
		{
			logger.Trace("IterationStarting {0} {1}", isReproduction, lastWasFault);
			var args = new IterationStartingArgs
			{
				IsReproduction = isReproduction,
				LastWasFault = lastWasFault
			};

			_agent.IterationStarting(args);
		}

		public void IterationFinished()
		{
			logger.Trace("IterationFinished");
			_agent.IterationFinished();
		}

		public bool DetectedFault()
		{
			logger.Trace("DetectedFault");
			return _agent.DetectedFault();
		}

		public RemoteData[] GetMonitorData()
		{
			logger.Trace("GetMonitorData");
			return _agent.GetMonitorData().Select(ToRemoteData).ToArray();
		}

		public void Message(string msg)
		{
			logger.Trace("Message: {0}", msg);
			_agent.Message(msg);
		}

		private static RemoteData ToRemoteData(MonitorData data)
		{
			var ret = new RemoteData
			{
				MonitorName = data.MonitorName,
				DetectionSource = data.DetectionSource,
				Title = data.Title,
				Data = data.Data.ToList(),
			};

			if (data.Fault != null)
			{
				ret.Fault = new RemoteData.Info
				{
					Description = data.Fault.Description,
					MajorHash = data.Fault.MajorHash,
					MinorHash = data.Fault.MinorHash,
					Risk = data.Fault.Risk,
					MustStop = data.Fault.MustStop,
				};
			}

			return ret;
		}
	}

	#endregion

	#region Agent Remote Service

	internal class AgentServiceTcpRemote : MarshalByRefObject
	{
		public AgentTcpRemote GetProxy()
		{
			return new AgentTcpRemote();
		}
	}

	#endregion

	#endregion

	#region TCP Agent Server

	[AgentServer("tcp")]
	public class AgentServerTcpRemoting : IAgentServer
	{
		private const string portOption = "--port=";

		public const ushort DefaultPort = 9001;

		#region IAgentServer Members

		public void Run(Dictionary<string, string> args)
		{
#if !MONO
			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
#endif

			var port = DefaultPort;

			foreach (var kv in args)
			{
				if (kv.Value.StartsWith(portOption))
				{
					var opt = kv.Value.Substring(portOption.Length);
					if (!ushort.TryParse(opt, out port))
						throw new PeachException("An invalid option for --port was specified.  The value '{0}' is not a valid port number.".Fmt(opt));
				}
			}

			// select channel to communicate
			var props = (IDictionary)new Hashtable();
			props["port"] = (int)port;
			props["name"] = string.Empty;

			var agentBindIp = ConfigurationManager.AppSettings["AgentBindIp"];
			if (!string.IsNullOrEmpty(agentBindIp))
				props["bindTo"] = agentBindIp;

			var serverProvider = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};
			var chan = new TcpServerChannel(props, serverProvider);

			// register channel
			ChannelServices.RegisterChannel(chan, false);

			// register remote object
			// mono doesn't work with client activation so
			// use singleton activation with a function to
			// provide the actual client instance
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(AgentServiceTcpRemote),
				"PeachAgent", WellKnownObjectMode.Singleton);

			// inform console
			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Listening for connections on port {0}", port);
			Console.WriteLine();
			Console.WriteLine(" -- Press ENTER to quit agent -- ");
			Console.ReadLine();
		}

		#endregion
	}

	#endregion
}
