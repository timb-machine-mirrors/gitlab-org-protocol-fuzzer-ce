﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
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

		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		readonly NameCache _nameCache;
		readonly Dictionary<Tuple<long, long>, State> _stateCache;
		readonly TimeSpan _runtime;
		readonly Task<Job> _task;
		readonly LinkedList<Func<Stopwatch, Job>> _queue = new LinkedList<Func<Stopwatch, Job>>();
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

			_task = Task.Factory.StartNew<Job>(BackgroundTask, job, TaskCreationOptions.LongRunning);
		}

		public Job Job { get; private set; }

		private Job BackgroundTask(object obj)
		{
			var job = (Job)obj;
			var sw = Stopwatch.StartNew();

			while (true)
			{
				var func = GetNext(sw, job);
				if (func == null)
					continue;

				var ret = func(sw);
				if (ret == null)
					return job;

				job = ret;

				lock (job)
				{
					Monitor.Pulse(job);
				}
			}
		}

		private Func<Stopwatch, Job> GetNext(Stopwatch sw, Job job)
		{
			lock (_queue)
			{
				if (_queue.Count == 0 && !Monitor.Wait(_queue, HeartBeatInterval))
				{
					// inject heartbeat record
					DoUpdateRunningJob(sw, job);
					return null;
				}

				_queueSemaphore.Release();
				var ret = _queue.First();
				_queue.RemoveFirst();
				return ret;
			}
		}

		private void CheckTask()
		{
			if (_task.IsFaulted)
			{
				Logger.Error("BackgroundTask exception: {0}".Fmt(_task.Exception.InnerException));
				throw _task.Exception;
			}
		}

		private void EnqueueFront(Func<Stopwatch, Job> func)
		{
			CheckTask();

			_queueSemaphore.Wait();
			lock (_queue)
			{
				_queue.AddFirst(func);
				_maxQueueDepth = Math.Max(_maxQueueDepth, _queue.Count);
				Monitor.Pulse(_queue);
			}
		}

		private void EnqueueBack(Func<Stopwatch, Job> func)
		{
			CheckTask();

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
				EnqueueBack(sw =>
				{
					DoUpdateRunningJob(sw, copy);
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

			EnqueueBack(sw =>
			{
				using (var db = new JobDatabase(copy.DatabasePath))
				{
					db.Transaction(() =>
					{
						db.InsertNames(names);
						db.UpsertStates(states.Values);
						db.UpsertMutations(mutations);
					});
				}
				DoUpdateRunningJob(sw, copy);
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
				EnqueueBack(sw =>
				{
					using (var db = new JobDatabase(copy.DatabasePath))
					{
						db.Transaction(() =>
						{
							db.InsertNames(names);
							db.InsertFault(detail);
							db.InsertFaultMetrics(faults);
						});
					}
					DoUpdateRunningJob(sw, copy);
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
				EnqueueFront(sw =>
				{
					_status = JobStatus.Stopping;
					sw.Stop();
					DoUpdateRunningJob(sw, copy);
					return copy;
				});
				// Wait until StopPending is received
				Monitor.Wait(copy);
			}

			lock (copy)
			{
				EnqueueBack(sw =>
				{
					copy.StopDate = now;
					copy.Mode = !copy.DryRun ? JobMode.Reporting : JobMode.Fuzzing;
					DoUpdateRunningJob(sw, copy);
					return copy;
				});
				// Wait for queue to drain
				Monitor.Wait(copy);
			}

			if (!Job.DryRun && Job.IterationCount > 0)
			{
				// use the `copy` here because it has been modified with the stopped status
				try
				{
					using (var db = new JobDatabase(Job.DatabasePath))
					{
						Debug.Assert(copy.StopDate == now);
						copy.Status = JobStatus.Stopped;
						var report = db.GetReport(copy);
						Reporting.SaveReportPdf(report);
					}
				}
				catch (Exception ex)
				{
					Logger.Error("An unexpected error occured saving the job report.", ex);

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

			EnqueueBack(sw => null);
			_task.Wait();

			Job = _task.Result;
			Job.Mode = JobMode.Fuzzing;
		}

		public void Pause()
		{
			var copy = CopyJob();
			lock (copy)
			{
				EnqueueFront(sw =>
				{
					_status = JobStatus.Paused;
					sw.Stop();
					DoUpdateRunningJob(sw, copy);
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
				EnqueueFront(sw =>
				{
					_status = JobStatus.Running;
					sw.Start();
					DoUpdateRunningJob(sw, copy);
					return copy;
				});
				Monitor.Wait(copy);
			}
		}

		private Job CopyJob()
		{
			return ObjectCopier.Clone(Job);
		}

		private void DoUpdateRunningJob(Stopwatch sw, Job job)
		{
			using (var db = new NodeDatabase())
			{
				job.HeartBeat = DateTime.Now;
				job.Runtime = _runtime + sw.Elapsed;
				job.Status = _status;
				db.UpdateRunningJob(job);
			}
		}
	}
}
