using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core.Analyzers;
using Peach.Core.Agent.Monitors;
using NLog;

namespace Peach.Core.Test.Monitors
{
	[TestFixture]
	class VmwareMonitorTests
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		void RunMonitor(Dictionary<string, Variant> args)
		{
			VmwareMonitor monitor = null;

			try
			{
				monitor = new VmwareMonitor(null, "name", args);
				monitor.SessionStarting();
				monitor.IterationStarting(0, false);
				monitor.IterationFinished();
				monitor.SessionFinished();
				monitor.GetMonitorData();
			}
			catch (PeachException ex)
			{
				logger.Trace(ex.Message);
				logger.Trace(ex.StackTrace);
			}
			finally
			{
				if (monitor != null)
					monitor.StopMonitor();
			}
		}

		[Test]
		public void TestGood()
		{
			var args = new Dictionary<string, Variant>();
			args["Vmx"] = new Variant("D:\\Virtual Machines\\Windows XP Professional (Peach)\\Windows XP Professional.vmx");
			args["SnapshotIndex"] = new Variant("0");
			RunMonitor(args);
		}

		[Test]
		public void TestBadVM()
		{
			var args = new Dictionary<string, Variant>();
			args["Vmx"] = new Variant("BadVM.vmx");
			args["SnapshotIndex"] = new Variant("0");
			RunMonitor(args);
		}

		[Test]
		public void TestBadHost()
		{
			var args = new Dictionary<string, Variant>();
			args["Host"] = new Variant("BadHost");
			args["Vmx"] = new Variant("D:\\Virtual Machines\\Windows XP Professional (Peach)\\Windows XP Professional.vmx");
			args["SnapshotIndex"] = new Variant("0");
			RunMonitor(args);
		}

		[Test]
		public void TestBadIndex()
		{
			var args = new Dictionary<string, Variant>();
			args["Vmx"] = new Variant("D:\\Virtual Machines\\Windows XP Professional (Peach)\\Windows XP Professional.vmx");
			args["SnapshotIndex"] = new Variant("100");
			RunMonitor(args);
		}

		[Test]
		public void TestBadName()
		{
			var args = new Dictionary<string, Variant>();
			args["Vmx"] = new Variant("D:\\Virtual Machines\\Windows XP Professional (Peach)\\Windows XP Professional.vmx");
			args["SnapshotName"] = new Variant("FooBar");
			RunMonitor(args);
		}
	}
}
