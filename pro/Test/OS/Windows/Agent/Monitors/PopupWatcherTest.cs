using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.OS.Windows.Agent.Monitors;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture, Category("Peach")]
	public class PopupWatcherTest
	{
		class LameWindow : NativeWindow, IDisposable
		{
			// ReSharper disable once InconsistentNaming
			private const uint WM_CLOSE = 0x0010;
			private readonly ApplicationContext _ctx = new ApplicationContext();

			private LameWindow()
			{
			}

			public static void Run(string windowTitle)
			{
				using (var ret = new LameWindow())
				{
					ret.CreateHandle(new CreateParams { Caption = windowTitle });
					Application.Run(ret._ctx);
				}
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_CLOSE)
					_ctx.ExitThread();

				base.WndProc(ref m);
			}

			public void Dispose()
			{
				DestroyHandle();
			}
		}

		[Test]
		public void TestNoWindow()
		{
			var mon = new PopupWatcher(null);
			var ex = Assert.Throws<PeachException>(() =>
				mon.StartMonitor(new Dictionary<string, string>()));
			Assert.AreEqual("Monitor 'PopupWatcher' is missing required parameter 'WindowNames'.", ex.Message);
		}

		[Test]
		public void TestNoFault()
		{
			var windowName = "PopupWatcherTest - " + System.Diagnostics.Process.GetCurrentProcess().Id;

			var runner = new MonitorRunner("PopupWatcher", new Dictionary<string, string>
			{
				
				{ "WindowNames", windowName },
				{ "Fault", "false" },
			})
			{
				Message = m =>
				{
					// Monitor won't produce a fault unless the window is closed
					// between IterationStarting and IterationFinished

					// Window gets closed by posting a WM_CLOSE message

					var th = new Thread(() => LameWindow.Run(windowName));

					th.Start();

					if (th.Join(500))
						return;

					th.Abort();
					Assert.Fail("Window did not get closed within 500ms");
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					return true; // Trigger GetMonitorData()
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("PopupWatcher", faults[0].detectionSource);
			Assert.AreEqual("Closed 1 popup window.", faults[0].title);
			Assert.AreEqual(FaultType.Data, faults[0].type);
			Assert.True(faults[0].description.Contains(windowName));
		}

		[Test]
		public void TestWindowList()
		{
			var windowName1 = "Foo - " + System.Diagnostics.Process.GetCurrentProcess().Id;
			var windowName2 = "Bar - " + System.Diagnostics.Process.GetCurrentProcess().Id;

			var runner = new MonitorRunner("PopupWatcher", new Dictionary<string, string>
			{
				
				{ "WindowNames", windowName1 + "," + windowName2 },
				{ "Fault", "false" },
			})
			{
				Message = m =>
				{
					// Monitor won't produce a fault unless the window is closed
					// between IterationStarting and IterationFinished

					// Window gets closed by posting a WM_CLOSE message

					var th1 = new Thread(() =>
					{
						var th2 = new Thread(() => LameWindow.Run(windowName1));

						th2.Start();

						LameWindow.Run(windowName2);

						th2.Join();
					});


					th1.Start();

					if (th1.Join(50000))
						return;

					th1.Abort();
					Assert.Fail("Window did not get closed within 500ms");
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					return true; // Trigger GetMonitorData()
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("PopupWatcher", faults[0].detectionSource);
			Assert.AreEqual("Closed 2 popup windows.", faults[0].title);
			Assert.AreEqual(FaultType.Data, faults[0].type);
			StringAssert.Contains(windowName1, faults[0].description);
			StringAssert.Contains(windowName2, faults[0].description);
		}

		[Test]
		public void TestFault()
		{
			var windowName = "PopupWatcherTest - " + System.Diagnostics.Process.GetCurrentProcess().Id;

			var runner = new MonitorRunner("PopupWatcher", new Dictionary<string,string>
			{
				
				{ "WindowNames", windowName },
				{ "Fault", "true" },
			})
			{
				Message = m =>
				{
					// Monitor won't produce a fault unless the window is closed
					// between IterationStarting and IterationFinished

					// Window gets closed by posting a WM_CLOSE message

					var th = new Thread(() => LameWindow.Run(windowName));

					th.Start();

					if (th.Join(500))
						return;

					th.Abort();
					Assert.Fail("Window did not get closed within 500ms");
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("PopupWatcher", faults[0].detectionSource);
			Assert.AreEqual("Closed 1 popup window.", faults[0].title);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.True(faults[0].description.Contains(windowName));
		}
	}
}
