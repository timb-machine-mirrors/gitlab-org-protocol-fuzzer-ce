using System;
using Nancy;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

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

		protected PitDatabase PitDatabase
		{
			get { return new PitDatabase(_context.PitLibraryPath); }
		}

		protected IJobMonitor JobMonitor
		{
			get { return _context.JobMonitor; }
		}
	}
}
