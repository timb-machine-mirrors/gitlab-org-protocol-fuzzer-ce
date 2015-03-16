using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Peach.Pro.Core.Storage
{
	public interface IMetric
	{
		long Id { get; set; }
		string Name { get; set; }
	}

	public class State : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }
	
		[Index(IsUnique = true)]
		[Required]
		public string Name { get; set; }
	}

	public class Action : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		[Required]
		public string Name { get; set; }
	}

	public class Parameter : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	public class Element : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		[Required]
		public string Name { get; set; }
	}

	public class Mutator : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		[Required]
		public string Name { get; set; }
	}

	public class Dataset : IMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index(IsUnique = true)]
		public string Name { get; set; }
	}

	/// <summary>
	/// One row per state instance.
	/// </summary>
	public class StateInstance
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public long StateId { get; set; }
		[ForeignKey("StateId")]
		public virtual State State { get; set; }
	}

	/// <summary>
	/// One row per data mutation.
	/// </summary>
	public class Mutation
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Iteration_Alone")]
		public long Iteration { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_State")]
		public long StateId { get; set; }
		
		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Action")]
		public long ActionId { get; set; }
		
		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Parameter")]
		public long ParameterId { get; set; }
		
		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Element")]
		public long ElementId { get; set; }

		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Mutator")]

		public long MutatorId { get; set; }
		[Index("IX_Mutation")]
		[Index("IX_Mutation_Iteration")]
		[Index("IX_Mutation_Dataset")]
		public long DatasetId { get; set; }

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

		public virtual ICollection<FaultMetric> Faults { get; set; }
	}

	/// <summary>
	/// One row per fault.
	/// </summary>
	public class FaultMetric
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

		public virtual ICollection<Mutation> Mutations { get; set; }
	}
}
