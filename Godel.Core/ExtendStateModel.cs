using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;
using NLog;

namespace Godel.Core
{
	public class ExtendStateModel
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ExtendStateModel(RunContext context)
		{
			Context = context;

			StateModel.Starting += new StateModelStartingEventHandler(StateModel_Starting);
			StateModel.Finished += new StateModelFinishedEventHandler(StateModel_Finished);

			OclContextsInstances = (Dictionary<string, OCL.OclContext>)Context.stateStore["OclContexts"];
		}

		public Dictionary<string, OCL.OclContext> OclContextsInstances { get; set; }
		public Dictionary<string, string> OclContexts = new Dictionary<string, string>();
		public Dictionary<string, object> OclPreObjects = new Dictionary<string, object>();
		public Engine Engine { get; set; }
		public RunContext Context { get; set; }

		public void AttachToStateModel(Peach.Core.Dom.StateModel state, string contextName)
		{
			OclContexts.Add(state.name, contextName);
		}

		void StateModel_Starting(StateModel model)
		{
			if (!OclContexts.ContainsKey(model.name))
				return;
			
			string oclContext = OclContexts[model.name];
			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");

			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(model, null, Context))
				logger.Debug("OCL: Pre-Inv: Passed. (StateModel: " + model.name + ")");
			else
			{
				logger.Error("OCL: Pre-Inv: Expression failed. (StateModel: " + model.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Inv expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.description = "OCL Inv expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre-Inv: Expression failed. (StateModel: " + model.name + ")");
			}

			if (ocl.Pre(model, null, Context))
				logger.Debug("OCL: Pre: Passed. (StateModel: " + model.name + ")");
			else
			{
				logger.Error("OCL: Pre: Expression failed. (StateModel: " + model.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Pre expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.description = "OCL Pre expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Pre: Expression failed. (StateModel: " + model.name + ")");
			}

			// need to keep a @pre!
			OclPreObjects[model.name] = ObjectCopier.Clone<StateModel>(model);
		}

		void StateModel_Finished(StateModel model)
		{
			// Do we have an OCL context for
			// this state?
			if (!OclContexts.ContainsKey(model.name))
				return;

			string oclContext = OclContexts[model.name];
			if (!OclContextsInstances.ContainsKey(oclContext))
				throw new PeachException("Error, unable to find OCL context name \"" + oclContext + "\".");

			object oclPre = OclPreObjects[model.name];
			OclPreObjects.Remove(model.name);
			var ocl = OclContextsInstances[oclContext];

			if (ocl.Inv(model, oclPre, Context))
				logger.Debug("OCL: Post-Inv: Passed. (StateModel: " + model.name + ")");
			else
			{
				logger.Error("OCL: Post-Inv: Expression failed. (StateModel: " + model.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post-Inv expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.description = "OCL Post-Inv expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post-Inv: Expression failed. (StateModel: " + model.name + ")");
			}

			if (ocl.Post(model, oclPre, Context))
				logger.Debug("OCL: Post: Passed. (StateModel: " + model.name + ")");
			else
			{
				logger.Error("OCL: Post: Expression failed. (StateModel: " + model.name + ")");

				Fault fault = new Fault();
				fault.detectionSource = "Godel";
				fault.type = FaultType.Fault;
				fault.title = "OCL Post expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.description = "OCL Post expression [" + ocl.Name + "] for state model [" + model.name + "] failed.";
				fault.folderName = "Godel";

				Context.faults.Add(fault);

				throw new SoftException("OCL: Post: Expression failed. (StateModel: " + model.name + ")");
			}
		}
	}
}
