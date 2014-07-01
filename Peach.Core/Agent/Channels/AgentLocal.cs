
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
using Peach.Core.Dom;
using NLog;
using Peach.Core.Agent;
using Peach.Core.IO;
using System.IO;

namespace Peach.Core.Agent.Channels
{
	/// <summary>
	/// This is an agent that runs in the local
	/// process, instead of a remote process.  This
	/// is much faster for things like file fuzzing.
	/// </summary>
	[Agent("local", true)]
	public class AgentServerLocal : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			Publisher publisher;

			public PublisherProxy(Publisher publisher)
			{
				this.publisher = publisher;
			}

			#region IPublisher

			public uint Iteration
			{
				set { publisher.Iteration = value; }
			}

			public bool IsControlIteration
			{
				set { publisher.IsControlIteration = value; }
			}

			public string Result
			{
				get { return publisher.Result; }
			}

			public Stream Stream
			{
				get { return publisher; }
			}

			public void Start()
			{
				publisher.start();
			}

			public void Stop()
			{
				publisher.stop();
			}

			public void Open()
			{
				publisher.open();
			}

			public void Close()
			{
				publisher.close();
			}

			public void Accept()
			{
				publisher.accept();
			}

			public Variant Call(string method, List<ActionParameter> args)
			{
				return publisher.call(method, args);
			}

			public void SetProperty(string property, Variant value)
			{
				publisher.setProperty(property, value);
			}

			public Variant GetProperty(string property)
			{
				return publisher.getProperty(property);
			}

			public void Output(DataModel data)
			{
				publisher.output(data);
			}

			public void Input()
			{
				publisher.input();
			}

			public void WantBytes(long count)
			{
				publisher.WantBytes(count);
			}

			#endregion
		}

		#endregion

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		Agent agent = new Agent();

		public AgentServerLocal(string name, string uri, string password)
			: base(name, uri, password)
		{
		}

		protected override void OnAgentConnect()
		{
			agent.AgentConnect();
		}

		protected override void OnAgentDisconnect()
		{
			agent.AgentDisconnect();
		}

		protected override IPublisher OnCreatePublisher(string cls, Dictionary<string, Variant> args)
		{
			return new PublisherProxy(agent.CreatePublisher(cls, args));
		}

		protected override void OnStartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			agent.StartMonitor(name, cls, args);
		}

		protected override void OnStopAllMonitors()
		{
			agent.StopAllMonitors();
		}

		protected override void OnSessionStarting()
		{
			agent.SessionStarting();
		}

		protected override void OnSessionFinished()
		{
			agent.SessionFinished();
		}

		protected override void OnIterationStarting(uint iterationCount, bool isReproduction)
		{
			agent.IterationStarting(iterationCount, isReproduction);
		}

		protected override bool OnIterationFinished()
		{
			return agent.IterationFinished();
		}

		protected override bool OnDetectedFault()
		{
			return agent.DetectedFault();
		}

		protected override Fault[] OnGetMonitorData()
		{
			return agent.GetMonitorData();
		}

		protected override bool OnMustStop()
		{
			return agent.MustStop();
		}

		protected override Variant OnMessage(string name, Variant data)
		{
			return agent.Message(name, data);
		}
	}
}
