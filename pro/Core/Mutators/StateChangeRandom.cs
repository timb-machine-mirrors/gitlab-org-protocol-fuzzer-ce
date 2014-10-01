using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NLog;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("StateChangeRandom")]
	[Description("Causes state changes to be random. The chance a state change will be modified is based on the number of states.")]
	public class StateChangeRandom : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static new readonly bool affectDataModel = false;
		public static new readonly bool affectStateModel = true;

		int _count = 0;
		uint _mutation = 0;
		int _stateCount = 0;
		Core.Dom.StateModel _model;

		public StateChangeRandom(StateModel model)
			: base(model)
		{
			_count = model.states.Count * model.states.Count;
			_stateCount = model.states.Count;
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
			_model = obj;
		}

		public override void randomMutation(Core.Dom.StateModel obj)
		{
			_model = obj;
		}

		public override Core.Dom.State changeState(Core.Dom.State currentState, Core.Dom.Action currentAction, Core.Dom.State nextState)
		{
			if (context.Random.NextInt32() % _stateCount == 0)
			{
				var newState = context.Random.Choice(_model.states);

				logger.Trace("changeState: Swap {0} for {1}.", nextState.name, newState.name);
				return newState;
			}

			return nextState;
		}
	}
}
