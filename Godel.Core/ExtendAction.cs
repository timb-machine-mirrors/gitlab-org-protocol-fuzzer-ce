using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;
using NLog;

namespace Godel.Core
{
#if DISABLED
	public class ExtendAction
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ExtendAction(RunContext context)
		{
			Context = context;

			Peach.Core.Dom.Action.Starting += new ActionStartingEventHandler(Action_Starting);
			Peach.Core.Dom.Action.Finished += new ActionFinishedEventHandler(Action_Finished);

			OclContextsInstances = (Dictionary<string, OCL.OclContext>)Context.stateStore["OclContexts"];
		}

		public void AttachToAction(Peach.Core.Dom.Action action, string contextName)
		{
			OclContexts.Add(action.name, contextName);
		}

		public Dictionary<string, OCL.OclContext> OclContextsInstances { get; set; }
		public Dictionary<string, string> OclContexts = new Dictionary<string, string>();
		public Dictionary<string, object> OclPreObjects = new Dictionary<string, object>();
		public Engine Engine { get; set; }
		public RunContext Context { get; set; }

		void Action_Starting(Peach.Core.Dom.Action action)
		{
			if (!OclContexts.ContainsKey(action.name))
				return;

			string oclContext = OclContexts[action.name];
			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");
			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(action, null, Context))
				logger.Debug("OCL: Pre-Inv: Passed. (Action: " + action.name + ")");
			else
			{
				logger.Error("OCL: Pre-Inv: Expression failed. (Action: " + action.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Inv expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.description = "OCL Inv expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre-Inv: Expression failed. (Action: " + action.name + ")");
			}

			if (ocl.Pre(action, null, Context))
				logger.Debug("OCL: Pre: Passed. (Action: " + action.name + ")");
			else
			{
				logger.Error("OCL: Pre: Expression failed. (Action: " + action.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Pre expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.description = "OCL Pre expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre: Expression failed. (Action: " + action.name + ")");
			}

			// need to keep a @pre!
			OclPreObjects[action.name] = ObjectCopier.Clone<Peach.Core.Dom.Action>(action);
		}

		void Action_Finished(Peach.Core.Dom.Action action)
		{
			if (!OclContexts.ContainsKey(action.name))
				return;

			string oclContext = OclContexts[action.name];
			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");

			var oclPre = OclPreObjects[action.name];
			OclPreObjects.Remove(action.name);
			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(action, oclPre, Context))
				logger.Debug("OCL: Post-Inv: Passed. (Action: " + action.name + ")");
			else
			{
				logger.Error("OCL: Post-Inv: Expression failed. (Action: " + action.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post-Inv expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.description = "OCL Post-Inv expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post-Inv: Expression failed. (Action: " + action.name + ")");
			}

			if (ocl.Post(action, oclPre, Context))
				logger.Debug("OCL: Post: Passed. (Action: " + action.name + ")");
			else
			{
				logger.Error("OCL: Post: Expression failed. (Action: " + action.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.description = "OCL Post expression [" + ocl.Name + "] for action [" + action.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post: Expression failed. (Action: " + action.name + ")");
			}
		}
	}
#endif
}

// end
