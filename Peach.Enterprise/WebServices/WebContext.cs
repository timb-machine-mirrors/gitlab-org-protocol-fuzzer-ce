using Peach.Core;
using System;

namespace Peach.Enterprise.WebServices
{
	/// <summary>
	/// The context that is passed to each WebService instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class WebContext
	{
		public WebContext()
			: this(String.Empty)
		{
		}

		public WebContext(string pitLibraryPath)
		{
			Mutex = new object();
			PitLibraryPath = pitLibraryPath;
			NodeGuid = System.Guid.NewGuid().ToString().ToLower();
			Logger = new WebLogger(NodeGuid);
		}

		public object Mutex { get; private set; }

		public string PitLibraryPath { get; private set; }

		public string NodeGuid { get; private set; }

		public WebLogger Logger { get; private set; }

		public JobRunner Runner { get; private set; }

		public PitTester Tester { get; private set; }

		public void StartTest(string pitFile)
		{
			Tester = new PitTester(PitLibraryPath, pitFile);
		}

		public void StartJob(string pitFile)
		{
			Runner = JobRunner.Run(Logger, PitLibraryPath, pitFile);
		}

		public void AttachJob(RunConfiguration config)
		{
			Runner = JobRunner.Attach(config);
		}
	}
}
