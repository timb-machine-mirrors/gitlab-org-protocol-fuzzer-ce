using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;
using NLog;

namespace Godel.Core
{
	public class ExtendState
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ExtendState(RunContext context)
		{
			Context = context;

			State.Starting += new StateStartingEventHandler(State_Starting);
			State.Finished += new StateFinishedEventHandler(State_Finished);
			State.ChangingState += new StateChangingStateEventHandler(State_ChangingState);

			OclContextsInstances = (Dictionary<string, OCL.OclContext>)Context.stateStore["OclContexts"];
		}

		public Dictionary<string, string> OclContexts = new Dictionary<string, string>();
		public Dictionary<string, object> OclPreObjects = new Dictionary<string, object>();
		public Dictionary<string, OCL.OclContext> OclContextsInstances { get; set; }

		public Engine Engine { get; set; }
		public RunContext Context { get; set; }

		public void AttachToState(Peach.Core.Dom.State state, string contextName)
		{
			OclContexts.Add(state.name, contextName);
		}

		void State_Starting(State state)
		{
			if (!OclContexts.ContainsKey(state.name))
				return;

			string oclContext = OclContexts[state.name];
			if (OclPreObjects.ContainsKey(state.name))
				OclPreObjects.Remove(state.name);

			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");

			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(state, null, Context))
				logger.Debug("OCL: Pre-Inv: Passed. (State: " + state.name + ")");
			else
			{
				logger.Error("OCL: Pre-Inv: Expression failed. (State: " + state.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Inv expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.description = "OCL Inv expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre-Inv: Expression failed. (State: " + state.name + ")");
			}

			if (ocl.Pre(state, null, Context))
				logger.Debug("OCL: Pre: Passed. (State: " + state.name + ")");
			else
			{
				logger.Error("OCL: Pre: Expression failed. (State: " + state.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Pre expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.description = "OCL Pre expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre: Expression failed. (State: " + state.name + ")");
			}

			// need to keep a @pre!
			OclPreObjects[state.name] = ObjectCopier.Clone<State>(state);
		}

		void State_ChangingState(State state, State toState)
		{
		}

		void State_Finished(State state)
		{
			if (!OclContexts.ContainsKey(state.name))
				return;

			string oclContext = OclContexts[state.name];

			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");

			object oclPre = OclPreObjects[state.name];
			OclPreObjects.Remove(state.name);

			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(state, oclPre, Context))
				logger.Debug("OCL: Post-Inv: Passed. (State: " + state.name + ")");
			else
			{
				logger.Error("OCL: Post-Inv: Expression failed. (State: " + state.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post-Inv expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.description = "OCL Post-Inv expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post-Inv: Expression failed. (State: " + state.name + ")");
			}

			if (ocl.Post(state, oclPre, Context))
				logger.Debug("OCL: Post: Passed. (State: " + state.name + ")");
			else
			{
				logger.Error("OCL: Post: Expression failed. (State: " + state.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.description = "OCL Post expression [" + ocl.Name + "] for state [" + state.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post: Expression failed. (State: " + state.name + ")");
			}
		}
	}
}
