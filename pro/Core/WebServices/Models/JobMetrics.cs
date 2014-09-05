using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class BucketTimelineMetric
	{
		public string Label { get; set; }
		public uint Iteration { get; set; }
		public DateTime Time { get; set; }
		public string Type { get; set; }
		public string MajorHash { get; set; }
		public string MinorHash { get; set; }
		public uint FaultCount { get; set; }
	}

	public class MutatorMetric
	{
		public string Mutator { get; set; }
		public uint ElementCount { get; set; }
		public uint IterationCount { get; set; }
		public uint BucketCount { get; set; }
		public uint FaultCount { get; set; }
	}

	public class ElementMetric
	{
		public string Dataset { get; set; }
		public string State { get; set; }
		public string Action { get; set; }
		public string Element { get; set; }
		public uint MutationCount { get; set; }
		public uint BucketCount { get; set; }
		public uint FaultCount { get; set; }
	}

	public class StateMetric
	{
		public string State { get; set; }
		public uint ExecutionCount { get; set; }
	}

	public class DatasetMetric
	{
		public string Dataset { get; set; }
		public uint IterationCount { get; set; }
		public uint BucketCount { get; set; }
		public uint FaultCount { get; set; }
	}

	public class BucketMetric
	{
		public string Bucket { get; set; }
		public string Mutator { get; set; }
		public string Dataset { get; set; }
		public string State { get; set; }
		public string Action { get; set; }
		public string Element { get; set; }
		public uint IterationCount { get; set; }
		public uint FaultCount { get; set; }
	}
}
