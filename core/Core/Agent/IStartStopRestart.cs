using System;

namespace Peach.Core
{
	public interface IStartStopRestart
	{
		void Stop();

		void Start();

		void Restart();
	}
}

