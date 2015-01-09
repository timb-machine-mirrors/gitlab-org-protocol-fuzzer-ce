using System.Threading;

namespace Peach.Pro.Core.Agent.Channels.RestServer
{
	/// <summary>
	/// Agent task running from dispatch thread.
	/// </summary>
	public class AgentTask
	{
		/// <summary>
		/// Contians result of task
		/// </summary>
		public object Result = null;
		
		/// <summary>
		/// Parameters for Task
		/// </summary>
		public object[] Parameters = null;

		/// <summary>
		/// Set by task to mark it's completed
		/// </summary>
		public ManualResetEvent Completed = new ManualResetEvent(false);

		public delegate object TaskDelegate(object[] args);

		/// <summary>
		/// Task to run
		/// </summary>
		public TaskDelegate Task = null;
	}
}
