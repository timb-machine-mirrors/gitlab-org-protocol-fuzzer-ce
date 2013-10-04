using System;
using System.IO;
using System.Collections.Generic;
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
		class DataSetTracker
		{
			public List<Data> options = new List<Data>();
			public uint iteration = 1;
		};

		protected class ElementId : Tuple<string, string>
		{
			public ElementId(string modelName, string elementName)
				: base(modelName, elementName)
			{
			}

			public string ModelName { get { return Item1; } }
			public string ElementName { get { return Item2; } }
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		OrderedDictionary<string, DataSetTracker> _dataSets;

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
				_dataSets = new OrderedDictionary<string, DataSetTracker>();
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

						if (_optionsIndex >= _dataSets[_dataSetsIndex].options.Count)
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
			if (!(action.type == ActionType.Output || action.type == ActionType.SetProperty || action.type == ActionType.Call))
				return;

			if(Context.controlRecordingIteration)
				RecordDataSet(action);
			else if(!Context.controlIteration)
				SyncDataSet(action);
		}

		void State_Starting(State state)
		{
		}

		private DataModel ApplyFileData(Peach.Core.Dom.Action action, Data data)
		{
			byte[] fileBytes = null;

			for (int i = 0; i < 5 && fileBytes == null; ++i)
			{
				try
				{
					fileBytes = File.ReadAllBytes(data.FileName);
				}
				catch (Exception ex)
				{
					logger.Debug("Failed to open '{0}'. {1}", data.FileName, ex.Message);
				}
			}

			if (fileBytes == null)
				throw new CrackingFailure(null, null);

			// Note: We need to find the origional data model to use.  Re-using
			// a data model that has been cracked into will fail in odd ways.
			var dataModel = GetNewDataModel(action);

			dataModel.MutatedValue = new Variant(fileBytes);
			dataModel.mutationFlags = MutateOverride.TypeTransform;

			return dataModel;
		}

		private DataModel ApplyFieldData(Peach.Core.Dom.Action action, Data data)
		{
			// Note: We need to find the origional data model to use.  Re-using
			// a data model that has been cracked into will fail in odd ways.
			var dataModel = GetNewDataModel(action);

			// Apply the fields
			data.ApplyFields(dataModel);

			return dataModel;
		}

		private DataModel GetNewDataModel(Peach.Core.Dom.Action action)
		{
			var referenceName = action.dataModel.referenceName;
			if (referenceName == null)
				referenceName = action.dataModel.name;

			var sm = action.parent.parent;
			var dom = _context.dom;

			int i = sm.name.IndexOf(':');
			if (i > -1)
			{
				string prefix = sm.name.Substring(0, i);

				Peach.Core.Dom.Dom other;
				if (!_context.dom.ns.TryGetValue(prefix, out other))
					throw new PeachException("Unable to locate namespace '" + prefix + "' in state model '" + sm.name + "'.");

				dom = other;
			}

			// Need to take namespaces into account when searching for the model
			var baseModel = dom.getRef<DataModel>(referenceName, a => a.dataModels);

			var dataModel = baseModel.Clone() as DataModel;
			dataModel.isReference = true;
			dataModel.referenceName = referenceName;

			return dataModel;
		}

		private void SyncDataSet(Peach.Core.Dom.Action action)
		{
			System.Diagnostics.Debug.Assert(_iteration != 0);

			// Only sync <Data> elements if the action has a data model
			if (action.dataModel == null)
				return;

			string key = GetDataModelName(action);

			if (_dataSets.IndexOfKey(key) != _dataSetsIndex)
				return;

			DataSetTracker val = null;
			if (!_dataSets.TryGetValue(key, out val))
				return;

			DataModel dataModel = null;
			Data option = val.options[_optionsIndex];

			if (option.DataType == DataType.File)
			{
				dataModel = ApplyFileData(action, option);
			}
			else if (option.DataType == DataType.Fields)
			{
				try
				{
					dataModel = ApplyFieldData(action, option);
				}
				catch (PeachException)
				{
					logger.Debug("Removing " + option.name + " from sample list.  Unable to apply fields.");
					val.options.Remove(option);
				}
			}

			if (dataModel == null)
				return;

			// Set new data model
			action.dataModel = dataModel;

			// Generate all values;
			var ret = action.dataModel.Value;
			System.Diagnostics.Debug.Assert(ret != null);

			// Store copy of new origional data model
			action.origionalDataModel = action.dataModel.Clone() as DataModel;
		}

		private void RecordDataSet(Core.Dom.Action action)
		{
			if (action.dataSet != null)
			{
				DataSetTracker val = new DataSetTracker();
				foreach (var item in action.dataSet.Datas)
				{
					switch (item.DataType)
					{
						case DataType.File:
							val.options.Add(item);
							_count++;
							break;
						case DataType.Files:
							val.options.AddRange(item.Files.Select(a => new Data() { DataType = DataType.File, FileName = a }));
							_count += (uint) item.Files.Count;
							break;
						case DataType.Fields:
							val.options.Add(item);
							_count++;
							break;
						default:
							throw new PeachException("Unexpected DataType: " + item.DataType.ToString());
					}
				}

				if (val.options.Count > 0)
				{
					// Need to properly support more than one action that are unnamed
					string key = GetDataModelName(action);
					System.Diagnostics.Debug.Assert(!_dataSets.ContainsKey(key));
					_dataSets.Add(key, val);
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
