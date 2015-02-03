using System.Collections.Concurrent;
using System.Threading;

namespace Peach.Pro.Core.Agent.Channels.RestServer
{
	/// <summary>
	/// Thread for performing Agent calls. This allows Agent interface to be single threaded.
	/// </summary>
	public class AgentDispatcher
	{
		/// <summary>
		/// Set when a task is added to Queue
		/// </summary>
		AutoResetEvent _taskQueued = new AutoResetEvent(false);

		/// <summary>
		/// Set this event to cause dispatcher to exit
		/// </summary>
		ManualResetEvent _exit = new ManualResetEvent(false);

		/// <summary>
		/// Queue of tasks to perform
		/// </summary>
		ConcurrentQueue<AgentTask> _taskQueue = new ConcurrentQueue<AgentTask>();

		Thread _dispatcher = null;

		/// <summary>
		/// Start dispatcher thread
		/// </summary>
		public void Start()
		{
			if (_dispatcher != null)
				return;

			_dispatcher = new Thread(new ThreadStart(Run));
			_dispatcher.Start();
		}

		/// <summary>
		/// Stop dispatcher thread
		/// </summary>
		public void Stop()
		{
			_exit.Set();
			_dispatcher.Join();
			_dispatcher = null;
		}

		/// <summary>
		/// Add task to queue
		/// </summary>
		/// <param name="task"></param>
		public void QueueTask(AgentTask task)
		{
			_taskQueue.Enqueue(task);
			_taskQueued.Set();
		}

		void Run()
		{
			AgentTask task = null;

			while (!_exit.WaitOne(0))
			{
				if (!_taskQueued.WaitOne(100))
					continue;

				if (_taskQueue.TryDequeue(out task))
				{
					task.Result = task.Task(task.Parameters);
					task.Completed.Set();
				}
			}
		}
	}
}
