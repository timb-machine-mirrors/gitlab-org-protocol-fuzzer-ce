using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;
using System.Runtime.Remoting;

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
		public Dictionary<string, Variant> Args { get; protected set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		private Peach.Core.Agent.IPublisher publisher;

		public RemotePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			Args = args;
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

				// Can be null if called before start()
				if (publisher != null)
					publisher.Iteration = value;
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

				// Can be null if called before start()
				if (publisher != null)
					publisher.IsControlIteration = value;
			}
		}

		public override string Result
		{
			get
			{
				return publisher.Result;
			}
		}

		protected override void OnStart()
		{
			publisher = Test.parent.context.agentManager.CreatePublisher(Agent, Class, Args);

			publisher.Iteration = Iteration;
			publisher.IsControlIteration = IsControlIteration;
			publisher.Start();

			stream = publisher.Stream;
		}

		protected override void OnStop()
		{
			// Stream is managed by IPublisher
			stream = null;

			// Might be null if start() threw an exception
			if (publisher != null)
				publisher.Stop();
		}

		protected override void OnOpen()
		{
			publisher.Open();
		}

		protected override void OnClose()
		{
			publisher.Close();
		}

		protected override void OnAccept()
		{
			publisher.Accept();
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			return publisher.Call(method, args);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			publisher.SetProperty(property, value);
		}

		protected override Variant OnGetProperty(string property)
		{
			return publisher.GetProperty(property);
		}

		protected override void OnOutput(BitwiseStream data)
		{
			// Should never get called
			throw new NotSupportedException();
		}

		public override void output(DataModel data)
		{
			publisher.Output(data);
		}

		protected override void OnInput()
		{
			publisher.Input();
		}

		public override void WantBytes(long count)
		{
			count -= stream.Length - stream.Position;
			if (count > 0)
				publisher.WantBytes(count);
		}
	}
}
