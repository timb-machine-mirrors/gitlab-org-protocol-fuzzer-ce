using System;

namespace Peach.Core.Agent
{
	public interface IStartStopRestart
	{
		void Stop();

		void Start();

		void Restart();
	}
}

