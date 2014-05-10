
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
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Peach.Core;
using Peach.Core.Dom;
using NLog;
using Peach.Core.Agent;
using System.Net.Sockets;
using Peach.Core.IO;
using System.IO;
using System.Runtime.Remoting.Activation;

namespace Peach.Core.Agent.Channels
{
	#region TCP Agent Client

	[Agent("tcp", true)]
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

			uint iteration;
			bool isControlIteration;
			PublisherTcpRemote remotePub;
			MemoryStream stream;

			#endregion

			#region Public Members

			public string cls { get; private set; }

			public List<KeyValuePair<string, Variant>> args { get; private set; }

			public PublisherTcpRemote proxy
			{
				get
				{
					return remotePub;
				}
				set
				{
					if (remotePub != null)
					{
						// Proxy changed due to reconnect.
						// Don't have to Exec() this in a worker thread
						// since its called from a worker thread inside
						// of AgentClientTcpRemoting
						value.Iteration = iteration;
						value.IsControlIteration = isControlIteration;
						value.Start();
					}

					remotePub = value;

					// Reset the stream since we have a new remote publisher
					stream.Seek(0, SeekOrigin.Begin);
					stream.SetLength(0);
				}
			}

			#endregion

			#region Constructor

			public PublisherProxy(string cls, Dictionary<string, Variant> args)
			{
				this.cls = cls;
				this.args = args.ToList();
				this.stream = new MemoryStream();
			}

			#endregion

			#region IPublisher

			public uint Iteration
			{
				set
				{
					iteration = value;
					Exec("Iteration set", () => { proxy.Iteration = value; });
				}
			}

			public bool IsControlIteration
			{
				set
				{
					isControlIteration = value;
					Exec("IsControlIteration set", () => { proxy.IsControlIteration = value; });
				}
			}

			public string Result
			{
				get
				{
					return Exec<string>("Result get", () => { return proxy.Result; });
				}
			}

			public Stream Stream
			{
				get { return stream; }
			}

			public void Start()
			{
				Exec("Start", () => { proxy.Start(); });
			}

			public void Stop()
			{
				Exec("Stop", () => { proxy.Stop(); });
			}

			public void Open()
			{
				Exec("Open", () => { proxy.Open(); });
			}

			public void Close()
			{
				Exec("Close", () => { proxy.Close(); });
			}

			public void Accept()
			{
				Exec("Accept", () => { proxy.Accept(); });
			}

			public Variant Call(string method, List<ActionParameter> args)
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
				var bytes = Exec<byte[]>("GetProperty", () => { return proxy.GetProperty(property); });

				return FromBytes<Variant>(bytes);
			}

			public void Output(DataModel dataModel)
			{
				Exec("BeginOutput", () => { proxy.BeginOutput(); });

				var data = dataModel.Value.PadBits();

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
				var reset = Exec<bool>("Input", () => { return proxy.Input(); });

				if (reset)
				{
					stream.Seek(0, SeekOrigin.Begin);
					stream.SetLength(0);
				}

				ReadAllBytes();
			}

			public void WantBytes(long count)
			{
				Exec("WantBytes", () => { proxy.WantBytes(count); });

				ReadAllBytes();
			}

			#endregion

			#region Read Input Bytes

			private void ReadAllBytes()
			{
				var pos = stream.Position;
				var buf = ReadBytes();

				while (buf.Length > 0)
				{
					stream.Write(buf, 0, buf.Length);
					buf = ReadBytes();
				}

				stream.Seek(pos, SeekOrigin.Begin);
			}

			private byte[] ReadBytes()
			{
				return Exec<byte[]>("ReadBytes", () => { return proxy.ReadBytes(); });
			}

			#endregion
		}

		#endregion

		#region Private Members

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return logger; } }

		class MonitorInfo
		{
			public string Name { get; set; }
			public string Class { get; set; }
			public List<KeyValuePair<string, Variant>> Args { get; set; }
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
			serviceUrl = new Uri(new Uri(url), "/PeachAgent").ToString();
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
			// Perform client activation
			//object[] attr = { new UrlAttribute(serviceUrl) };
			//proxy = (AgentTcpRemote)Activator.CreateInstance(typeof(AgentTcpRemote), null, attr);

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
			props["port"] = 0;
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

		private void ReconnectProxy(uint iterationCount, bool isReproduction)
		{
			logger.Debug("ReconnectProxy: Attempting to reconnect");

			CreateProxy();

			Exec(() =>
			{
				proxy.AgentConnect();

				foreach (var item in monitors)
					proxy.StartMonitor(item.Name, item.Class, item.Args);

				proxy.SessionStarting();
				proxy.IterationStarting(iterationCount, isReproduction);

				// ReconnectProxy is only called via IterationStart()
				// IterationStart is called on the agents before the current
				// Iteration/IsControlIteration is set on the publishers
				// Therefore we just need to recreate the publisher proxy
				foreach (var item in publishers)
					item.proxy = proxy.CreatePublisher(item.cls, item.args);
			});
		}

		#endregion

		#region AgentClient Overrides

		protected override void OnAgentConnect()
		{
			System.Diagnostics.Debug.Assert(channel == null);

			try
			{
				CreateChannel();
				CreateProxy();

				try
				{
					Exec(() => { proxy.AgentConnect(); });
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

		protected override void OnAgentDisconnect()
		{
			try
			{
				Exec(() => { proxy.AgentDisconnect(); });
			}
			finally
			{
				RemoveProxy();
				RemoveChannel();
			}
		}

		protected override IPublisher OnCreatePublisher(string cls, Dictionary<string, Variant> args)
		{
			var pub = new PublisherProxy(cls, args);

			publishers.Add(pub);

			Exec(() => { pub.proxy = proxy.CreatePublisher(pub.cls, pub.args); });

			return pub;
		}

		protected override void OnStartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			// Remote 'args' as a List to support mono/microsoft interoperability
			var asList = args.ToList();

			// Keep track of monitor info so we can recreate them if the proxy disappears
			monitors.Add(new MonitorInfo() { Name = name, Class = cls, Args = asList });

			Exec(() => { proxy.StartMonitor(name, cls, asList); });
		}

		protected override void OnStopAllMonitors()
		{
			Exec(() => { proxy.StopAllMonitors(); });
		}

		protected override void OnSessionStarting()
		{
			Exec(() => { proxy.SessionStarting(); });
		}

		protected override void OnSessionFinished()
		{
			Exec(() => { proxy.SessionFinished(); });
		}

		protected override void OnIterationStarting(uint iterationCount, bool isReproduction)
		{
			try
			{
				Exec(() => { proxy.IterationStarting(iterationCount, isReproduction); });
			}
			catch (RemotingException ex)
			{
				logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(iterationCount, isReproduction);
			}
			catch (SocketException ex)
			{
				logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(iterationCount, isReproduction);
			}
		}

		protected override bool OnIterationFinished()
		{
			return Exec<bool>(() => { return proxy.IterationFinished(); });
		}

		protected override bool OnDetectedFault()
		{
			return Exec<bool>(() => { return proxy.DetectedFault(); });
		}

		protected override Fault[] OnGetMonitorData()
		{
			return Exec<Fault[]>(() => { return proxy.GetMonitorData(); });
		}

		protected override bool OnMustStop()
		{
			return Exec<bool>(() => { return proxy.MustStop(); });
		}

		protected override Variant OnMessage(string name, Variant data)
		{
			return Exec<Variant>(() => { return proxy.Message(name, data); });
		}

		#endregion
	}

	#endregion

	#region Remoting Objects

	#region Publisher Remoting Object

	internal class PublisherTcpRemote  : MarshalByRefObject
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
		}

		public uint Iteration
		{
			set { pub.Iteration = value; }
		}

		public bool IsControlIteration
		{
			set { pub.IsControlIteration = value; }
		}

		public string Result
		{
			get { return pub.Result; }
		}

		public void Start()
		{
			pub.start();
		}

		public void Stop()
		{
			pub.stop();
		}

		public void Open()
		{
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

	/// <summary>
	/// Implement agent service running over .NET TCP remoting
	/// </summary>
	internal class AgentTcpRemote : MarshalByRefObject, IAgent
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		Agent agent = new Agent();

		public AgentTcpRemote()
		{
		}

		public PublisherTcpRemote CreatePublisher(string cls, IEnumerable<KeyValuePair<string, Variant>> args)
		{
			logger.Trace("CreatePublisher: {0}", cls);
			return new PublisherTcpRemote(agent.CreatePublisher(cls, args));
		}

		public void AgentConnect()
		{
			logger.Trace("AgentConnect");
			agent.AgentConnect();
		}

		public void AgentDisconnect()
		{
			logger.Trace("AgentDisconnect");
			agent.AgentDisconnect();
		}

		public void StartMonitor(string name, string cls, IEnumerable<KeyValuePair<string, Variant>> args)
		{
			logger.Trace("StartMonitor: {0} {1}", name, cls);
			agent.StartMonitor(name, cls, args);
		}

		public void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");
			agent.StopAllMonitors();
		}

		public void SessionStarting()
		{
			logger.Trace("SessionStarting");
			agent.SessionStarting();
		}

		public void SessionFinished()
		{
			logger.Trace("SessionFinished");
			agent.SessionFinished();
		}

		public void IterationStarting(uint iterationCount, bool isReproduction)
		{
			logger.Trace("IterationStarting: {0} {1}", iterationCount, isReproduction);
			agent.IterationStarting(iterationCount, isReproduction);
		}

		public bool IterationFinished()
		{
			logger.Trace("IterationFinished");
			return agent.IterationFinished();
		}

		public bool DetectedFault()
		{
			logger.Trace("DetectedFault");
			return agent.DetectedFault();
		}

		public Fault[] GetMonitorData()
		{
			logger.Trace("GetMonitorData");
			return agent.GetMonitorData();
		}

		public bool MustStop()
		{
			logger.Trace("MustStop");
			return agent.MustStop();
		}

		public Variant Message(string name, Variant data)
		{
			logger.Trace("Message: {0}", name);
			return agent.Message(name, data);
		}

		public object QueryMonitors(string query)
		{
			logger.Trace("QueryMonitors: {0}", query);
			return agent.QueryMonitors(query);
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
		#region IAgentServer Members

		public void Run(Dictionary<string, string> args)
		{
#if !MONO
			if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
#endif

			int port = 9001;

			if (args.ContainsKey("port"))
				port = int.Parse(args["port"]);

			// select channel to communicate
			var props = (IDictionary)new Hashtable();
			props["port"] = port;
			props["name"] = string.Empty;
			//props["exclusiveAddressUse"] = false;

			var serverProvider = new BinaryServerFormatterSinkProvider();
			serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			var chan = new TcpChannel(props, null, serverProvider);

			// register channel
			ChannelServices.RegisterChannel(chan, false);

			// register remote object
			// mono doesn't work with client activation so
			// use singleton activation with a function to
			// provide the actual client instance
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(AgentServiceTcpRemote),
				"PeachAgent", WellKnownObjectMode.Singleton);

			// register remote object for client activation
			//RemotingConfiguration.ApplicationName = "PeachAgent";
			//RemotingConfiguration.RegisterActivatedServiceType(typeof(AgentTcpRemote));

			//inform console
			Console.WriteLine(" -- Press ENTER to quit agent -- ");
			Console.ReadLine();
		}

		#endregion
	}

	#endregion
}
