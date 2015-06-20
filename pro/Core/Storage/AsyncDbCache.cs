﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Core.Storage
{
	internal class AsyncDbCache
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

		const int HeartBeatInterval = 1000;

		readonly NameCache _nameCache;
		readonly Dictionary<Tuple<long, long>, State> _stateCache;
		readonly TimeSpan _runtime;
		readonly Task<Job> _task;
		readonly LinkedList<Func<JobDatabase, Stopwatch, Job>> _queue = new LinkedList<Func<JobDatabase, Stopwatch, Job>>();
		readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(10);

		Dictionary<Tuple<long, long>, State> _pendingStates;
		List<Mutation> _mutations;
		long _nextStateId;
		Mutation _mutation;
		int _maxQueueDepth;
		JobStatus _status;

		public AsyncDbCache(Job job)
		{
			_status = job.Status;
			Job = job;

			using (var db = new JobDatabase(job.DatabasePath))
			{
				_nameCache = new NameCache(db);
				_stateCache = db.LoadTable<State>().ToDictionary(x =>
					new Tuple<long, long>(x.NameId, x.RunCount));
			}

			// Remember previous runtime so it properly accumulates on restarted jobs
			_runtime = job.Runtime;

			_task = Task.Factory.StartNew<Job>(BackgroundTask, job);
		}

		public Job Job { get; private set; }

		private Job BackgroundTask(object obj)
		{
			var job = (Job)obj;
			var sw = Stopwatch.StartNew();

			while (true)
			{
				using (var db = new JobDatabase(job.DatabasePath))
				{
					var func = GetNext(db, sw, job);
					if (func == null)
						continue;

					using (var xact = db.Connection.BeginTransaction())
					{
						var ret = func(db, sw);
						if (ret == null)
							return job;
						xact.Commit();

						job = ret;
					}

					lock (job)
					{
						Monitor.Pulse(job);
					}
				}
			}
		}

		private Func<JobDatabase, Stopwatch, Job> GetNext(JobDatabase db, Stopwatch sw, Job job)
		{
			lock (_queue)
			{
				if (_queue.Count == 0 && !Monitor.Wait(_queue, HeartBeatInterval))
				{
					// inject heartbeat record
					DoUpdateRunningJob(db, sw, job);
					return null;
				}

				_queueSemaphore.Release();
				var ret = _queue.First();
				_queue.RemoveFirst();
				return ret;
			}
		}

		private void EnqueueFront(Func<JobDatabase, Stopwatch, Job> func)
		{
			_queueSemaphore.Wait();
			lock (_queue)
			{
				_queue.AddFirst(func);
				_maxQueueDepth = Math.Max(_maxQueueDepth, _queue.Count);
				Monitor.Pulse(_queue);
			}
		}

		private void EnqueueBack(Func<JobDatabase, Stopwatch, Job> func)
		{
			_queueSemaphore.Wait();

			lock (_queue)
			{
				_queue.AddLast(func);
				_maxQueueDepth = Math.Max(_maxQueueDepth, _queue.Count);
				Monitor.Pulse(_queue);
			}
		}

		public void IterationStarting(JobMode newMode)
		{
			//Console.WriteLine("cache.IterationStarting({0});", newMode);

			_pendingStates = new Dictionary<Tuple<long, long>, State>();
			_mutations = new List<Mutation>();
			_mutation = new Mutation();

			_nextStateId = _stateCache.Count + 1;

			if (newMode != Job.Mode)
			{
				Job.Mode = newMode;
				var copy = CopyJob();
				EnqueueBack((db, sw) =>
				{
					DoUpdateRunningJob(db, sw, copy);
					return copy;
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

		public void IterationFinished()
		{
			//Console.WriteLine("cache.IterationFinished();");

			Job.IterationCount++;
			var copy = CopyJob();

			var names = _nameCache.Flush();
			var states = _pendingStates;
			var mutations = _mutations;

			foreach (var kv in _pendingStates)
				_stateCache[kv.Key] = kv.Value;

			EnqueueBack((db, sw) =>
			{
				DoUpdateRunningJob(db, sw, copy);
				db.InsertNames(names);
				db.UpsertStates(states.Values);
				db.UpsertMutations(mutations);
				return copy;
			});
		}

		public void OnFault(FaultDetail detail)
		{
			//Console.WriteLine("cache.OnFault({0}, \"{1}\", \"{2}\", \"{3}\");",
			//	detail.Iteration,
			//	detail.MajorHash,
			//	detail.MinorHash,
			//	detail.TimeStamp);

			Job.FaultCount++;
			var copy = CopyJob();

			var fault = new FaultMetric
			{
				Iteration = detail.Iteration,
				MajorHash = detail.MajorHash,
				MinorHash = detail.MinorHash,
				Timestamp = detail.TimeStamp,
				Hour = detail.TimeStamp.Hour,
			};

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

			// Ensure that the queue is drained before proceeding
			lock (copy)
			{
				EnqueueBack((db, sw) =>
				{
					DoUpdateRunningJob(db, sw, copy);
					db.InsertNames(names);
					db.InsertFault(detail);
					db.InsertFaultMetrics(faults);
					return copy;
				});

				Monitor.Wait(copy);
			}
		}

		public void TestFinished()
		{
			var now = DateTime.Now;

			var copy = CopyJob();
			lock (copy)
			{
				EnqueueFront((db, sw) =>
				{
					_status = JobStatus.StopPending;
					sw.Stop();
					DoUpdateRunningJob(db, sw, copy);
					return copy;
				});
				// Wait until StopPending is received
				Monitor.Wait(copy);
			}

			lock (copy)
			{
				EnqueueBack((db, sw) =>
				{
					_status = JobStatus.Stopped;
					copy.StopDate = now;
					copy.Mode = !copy.IsControlIteration ? JobMode.Reporting : JobMode.Fuzzing;
					DoUpdateRunningJob(db, sw, copy);
					return copy;
				});
				// Wait for queue to drain
				Monitor.Wait(copy);
			}

			if (!Job.IsControlIteration)
			{
				// use the `copy` here because it has been modified with the stopped status
				try
				{
					using (var db = new JobDatabase(Job.DatabasePath))
					{
						var report = db.GetReport(copy);
						Reporting.SaveReportPdf(report);
					}
				}
				catch (Exception)
				{
					//Logger.Debug("An unexpected error occured saving the job report.", ex);

					try
					{
						if (File.Exists(Job.ReportPath))
							File.Delete(Job.ReportPath);
					}
					// ReSharper disable once EmptyGeneralCatchClause
					catch
					{
					}
				}
			}

			EnqueueBack((db, sw) => null);
			_task.Wait();

			Job = _task.Result;
			using (var db = new JobDatabase(Job.DatabasePath))
			{
				db.Connection.Execute(Sql.UpdateJob, Job);
			}
		}

		public void Pause()
		{
			var copy = CopyJob();
			lock (copy)
			{
				EnqueueFront((db, sw) =>
				{
					_status = JobStatus.Paused;
					sw.Stop();
					DoUpdateRunningJob(db, sw, copy);
					return copy;
				});
				Monitor.Wait(copy);
			}
		}

		public void Continue()
		{
			var copy = CopyJob();
			lock (copy)
			{
				EnqueueFront((db, sw) =>
				{
					_status = JobStatus.Running;
					sw.Start();
					DoUpdateRunningJob(db, sw, copy);
					return copy;
				});
				Monitor.Wait(copy);
			}
		}

		private Job CopyJob()
		{
			return ObjectCopier.Clone(Job);
		}

		private void DoUpdateRunningJob(JobDatabase db, Stopwatch sw, Job job)
		{
			job.HeartBeat = DateTime.Now;
			job.Runtime = _runtime + sw.Elapsed;
			job.Status = _status;
			db.Connection.Execute(Sql.UpdateRunningJob, job);
		}
	}
}