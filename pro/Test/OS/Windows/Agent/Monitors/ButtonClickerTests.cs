using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Category("Peach")]
	class ButtonClickerTests
	{
		const string Monitor = "ButtonClicker";

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			var msg = "Could not start monitor \"ButtonClicker\".  Monitor 'ButtonClicker' is missing required parameter 'ButtonName'.";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			var form = new ButtonClickerForm();
			var thread = new Thread(() => Application.Run(form));

			try
			{
				var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
				{
					{"WindowText", "ButtonClickerForm"},
					{"ButtonName", "Click Me"},
				})
				{
					IterationStarting = (m, args) =>
					{
						m.IterationStarting(args);

						thread.Start();

						Thread.Sleep(1000);
					}
				};
				var faults = runner.Run();
				Assert.AreEqual(0, faults.Length, "Faults mismatch");
				Assert.IsTrue(form.IsClicked);
			}
			finally
			{
				Application.Exit();
				thread.Join();
			}
		}
	}
}
