using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.Storage
{
	[AttributeUsage(AttributeTargets.Property)]
	class NotMappedAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	class Table : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	class KeyAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	class RequiredAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	class IndexAttribute : Attribute
	{
		public string Name { get; set; }
		public bool IsUnique { get; set; }

		public IndexAttribute() { }
		public IndexAttribute(string name) { Name = name; }
	}

	[AttributeUsage(AttributeTargets.Property)]
	class ForeignKeyAttribute : Attribute
	{
		public Type TargetEntity { get; set; }
		public string TargetProperty { get; set; }

		public ForeignKeyAttribute(Type targetEntity)
		{
			TargetEntity = targetEntity;
			TargetProperty = "Id";
		}
	}

	public class Metric
	{
		[Key]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	public class State : Metric { }
	public class Action : Metric { }
	public class Parameter : Metric { }
	public class Element : Metric { }
	public class Mutator : Metric { }
	public class Dataset : Metric { }

	/// <summary>
	/// One row per state instance.
	/// </summary>
	public class StateInstance
	{
		[Key]
		public long Id { get; set; }

		[ForeignKey(typeof(State))]
		public long StateId { get; set; }
	}

	/// <summary>
	/// One row per data mutation.
	/// </summary>
	public class Mutation
	{
		[Key]
		public long Id { get; set; }

		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Iteration_Alone")]
		public long Iteration { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_State")]
		[ForeignKey(typeof(State))]
		public long StateId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Action")]
		[ForeignKey(typeof(Action))]
		public long ActionId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Parameter")]
		[ForeignKey(typeof(Parameter))]
		public long ParameterId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Element")]
		[ForeignKey(typeof(Element))]
		public long ElementId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Mutator")]
		[ForeignKey(typeof(Mutator))]
		public long MutatorId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Dataset")]
		[ForeignKey(typeof(Dataset))]
		public long DatasetId { get; set; }
	}

	/// <summary>
	/// One row per fault.
	/// </summary>
	public class FaultMetric
	{
		[Key]
		public long Id { get; set; }

		[Index("IX_FaultMetric_Iteration")]
		public long Iteration { get; set; }

		[Required]
		[Index("IX_FaultMetric_MajorHash")]
		public string MajorHash { get; set; }
		[Required]
		public string MinorHash { get; set; }

		public DateTime Timestamp { get; set; }
		public int Hour { get; set; }
	}

	/// <summary>
	/// Many-to-many association
	/// </summary>
	public class FaultMetricMutation
	{
		[Key]
		[ForeignKey(typeof(FaultMetric))]
		[Index("IX_FaultMetricMutation_FaultMetric")]
		public long FaultMetricId { get; set; }

		[Key]
		[ForeignKey(typeof(Mutation))]
		[Index("IX_FaultMetricMutation_Mutation")]
		public long MutationId { get; set; }
	}
}
