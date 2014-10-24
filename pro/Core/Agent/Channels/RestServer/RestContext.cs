using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core.Agent;

namespace Peach.Core.Agent.Channels.RestServer
{
	/// <summary>
	/// The context that is passed to each RestService instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class RestContext
	{
		public RestContext()
		{
			//Dispatcher.Start();
		}

		public Agent Agent = new Agent();
		public IPublisher Publisher;
		public AgentDispatcher Dispatcher = new AgentDispatcher();
	}
}
