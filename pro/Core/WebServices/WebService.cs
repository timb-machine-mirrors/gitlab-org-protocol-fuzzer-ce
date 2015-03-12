using System;
using Nancy;

namespace Peach.Pro.Core.WebServices
{
	public abstract class WebService : NancyModule
	{
		private readonly WebContext _context;

		protected WebService(WebContext context)
			: this(context, String.Empty)
		{
		}

		protected WebService(WebContext context, string modulePath)
			: base(modulePath)
		{
			_context = context;
		}

		protected string NodeGuid
		{
			get { return _context.NodeGuid; }
		}

		protected string PitLibraryPath
		{
			get { return _context.PitLibraryPath; }
		}

		//protected object Mutex
		//{
		//	get
		//	{
		//		return _context.Mutex;
		//	}
		//}

		//protected WebLogger Logger
		//{
		//	get
		//	{
		//		return context.Logger;
		//	}
		//}

		//protected JobRunner Runner
		//{
		//	get
		//	{
		//		return _context.Runner;
		//	}
		//}

		//protected PitTester Tester
		//{
		//	get { return _context.Tester; }
		//}

		protected PitDatabase PitDatabase
		{
			get { return new PitDatabase(_context.PitLibraryPath); }
		}

		//protected bool IsEngineRunning
		//{
		//	get
		//	{
		//		return (Runner != null && Runner.Status != Models.JobStatus.Stopped) 
		//			|| (Tester != null && Tester.Status == Models.TestStatus.Active);
		//	}
		//}

		//protected void StartTest(Models.Pit pit)
		//{
		//	_context.StartTest(pit.Versions[0].Files[0].Name);
		//}
	}
}
