using System;

namespace Peach.Core.Dom.Actions
{
	[Action("Connect")]
	[Serializable]
	public class Connect : Action
	{
		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();
			publisher.open();
		}
	}
}
