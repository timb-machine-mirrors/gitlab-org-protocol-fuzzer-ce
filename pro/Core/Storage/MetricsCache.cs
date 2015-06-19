using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Monitor = System.Threading.Monitor;

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

		readonly NameCache _nameCache;
		readonly Dictionary<Tuple<long, long>, State> _stateCache;
		Dictionary<Tuple<long, long>, State> _pendingStates;
		List<Mutation> _mutations;
		long _nextStateId;
		Mutation _mutation;
		readonly JobDatabase _db;
		const int HeartBeatInterval = 1000;
		readonly Stopwatch _stopwatch = Stopwatch.StartNew();
		readonly TimeSpan _runtime;
		private readonly Task _task;
		private readonly Queue<Func<bool>> _queue = new Queue<Func<bool>>();
		private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(100);
		private int _maxQueueDepth;

		public MetricsCache(Job job)
		{
			_db = new JobDatabase(job.DatabasePath);

			_nameCache = new NameCache(_db);
			_stateCache = _db.LoadTable<State>().ToDictionary(x =>
				new Tuple<long, long>(x.NameId, x.RunCount));

			// Remember previous runtime so it properly accumulates on restarted jobs
			_runtime = job.Runtime;

			_task = Task.Factory.StartNew(Executor);
		}

		private void Executor()
		{
			var more = true;
			while (more)
			{
				var func = GetNext();
				if (func == null)
					continue;

				using (var xact = _db.Connection.BeginTransaction())
				{
					more = func();
					xact.Commit();
				}
			}
		}

		private Func<bool> GetNext()
		{
			lock (_queue)
			{
				if (_queue.Count == 0 && !Monitor.Wait(_queue, HeartBeatInterval))
				{
					// inject heartbeat record
					return null;
				}

				_queueSemaphore.Release();
				return _queue.Dequeue();
			}
		}

		private void Enqueue(Func<bool> func)
		{
			_queueSemaphore.Wait();

			lock (_queue)
			{
				_queue.Enqueue(func);
				_maxQueueDepth = Math.Max(_maxQueueDepth, _queue.Count);
				Monitor.Pulse(_queue);
			}
		}

		public void IterationStarting(JobMode newMode, Job job)
		{
			//Console.WriteLine("cache.IterationStarting({0});", iteration);

			_pendingStates = new Dictionary<Tuple<long, long>, State>();
			_mutations = new List<Mutation>();
			_mutation = new Mutation();

			_nextStateId = _stateCache.Count + 1;

			if (newMode != job.Mode)
			{
				job.Mode = newMode;
				var copy = CopyJob(job);
				Enqueue(() =>
				{
					UpdateRunningJob(copy);
					return true;
				});
			}
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
				state = new State
				{
					Id = state.Id,
					NameId = state.NameId,
					RunCount = state.RunCount,
					Count = state.Count + 1,
				};
				_pendingStates.Add(key, state);
			}
			else
			{
				state = new State
				{
					Id = _nextStateId++,
					NameId = nameId,
					RunCount = runCount,
					Count = 1,
				};
				_pendingStates.Add(key, state);
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

		public void IterationFinished(Job job)
		{
			//Console.WriteLine("cache.IterationFinished();");
			job.IterationCount++;
			var copy = CopyJob(job);

			var names = _nameCache.Flush();
			var states = _pendingStates;
			var mutations = _mutations;

			foreach (var kv in _pendingStates)
				_stateCache[kv.Key] = kv.Value;

			Enqueue(() =>
			{
				UpdateRunningJob(copy);
				_db.InsertNames(names);
				_db.UpsertStates(states.Values);
				_db.UpsertMutations(mutations);
				return true;
			});
		}

		public void OnFault(Job job, FaultDetail detail)
		{
			job.FaultCount++;
			var copy = CopyJob(job);

			var fault = new FaultMetric
			{
				Iteration = detail.Iteration,
				MajorHash = detail.MajorHash,
				MinorHash = detail.MinorHash,
				Timestamp = detail.TimeStamp,
				Hour = detail.TimeStamp.Hour,
			};

			//Console.WriteLine("cache.OnFault({0}, \"{1}\", \"{2}\", \"{3}\");", 
			//	fault.Iteration, 
			//	fault.MajorHash,
			//	fault.MinorHash,
			//	fault.Timestamp);
			var names = _nameCache.Flush();

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

			// This is to ensure that the queue is drained
			lock (copy)
			{
				Enqueue(() =>
				{
					UpdateRunningJob(copy);
					_db.InsertNames(names);
					_db.InsertFault(detail);
					_db.InsertFaultMetrics(faults);

					lock (copy)
					{
						Monitor.Pulse(copy);
					}
					return true;
				});

				Monitor.Wait(copy);
			}
		}

		public Report GetReport(Job job)
		{
			return _db.GetReport(job);
		}

		public void TestFinished(Job job)
		{
			job.Runtime = _runtime + _stopwatch.Elapsed;
			job.StopDate = DateTime.Now;
			job.HeartBeat = job.StopDate;
			job.Mode = JobMode.Fuzzing;
			// TODO: we should probably have a new state until report is done
			job.Status = JobStatus.Stopped;

			Enqueue(() => false);
			_task.Wait();

			_db.Connection.Execute(Sql.UpdateJob, job);
		}

		public void Pause()
		{
		}

		public void Continue()
		{
		}

		private Job CopyJob(Job job)
		{
			return new Job
			{
				Id = job.Id,
				IterationCount = job.IterationCount,
				FaultCount = job.FaultCount,
				Status = job.Status,
				Mode = job.Mode,
				Runtime = job.Runtime,
			};
		}

		private void UpdateRunningJob(Job job)
		{
			job.HeartBeat = DateTime.Now;
			_db.Connection.Execute(Sql.UpdateRunningJob, job);
		}
	}
}
