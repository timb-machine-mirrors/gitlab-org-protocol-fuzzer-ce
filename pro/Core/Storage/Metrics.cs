using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Peach.Pro.Core.Storage
{
	public interface IMetric
	{
		int Id { get; set; }
		string Name { get; set; }
	}

	[Serializable]
	public class State : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Action : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Parameter : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Element : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Mutator : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Dataset : IMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	[Serializable]
	public class Bucket
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string Name { get; set; }
		public string MajorHash { get; set; }
		public string MinorHash { get; set; }
	}

	/// <summary>
	/// One row per bucket instance.
	/// </summary>
	/// <summary>
	/// One row per state instance.
	/// </summary>
	public class StateInstance
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public int StateId { get; set; }
		[ForeignKey("StateId")]
		public virtual State State { get; set; }
	}

	/// <summary>
	/// One row per sample (data mutation).
	/// </summary>
	[Serializable]
	public class Sample
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public int StateId { get; set; }
		public int ActionId { get; set; }
		public int ParameterId { get; set; }
		public int ElementId { get; set; }
		public int MutatorId { get; set; }
		public int DatasetId { get; set; }

		[ForeignKey("StateId")]
		public virtual State State { get; set; }
		[ForeignKey("ActionId")]
		public virtual Action Action { get; set; }
		[ForeignKey("ParameterId")]
		public virtual Parameter Parameter { get; set; }
		[ForeignKey("ElementId")]
		public virtual Element Element { get; set; }
		[ForeignKey("MutatorId")]
		public virtual Mutator Mutator { get; set; }
		[ForeignKey("DatasetId")]
		public virtual Dataset Dataset { get; set; }
	}

	/// <summary>
	/// One row per sample per fault.
	/// </summary>
	public class FaultMetricSample
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public long FaultMetricId { get; set; }
		[ForeignKey("FaultMetricId")]
		public virtual FaultMetric Fault { get; set; }

		public long SampleId { get; set; }
		[ForeignKey("SampleId")]
		public virtual Sample Sample { get; set; }
	}

	/// <summary>
	/// One row per fault.
	/// </summary>
	public class FaultMetric
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public long Iteration { get; set; }

		public int BucketId { get; set; }
		[ForeignKey("BucketId")]
		public virtual Bucket Bucket { get; set; }

		public DateTime Timestamp { get; set; }
		public int Hour { get; set; }

		public virtual ICollection<FaultMetricSample> Samples { get; set; }
	}
}
