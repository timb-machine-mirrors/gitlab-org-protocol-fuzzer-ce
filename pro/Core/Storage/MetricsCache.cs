using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	internal class MetricsCache
	{
		class NameCache
		{
			long _lastId;
			readonly Dictionary<string, NamedItem> _map = new Dictionary<string, NamedItem>();
			List<NamedItem> _pending = new List<NamedItem>();

			public NameCache(JobDatabase db)
			{
				_map = db.LoadTable<NamedItem>().ToDictionary(x => x.Name);
				_lastId = _map.Count + 1;
			}

			public long Add(string name)
			{
				NamedItem item;
				if (!_map.TryGetValue(name, out item))
				{
					item = new NamedItem { Id = _lastId++, Name = name };
					_pending.Add(item);
					_map.Add(name, item);
				}
				return item.Id;
			}

			public IEnumerable<NamedItem> Flush()
			{
				var ret = _pending;
				_pending = new List<NamedItem>();
				return ret;
			}
		}

		readonly JobDatabaseFactory _factory;
		readonly NameCache _nameCache;
		readonly Dictionary<Tuple<long, long>, State> _stateCache;
		readonly Dictionary<Tuple<long, long>, State> _pendingStates;
		readonly List<Mutation> _mutations = new List<Mutation>();
		Mutation _mutation;

		public delegate JobDatabase JobDatabaseFactory();

		public MetricsCache(JobDatabaseFactory factory)
		{
			_factory = factory;
			_pendingStates = new Dictionary<Tuple<long, long>, State>();

			using (var db = _factory())
			{
				_nameCache = new NameCache(db);
				_stateCache = db.LoadTable<State>().ToDictionary(x =>
					new Tuple<long, long>(x.NameId, x.RunCount));
			}
		}

		public void IterationStarting(uint iteration)
		{
			//Console.WriteLine("cache.IterationStarting({0});", iteration);

			_mutations.Clear();
			_mutation = new Mutation();
		}

		public void StateStarting(string name, uint runCount)
		{
			//Console.WriteLine("cache.StateStarting(\"{0}\", {1});", name, runCount);

			var nameId = _nameCache.Add(name);
			var key = new Tuple<long, long>(nameId, runCount);
			State state;
			if (_pendingStates.TryGetValue(key, out state))
			{
				// Special case:
				// We've visited this state multiple times but we
				// haven't reached IterationFinished yet.
				state.Count++;
			}
			else if (_stateCache.TryGetValue(key, out state))
			{
				state.Count++;
				_pendingStates.Add(key, state);
			}
			else
			{
				state = new State
				{
					Id = _stateCache.Count + 1,
					NameId = nameId,
					RunCount = runCount,
					Count = 1,
				};
				_pendingStates.Add(key, state);
				_stateCache.Add(key, state);
			}
			_mutation.StateId = state.Id;
		}

		public void ActionStarting(string name)
		{
			//Console.WriteLine("cache.ActionStarting(\"{0}\");", name);

			_mutation.ActionId = _nameCache.Add(name);
		}

		public void DataMutating(
			string parameter,
			string element,
			string mutator,
			string dataset)
		{
			//Console.WriteLine("cache.DataMutating(\"{0}\", \"{1}\", \"{2}\", \"{3}\");", 
			//	parameter, element, mutator, dataset);

			_mutation.ParameterId = _nameCache.Add(parameter);
			_mutation.ElementId = _nameCache.Add(element);
			_mutation.MutatorId = _nameCache.Add(mutator);
			_mutation.DatasetId = _nameCache.Add(dataset);

			_mutations.Add(_mutation);

			_mutation = new Mutation
			{
				StateId = _mutation.StateId,
				ActionId = _mutation.ActionId,
			};
		}

		public void IterationFinished()
		{
			//Console.WriteLine("cache.IterationFinished();");

			using (var db = _factory())
			{
				using (var xact = db.Connection.BeginTransaction())
				{
					try
					{
						db.InsertNames(_nameCache.Flush());
						db.UpsertStates(_pendingStates.Values);
						db.UpsertMutations(_mutations);
						xact.Commit();
					}
					catch (Exception)
					{
						xact.Rollback();
						throw;
					}
				}
			}

			foreach (var mutation in _mutations)
			{
				mutation.IsSaved = true;
			}
			_pendingStates.Clear();
		}

		public void OnFault(FaultMetric fault)
		{
			//Console.WriteLine("cache.OnFault({0}, \"{1}\", \"{2}\", \"{3}\");", 
			//	fault.Iteration, 
			//	fault.MajorHash,
			//	fault.MinorHash,
			//	fault.Timestamp);

			using (var db = _factory())
			{
				using (var xact = db.Connection.BeginTransaction())
				{
					try
					{
						db.InsertNames(_nameCache.Flush());
						db.UpsertStates(_pendingStates.Values);
						db.UpsertMutations(_mutations.Where(x => !x.IsSaved));

						var faults = _mutations.Select(x => new FaultMetric
						{
							Iteration = fault.Iteration,
							MajorHash = fault.MajorHash,
							MinorHash = fault.MinorHash,
							Timestamp = fault.Timestamp,
							Hour = fault.Hour,
							StateId = x.StateId,
							ActionId = x.ActionId,
							ParameterId = x.ParameterId,
							ElementId = x.ElementId,
							MutatorId = x.MutatorId,
							DatasetId = x.DatasetId,
						});
						db.InsertFaultMetrics(faults);
						xact.Commit();
					}
					catch (Exception)
					{
						xact.Rollback();
						throw;
					}
				}
			}

			_pendingStates.Clear();
		}
	}
}
