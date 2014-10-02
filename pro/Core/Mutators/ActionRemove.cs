using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NLog;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("ActionRemove")]
	[Description("Causes Actions to be not executed.")]
	public class ActionRemove: Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static new readonly bool affectDataModel = false;
		public static new readonly bool affectStateModel = true;

		/// <summary>
		/// Count is actions * N
		/// </summary>
		int _count = 0;
		uint _mutation = 0;

		/// <summary>
		/// Total count of actions
		/// </summary>
		int _actionCount = 0;

		public ActionRemove(StateModel model)
			: base(model)
		{
			foreach (var state in model.states)
			{
				_actionCount += state.actions.Count;

				foreach (var action in state.actions)
				{
					if (SupportedActionType(action))
						_count++;
				}
			}
		}

		public override int count
		{
			get { return _count; }
		}

		public override uint mutation
		{
			get
			{
				return _mutation;
			}
			set
			{
				_mutation = value;
			}
		}

		public override void sequentialMutation(Core.Dom.DataElement obj)
		{
			throw new NotImplementedException();
		}

		public override void randomMutation(Core.Dom.DataElement obj)
		{
			throw new NotImplementedException();
		}

		public override void sequentialMutation(Core.Dom.StateModel obj)
		{
			int actionIndex = (int)_mutation-1;

			do
			{
				actionIndex++;
				_targetAction = null;

				foreach (var state in obj.states)
				{
					if (actionIndex >= state.actions.Count)
					{
						actionIndex -= state.actions.Count;
						continue;
					}

					_targetAction = state.actions[actionIndex];
					break;
				}

				if (_targetAction == null)
				{
					logger.Error("Ran out of actions, we should never be here.");
					return;
				}
			}
			while (!SupportedActionType(_targetAction));
		}

		Core.Dom.Action _targetAction = null;

		public override void randomMutation(Core.Dom.StateModel obj)
		{
			do
			{
				int actionIndex = context.Random.Next(_actionCount + 1);

				foreach (var state in obj.states)
				{
					if (actionIndex >= state.actions.Count)
					{
						actionIndex -= state.actions.Count;
						continue;
					}

					_targetAction = state.actions[actionIndex];
					break;
				}
			}
			while (!SupportedActionType(_targetAction));
		}

		bool SupportedActionType(Core.Dom.Action action)
		{
			if (action is Core.Dom.Actions.Call)
				return true;
			if (action is Core.Dom.Actions.GetProperty)
				return true;
			if (action is Core.Dom.Actions.Output)
				return true;
			if (action is Core.Dom.Actions.SetProperty)
				return true;

			return false;
		}

		public override Core.Dom.State changeState(State currentState, Core.Dom.Action currentAction, State nextState)
		{
			return nextState;
		}

		public override Core.Dom.Action nextAction(State state, Core.Dom.Action lastAction, Core.Dom.Action nextAction)
		{
			if (nextAction != _targetAction)
				return nextAction;

			// Skip this action entirely.
			state.MoveToNextAction();
			return state.NextAction();
		}
	}
}
