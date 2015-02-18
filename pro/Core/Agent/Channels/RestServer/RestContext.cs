using Peach.Core.Agent;

#if DISABLED

namespace Peach.Pro.Core.Agent.Channels.RestServer
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
		public uint Iteration;
		public bool IsControlIteration;
		public AgentDispatcher Dispatcher = new AgentDispatcher();
	}
}
#endif
