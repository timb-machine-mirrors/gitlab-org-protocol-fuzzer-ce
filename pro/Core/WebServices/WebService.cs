using Nancy;
using System;

namespace Peach.Enterprise.WebServices
{
	public abstract class WebService : NancyModule
	{
		private WebContext context;

		public WebService(WebContext context)
			: this(context, String.Empty)
		{
		}

		public WebService(WebContext context, string modulePath)
			: base(modulePath)
		{
			this.context = context;
		}

		protected string NodeGuid
		{
			get
			{
				return context.NodeGuid;
			}
		}

		protected object Mutex
		{
			get
			{
				return context.Mutex;
			}
		}

		protected WebLogger Logger
		{
			get
			{
				return context.Logger;
			}
		}

		protected JobRunner Runner
		{
			get
			{
				return context.Runner;
			}
		}

		protected PitTester Tester
		{
			get
			{
				return context.Tester;
			}
		}

		protected PitDatabase PitDatabase
		{
			get
			{
				return new PitDatabase(context.PitLibraryPath);
			}
		}

		protected bool IsEngineRunning
		{
			get
			{
				return (Runner != null && Runner.Status != Models.JobStatus.Stopped) || (Tester != null && Tester.Status == Models.TestStatus.Active);
			}
		}

		protected void StartTest(Models.Pit pit)
		{
			context.StartTest(pit.Versions[0].Files[0].Name);
		}

		protected void StartJob(Models.Pit pit)
		{
			context.StartJob(pit.Versions[0].Files[0].Name, pit.PitUrl);
		}
	}
}
