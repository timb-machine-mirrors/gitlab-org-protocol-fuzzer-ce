using Peach.Core;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class WebLogger : Logger
	{
		public string NodeGuid { get; private set; }
		public string JobGuid { get; private set; }
		public string Name { get; private set; }
		public uint Seed { get; private set; }
		public uint CurrentIteration { get; private set; }
		public uint StartIteration { get; private set; }
		public uint FaultCount { get; private set; }
		public DateTime StartDate { get; private set; }

		public WebLogger()
		{
			NodeGuid = Guid.NewGuid().ToString().ToLower();
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			lock (this)
			{
				JobGuid = System.Guid.NewGuid().ToString().ToLower();
				Name = context.config.pitFile;
				Seed = context.config.randomSeed;
				CurrentIteration = context.currentIteration;
				CurrentIteration = StartIteration;
				FaultCount = 0;
				StartDate = context.config.runDateTime.ToUniversalTime();
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			lock (this)
			{
				JobGuid = null;
				Name = null;
				Seed = 0;
				CurrentIteration = 0;
				CurrentIteration = 0;
				FaultCount = 0;
				StartDate = DateTime.MinValue;
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			CurrentIteration = currentIteration;
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			// Caught fault, trying to reproduce
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			// Reproducable
			++FaultCount;
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			// Non-reproducable
			++FaultCount;
		}
	}
}
