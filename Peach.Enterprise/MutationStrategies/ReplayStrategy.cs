using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Linq;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Enterprise.MutationStrategies
{
	[DefaultMutationStrategy]
	[MutationStrategy("Replay", true)]
	[Description("Replay an existing set of data sets")]
	public class ReplayStrategy : MutationStrategy
	{
		protected class DataSetTracker
		{
			public DataSetTracker(string ModelName, List<Data> Options)
			{
				this.ModelName = ModelName;
				this.Options = Options;
				this.Iteration = 1;
			}

			public string ModelName { get; private set; }
			public List<Data> Options { get; private set; }
			public uint Iteration { get; set; }
		}

		protected class DataSets : KeyedCollection<string, DataSetTracker>
		{
			protected override string GetKeyForItem(DataSetTracker item)
			{
				return item.ModelName;
			}
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		DataSets _dataSets;

		int _dataSetsIndex = 0;
		int _optionsIndex = 0;

		uint _count = 0;
		uint _iteration;

		public ReplayStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			Core.Dom.Action.Starting += new ActionStartingEventHandler(Action_Starting);
			Core.Dom.State.Starting += new StateStartingEventHandler(State_Starting);
			engine.IterationStarting += new Engine.IterationStartingEventHandler(engine_IterationStarting);
		}

		void engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				_dataSets = new DataSets();
			}
			else
			{
				// Switch data model!
			}
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			base.Finalize(context, engine);

			Core.Dom.Action.Starting -= Action_Starting;
			Core.Dom.State.Starting -= State_Starting;
			engine.IterationStarting -= engine_IterationStarting;
		}

		private uint GetSwitchIteration()
		{
			return _iteration;
		}

		public override bool UsesRandomSeed
		{
			get
			{
				return false;
			}
		}

		public override bool IsDeterministic
		{
			get
			{
				return true;
			}
		}

		public override uint Iteration
		{
			get
			{
				return _iteration;
			}
			set
			{
				_iteration = value;

				if (!Context.controlIteration)
				{
					if (_dataSetsIndex < 0)
					{
						_dataSetsIndex = 0;
					}
					else
					{
						_optionsIndex++;

						if (_optionsIndex >= _dataSets[_dataSetsIndex].Options.Count)
						{
							_dataSetsIndex++;
							_optionsIndex = 0;

							if (_dataSetsIndex >= _dataSets.Count)
								throw new ApplicationException("Out of data sets!");
						}
					}
				}
			}
		}

		void Action_Starting(Core.Dom.Action action)
		{
			// Is this a supported action?
			if (!(action.outputData.Any()))
				return;

			if(Context.controlRecordingIteration)
				RecordDataSet(action);
			else if(!Context.controlIteration)
				SyncDataSet(action);
		}

		void State_Starting(State state)
		{
		}

		private void SyncDataSet(Peach.Core.Dom.Action action)
		{
			System.Diagnostics.Debug.Assert(_iteration != 0);

			foreach (var item in action.outputData)
			{
				// Note: use the model name, not the instance name so
				// we only set the data set once for re-enterant states.
				var modelName = item.modelName;

				if (!_dataSets.Contains(modelName))
					return;

				var val = _dataSets[modelName];
				var opt = val.Options[_optionsIndex];

				// Don't try cracking files, just overwrite the entire data model
				var fileOpt = opt as DataFile;
				if (fileOpt != null)
				{
					try
					{
						var bs = new BitStream(File.OpenRead(fileOpt.FileName));

						item.dataModel.MutatedValue = new Variant(bs);
						item.dataModel.mutationFlags = MutateOverride.TypeTransform;
					}
					catch (IOException ex)
					{
						logger.Debug(ex.Message);
						logger.Debug("Unable to apply data from '{0}', ignoring.", fileOpt.FileName);
					}
				}
				else
				{
					try
					{
						item.Apply(opt);
					}
					catch (PeachException ex)
					{
						logger.Debug(ex.Message);
						logger.Debug("Unable to apply data '{0}', ignoring.", opt.name);
					}
				}
			}
		}

		private void RecordDataSet(Core.Dom.Action action)
		{
			foreach (var item in action.outputData)
			{
				var options = item.allData.ToList();

				if (options.Count > 0)
				{
					// Don't use the instance name here, we only pick the data set
					// once per state, not each time the state is re-entered.
					var rec = new DataSetTracker(item.modelName, options);

					if (!_dataSets.Contains(item.modelName))
						_dataSets.Add(rec);
				}
			}
		}

		public override uint Count
		{
			get
			{
				return _count-1;
			}
		}
	}
}

// end
