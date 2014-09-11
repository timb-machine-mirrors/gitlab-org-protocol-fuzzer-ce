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
	[Description("Causes state changes to be random.")]
	public class StateChangeRandomMutator : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static StateChangeRandomMutator()
		{
			affectDataModel = false;
			affectStateModel = true;
		}

		int _count = 0;
		uint _mutation = 0;
		int _stateCount = 0;
		Peach.Core.Random _random;

		public StateChangeRandomMutator(StateModel model)
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
			// TODO: Verify this is correct!
			_random = new Core.Random(_mutation);
		}

		public override void randomMutation(Core.Dom.StateModel obj)
		{
			_random = context.Random;
		}

		public override Core.Dom.State changeState(Core.Dom.State obj)
		{
			if (_random.NextInt32() % _stateCount == 0)
			{
				var newState = _random.Choice(obj.parent.states);

				logger.Trace("changeState: Swap {0} for {1}.", obj.name, newState.name);
				return newState;
			}

			return obj;
		}
	}
}
