using System;

namespace Peach.Core.Dom.Actions
{
	[Action("Start")]
	[Serializable]
	public class Start : Action
	{
		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();
		}
	}
}
