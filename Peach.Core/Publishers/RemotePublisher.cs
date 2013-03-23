using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;

namespace Peach.Core.Publishers
{
	[Publisher("Remote", true)]
	[Parameter("Agent", typeof(string), "Name of agent to host the publisher")]
	[Parameter("Class", typeof(string), "Publisher to host")]
	[InheritParameter("Class")]
	public class RemotePublisher : StreamPublisher
	{
		public string Agent { get; protected set; }
		public string Class { get; protected set; }
		public SerializableDictionary<string, Variant> Args { get; protected set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		private Publisher _publisher = null;
		int _remotingWaitTime = 1000 * 60 * 1;

		public RemotePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			this.Args = new SerializableDictionary<string,Variant>();

			foreach (var kv in args)
				this.Args.Add(kv.Key, kv.Value);

			stream = new MemoryStream();
		}


		/// <summary>
		/// Perform our remoting call with a forced timeout.
		/// </summary>
		/// <param name="method"></param>
		protected void PerformRemoting(ThreadStart method)
		{
			Exception remotingException = null;

			var thread = new System.Threading.Thread(delegate()
			{
				try
				{
					method();
				}
				catch (Exception ex)
				{
					remotingException = ex;
				}
			});

			thread.Start();
			if (thread.Join(_remotingWaitTime))
			{
				if (remotingException != null)
				{
					throw remotingException;
				}
			}
			else
			{
				throw new System.Runtime.Remoting.RemotingException("Remoting call timed out.");
			}
		}

		protected RunContext Context
		{
			get
			{
				Dom.Dom dom = this.Test.parent as Dom.Dom;
				return dom.context;
			}
		}

		public override Test Test
		{
			get { return base.Test; }
			set { base.Test = value; }
		}

		protected void RestartRemotePublisher()
		{
			try
			{
				logger.Debug("Restarting remote publisher");

				_publisher = Context.agentManager.CreatePublisher(Agent, Class, Args);
				_publisher.Iteration = Iteration;
				_publisher.IsControlIteration = IsControlIteration;
				_publisher.start();
			}
			catch(Exception ex)
			{
				// Allow iteration to complete in case there is a latent
				// fault we must catch, then exit.
				//Context.agentManager.mustStopDueToError = true;

				logger.Warn("Ignoring exception on remote publisher restart.  Will exit on MustStop. [" + ex.Message + "]");

				throw new SoftException(ex.Message);
			}
		}

		public override uint Iteration
		{
			get
			{
				return base.Iteration;
			}
			set
			{
				base.Iteration = value;

				if (_publisher != null)
				{
					try
					{
						_publisher.Iteration = value;
					}
					catch (System.Runtime.Remoting.RemotingException)
					{
						RestartRemotePublisher();
					}
				}
			}
		}

		public override bool IsControlIteration
		{
			get
			{
				return base.IsControlIteration;
			}
			set
			{
				base.IsControlIteration = value;

				if (_publisher != null)
				{
					try
					{
						_publisher.IsControlIteration = value;
					}
					catch (System.Runtime.Remoting.RemotingException)
					{
						RestartRemotePublisher();
					}
				}
			}
		}

		public override string Result
		{
			get
			{
				return _publisher.Result;
			}
			set
			{
				_publisher.Result = value;
			}
		}

		protected override void OnStart()
		{
			logger.Debug(">> OnStart");
			_publisher = Context.agentManager.CreatePublisher(Agent, Class, Args);
			_publisher.Iteration = Iteration;
			_publisher.IsControlIteration = IsControlIteration;
			_publisher.start();
		}

		protected override void OnStop()
		{
			try
			{
				PerformRemoting(delegate() { _publisher.stop(); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
			}

			_publisher = null;
		}

		protected override void OnOpen()
		{
			try
			{
				PerformRemoting(delegate() { _publisher.open(); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				RestartRemotePublisher();

				try
				{
					PerformRemoting(delegate() { _publisher.open(); });
				}
				catch(System.Runtime.Remoting.RemotingException)
				{
					// Ignore
				}
			}
		}

		protected override void OnClose()
		{
			try
			{
				PerformRemoting(delegate() { _publisher.close(); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				try
				{
					RestartRemotePublisher();
					_publisher.close();
				}
				catch (System.Runtime.Remoting.RemotingException)
				{
					logger.Warn("Ignoring remoting exception on OnClose");
				}
			}
		}

		protected override void OnAccept()
		{
			try
			{
				PerformRemoting(delegate() { _publisher.accept(); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				RestartRemotePublisher();

				try
				{
					PerformRemoting(delegate() { _publisher.accept(); });
				}
				catch (System.Runtime.Remoting.RemotingException)
				{
					// Ignore
				}
				
			}
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			try
			{
				Variant ret = null;
				PerformRemoting(delegate() { ret = _publisher.call(method, args); });
				return ret;
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				try
				{
					RestartRemotePublisher();
					return _publisher.call(method, args);
				}
				catch (System.Runtime.Remoting.RemotingException)
				{
					logger.Warn("Ignoring remoting exception on OnCall");
					return null;
				}
			}
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			try
			{
				PerformRemoting(delegate() { _publisher.setProperty(property, value); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				try
				{
					RestartRemotePublisher();
					_publisher.setProperty(property, value);
				}
				catch (System.Runtime.Remoting.RemotingException)
				{
					logger.Warn("Ignoring remoting exception on OnSetProperty");
				}
			}
		}

		protected override Variant OnGetProperty(string property)
		{
			try
			{
				Variant ret = null;
				PerformRemoting(delegate() { ret = _publisher.getProperty(property); });
				return ret;
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				try
				{
					RestartRemotePublisher();
					return _publisher.getProperty(property);
				}
				catch (System.Runtime.Remoting.RemotingException)
				{
					logger.Warn("Ignoring remoting exception on OnGetProperty");
					return null;
				}
			}
		}

		protected override void OnOutput(byte[] buffer, int offset, int count)
		{
			try
			{
				PerformRemoting(delegate() { _publisher.output(buffer, offset, count); });
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				logger.Warn("Ignoring remoting exception on OnOutput");
			}
		}

		protected override void OnInput()
		{
			try
			{
				PerformRemoting(delegate() { _publisher.input(); });

				stream.Seek(0, SeekOrigin.Begin);
				stream.SetLength(0);

				ReadAllBytes();
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				logger.Warn("Ignoring remoting exception on OnInput");
			}
		}

		public override void WantBytes(long count)
		{
			try
			{
				long need = count - (stream.Length - stream.Position);
				if (need > 0)
				{
					_publisher.WantBytes(need);
					ReadAllBytes();
				}
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				logger.Warn("Ignoring remoting exception on WantBytes");
			}
		}

		private void ReadAllBytes()
		{
			long pos = stream.Position;

			try
			{
				for (;;)
				{
					int b = -1;

					PerformRemoting(delegate() { b = _publisher.ReadByte(); });
					
					if (b == -1)
					{
						stream.Seek(pos, SeekOrigin.Begin);
						return;
					}

					stream.WriteByte((byte)b);
				}
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				logger.Warn("Ignoring remoting exception on ReadAllBytes");
				stream.Seek(pos, SeekOrigin.Begin);
			}
		}
	}
}
