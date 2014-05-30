using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython;
using IronPython.Hosting;
using System.IO;

namespace Godel.Core
{
	public class ExtendPeach
	{
		private RunContext Context { get; set; }
		private NamedCollection<GodelContext> Expressions { get; set; }
		private StateModel OriginalStateModel { get; set; }

		public ExtendPeach(RunContext context)
		{
			Context = context;

			Context.engine.TestStarting += engine_TestStarting;
			Context.engine.TestFinished += engine_TestFinished;

			Context.StateModelStarting += StateModel_Starting;
			Context.StateModelFinished += StateModel_Finished;
			Context.StateStarting += State_Starting;
			Context.StateFinished += State_Finished;
			Context.ActionStarting += Action_Starting;
			Context.ActionFinished += Action_Finished;
		}

		GodelContext GetExpr(params string[] names)
		{
			var name = string.Join(".", names);
			GodelContext ret;
			if (Expressions != null && Expressions.TryGetValue(name, out ret))
				return ret;
			return null;
		}

		void engine_TestStarting(RunContext context)
		{
			var dom = context.dom as Godel.Core.Dom;
			if (dom == null || dom.godel.Count == 0)
				return;

			// Create a script scope
			var scope = context.dom.Python.CreateScope();

			// Pre-compile all the expressions
			foreach (var item in dom.godel)
				item.OnTestStarting(Context, scope);

			Expressions = dom.godel;
		}

		void engine_TestFinished(RunContext context)
		{
			if (Expressions != null)
			{
				foreach (var item in Expressions)
					item.OnTestFinished();
			}

			OriginalStateModel = null;
			Expressions = null;
		}

		void StateModel_Starting(RunContext context, Peach.Core.Dom.StateModel model)
		{
			System.Diagnostics.Debug.Assert(model.parent.context == Context);

			if (Expressions == null)
				return;

			// Keep a copy of the original for 'pre' variable in the post 
			OriginalStateModel = ObjectCopier.Clone(model);

			var expr = GetExpr(model.name);
			if (expr != null)
				expr.Pre(model);
		}

		void StateModel_Finished(RunContext context, Peach.Core.Dom.StateModel model)
		{
			var expr = GetExpr(model.name);
			if (expr != null)
				expr.Post(model, OriginalStateModel);
		}

		void State_Starting(RunContext context, Peach.Core.Dom.State state)
		{
			var expr = GetExpr(state.parent.name, state.name);
			if (expr != null)
				expr.Pre(state);
		}

		void State_Finished(RunContext context, Peach.Core.Dom.State state)
		{
			var expr = GetExpr(state.parent.name, state.name);
			if (expr != null)
			{
				var pre = OriginalStateModel.states[state.name];
				expr.Post(state, pre);
			}
		}

		void Action_Starting(RunContext context, Peach.Core.Dom.Action action)
		{
			var expr = GetExpr(action.parent.parent.name, action.parent.name, action.name);
			if (expr != null)
				expr.Pre(action);
		}

		void Action_Finished(RunContext context, Peach.Core.Dom.Action action)
		{
			var expr = GetExpr(action.parent.parent.name, action.parent.name, action.name);
			if (expr != null)
			{
				var pre = OriginalStateModel.states[action.parent.name].actions[action.name];
				expr.Post(action, pre);
			}
		}
	}
}
