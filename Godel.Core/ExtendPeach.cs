﻿using System;
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
		private ScriptEngine Engine { get; set; }
		private StateModel OriginalStateModel { get; set; }

		public ExtendPeach(RunContext context)
		{
			Context = context;

			Context.engine.TestStarting += engine_TestStarting;
			Context.engine.TestFinished += engine_TestFinished;

			Peach.Core.Dom.StateModel.Starting += StateModel_Starting;
			Peach.Core.Dom.StateModel.Finished += StateModel_Finished;
			Peach.Core.Dom.State.Starting += State_Starting;
			Peach.Core.Dom.State.Finished += State_Finished;
			Peach.Core.Dom.Action.Starting += Action_Starting;
			Peach.Core.Dom.Action.Finished += Action_Finished;
		}

		void CleanupEvents()
		{
			Peach.Core.Dom.StateModel.Starting -= StateModel_Starting;
			Peach.Core.Dom.StateModel.Finished -= StateModel_Finished;
			Peach.Core.Dom.State.Starting -= State_Starting;
			Peach.Core.Dom.State.Finished -= State_Finished;
			Peach.Core.Dom.Action.Starting -= Action_Starting;
			Peach.Core.Dom.Action.Finished -= Action_Finished;
		}

		GodelContext GetExpr(params string[] names)
		{
			var name = string.Join(".", names);
			GodelContext ret;
			if (Expressions.TryGetValue(name, out ret))
				return ret;
			return null;
		}

		void engine_TestStarting(RunContext context)
		{
			var dom = context.dom as Godel.Core.Dom;
			if (dom == null || dom.godel.Count == 0)
				return;

			// Create the engine
			Engine = IronPython.Hosting.Python.CreateEngine();

			// Add any specified paths to our engine.
			ICollection<string> enginePaths = Engine.GetSearchPaths();
			foreach (string path in Peach.Core.Scripting.Paths)
				enginePaths.Add(path);
			foreach (string path in ClassLoader.SearchPaths)
				enginePaths.Add(Path.Combine(path, "Lib"));
			Engine.SetSearchPaths(enginePaths);

			// Import any modules
			var modules = new Dictionary<string, object>();
			foreach (string import in Peach.Core.Scripting.Imports)
				if (!modules.ContainsKey(import))
					modules.Add(import, Engine.ImportModule(import));

			// Create the global scope
			var scope = Engine.CreateScope();

			// Add the imports to the global scope
			foreach (var kv in modules)
				scope.SetVariable(kv.Key, kv.Value);


			foreach (var item in dom.godel)
			{
				// Pre-compile all the expressions
				item.OnTestStarting(Context, scope);
			}

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
			Engine = null;

			// Leave the events subscribed until we get a state model start with the wrong context
			//CleanupEvents();
		}

		void StateModel_Starting(Peach.Core.Dom.StateModel model)
		{
			if (model.parent.context != Context)
			{
				CleanupEvents();
				return;
			}

			// Keep a copy of the original for 'pre' variable in the post 
			OriginalStateModel = ObjectCopier.Clone(model);

			// StateModel.parent and State.parent are not copied, so fix them up
			foreach (var state in OriginalStateModel.states)
			{
				state.parent = OriginalStateModel;

				foreach (var action in state.actions)
				{
					action.parent = state;
				}
			}

			var expr = GetExpr(model.name);
			if (expr != null)
				expr.Pre(model);
		}

		void StateModel_Finished(Peach.Core.Dom.StateModel model)
		{
			var expr = GetExpr(model.name);
			if (expr != null)
				expr.Post(model, OriginalStateModel);
		}

		void State_Starting(Peach.Core.Dom.State state)
		{
			var expr = GetExpr(state.parent.name, state.name);
			if (expr != null)
				expr.Pre(state);
		}

		void State_Finished(Peach.Core.Dom.State state)
		{
			var expr = GetExpr(state.parent.name, state.name);
			if (expr != null)
			{
				var pre = OriginalStateModel.states[state.name];
				expr.Post(state, pre);
			}
		}

		void Action_Starting(Peach.Core.Dom.Action action)
		{
			var expr = GetExpr(action.parent.parent.name, action.parent.name, action.name);
			if (expr != null)
				expr.Pre(action);
		}

		void Action_Finished(Peach.Core.Dom.Action action)
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
