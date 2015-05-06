﻿using System;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	[Table("ViewFaultTimeline")]
	public class FaultTimelineMetric
	{
		private DateTime _date;
		public DateTime Date
		{
			get { return _date; }
			set { _date = value.MakeUtc(); }
		}

		public long FaultCount { get; set; }

		public FaultTimelineMetric() { }
		public FaultTimelineMetric(
			DateTime date,
			long faultCount)
		{
			_date = date;
			FaultCount = faultCount;
		}
	}

	[Table("ViewBucketTimeline")]
	public class BucketTimelineMetric
	{
		public string Label { get; set; }
		public long Iteration { get; set; }

		private DateTime _time;
		public DateTime Time
		{
			get { return _time; }
			set { _time = value.MakeUtc(); }
		}

		public long FaultCount { get; set; }

		public BucketTimelineMetric() { }
		public BucketTimelineMetric(
			string label,
			long iteration,
			DateTime time,
			long faultCount)
		{
			Label = label;
			Iteration = iteration;
			_time = time;
			FaultCount = faultCount;
		}
	}

	[Table("ViewMutators")]
	public class MutatorMetric
	{
		public string Mutator { get; set; }
		public long ElementCount { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public MutatorMetric() { }
		public MutatorMetric(
			string mutator,
			long elementCount,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			Mutator = mutator;
			ElementCount = elementCount;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewElements")]
	public class ElementMetric
	{
		public string State { get; set; }
		public string Action { get; set; }
		public string Parameter { get; set; }
		public string Dataset { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public ElementMetric() { }
		public ElementMetric(
			string state,
			string action,
			string parameter,
			string dataset,
			string element,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			State = state;
			Action = action;
			Parameter = parameter;
			Dataset = dataset;
			Element = element;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewStates")]
	public class StateMetric
	{
		public string State { get; set; }
		public long ExecutionCount { get; set; }

		public StateMetric() { }
		public StateMetric(
			string state,
			long executionCount)
		{
			State = state;
			ExecutionCount = executionCount;
		}
	}

	[Table("ViewDatasets")]
	public class DatasetMetric
	{
		public string Dataset { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public DatasetMetric() { }
		public DatasetMetric(
			string dataset,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			Dataset = dataset;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewBuckets")]
	public class BucketMetric
	{
		public string Bucket { get; set; }
		public string Mutator { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long FaultCount { get; set; }

		public BucketMetric() { }
		public BucketMetric(
			string bucket,
			string mutator,
			string element,
			long iterationCount,
			long faultCount)
		{
			Bucket = bucket;
			Mutator = mutator;
			Element = element;
			IterationCount = iterationCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewIterations")]
	public class IterationMetric
	{
		public string State { get; set; }
		public string Action { get; set; }
		public string Parameter { get; set; }
		public string Element { get; set; }
		public string Mutator { get; set; }
		public string Dataset { get; set; }
		public long IterationCount { get; set; }

		public IterationMetric() { }
		public IterationMetric(
			string state,
			string action,
			string parameter,
			string element,
			string mutator,
			string dataset,
			long iterationCount)
		{
			State = state;
			Action = action;
			Parameter = parameter;
			Element = element;
			Mutator = mutator;
			Dataset = dataset;
			IterationCount = iterationCount;
		}
	}
}
