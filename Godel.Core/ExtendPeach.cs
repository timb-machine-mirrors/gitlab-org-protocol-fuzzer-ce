using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;

namespace Godel.Core
{
	public class ExtendPeach
	{
		public ExtendAction ExtendAction { get; set; }
		public ExtendState ExtendState { get; set; }
		public ExtendStateModel ExtendStateModel { get; set; }
		public RunContext Context { get; set; }

		public ExtendPeach(RunContext context)
		{
			context.stateStore["OclContexts"] = new Dictionary<string, OCL.OclContext>();
			Context = context;
			ExtendAction = new ExtendAction(context);
			ExtendState = new ExtendState(context);
			ExtendStateModel = new ExtendStateModel(context);
		}
	}
}
