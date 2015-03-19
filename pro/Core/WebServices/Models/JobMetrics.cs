using System;

namespace Peach.Pro.Core.WebServices.Models
{
	public class FaultTimelineMetric
	{
		public DateTime Date { get; set; }
		public long FaultCount { get; set; }

		public FaultTimelineMetric() { }
		public FaultTimelineMetric(
			DateTime date,
			long faultCount)
		{
			Date = date;
			FaultCount = faultCount;
		}
	}

	public class BucketTimelineMetric
	{
		public string Label { get; set; }
		public long Iteration { get; set; }
		public DateTime Time { get; set; }
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
			Time = time;
			FaultCount = faultCount;
		}
	}

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
