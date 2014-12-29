using System;

namespace Peach.Pro.Core.WebServices.Models
{
	public class FaultTimelineMetric
	{
		public DateTime Date { get; set; }
		public long FaultCount { get; set; }
	}

	public class BucketTimelineMetric
	{
		public long ID { get; set; }
		public string Label { get; set; }
		public long Iteration { get; set; }
		public DateTime Time { get; set; }
		public long FaultCount { get; set; }
	}

	public class MutatorMetric
	{
		public string Mutator { get; set; }
		public long ElementCount { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }
	}

	public class ElementMetric
	{
		public string State { get; set; }
		public string Action { get; set; }
		public string Parameter { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }
	}

	public class StateMetric
	{
		public string State { get; set; }
		public long ExecutionCount { get; set; }
	}

	public class DatasetMetric
	{
		public string Dataset { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }
	}

	public class BucketMetric
	{
		public string Bucket { get; set; }
		public string Mutator { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long FaultCount { get; set; }
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
	}
}
