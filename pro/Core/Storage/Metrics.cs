﻿using System;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.Storage
{
	public class NamedItem
	{
		[Key]
		public long Id { get; set; }

		[Unique]
		public string Name { get; set; }
	}

	public class State
	{
		[Key]
		public long Id { get; set; }

		[Index("UX_StateCount", IsUnique = true)]
		[ForeignKey(typeof(NamedItem))]
		public long NameId { get; set; }

		[Index("UX_StateCount", IsUnique = true)]
		public long RunCount { get; set; }

		public long Count { get; set; }
	}

	/// <summary>
	/// One row per data mutation.
	/// </summary>
	public class Mutation
	{
		[Key]
		public long Id { get; set; }

		public long IterationCount { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_State")]
		[ForeignKey(typeof(State))]
		public long StateId { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_Action")]
		[ForeignKey(typeof(NamedItem), Name = "FK_Mutation_Action")]
		public long ActionId { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_Parameter")]
		[ForeignKey(typeof(NamedItem), Name = "FK_Mutation_Parameter")]
		public long ParameterId { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_Element")]
		[ForeignKey(typeof(NamedItem), Name = "FK_Mutation_Element")]
		public long ElementId { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_Mutator")]
		[ForeignKey(typeof(NamedItem), Name = "FK_Mutation_Mutator")]
		public long MutatorId { get; set; }

		[Index("IX_Mutation")]
		[Index("UX_Mutation", IsUnique = true)]
		[Index("IX_Mutation_Dataset")]
		[ForeignKey(typeof(NamedItem), Name = "FK_Mutation_Dataset")]
		public long DatasetId { get; set; }

		[NotMapped]
		public bool IsSaved { get; set; }
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

		private DateTime _timestamp;
		public DateTime Timestamp
		{
			get { return _timestamp; }
			set { _timestamp = value.MakeUtc(); }
		}

		public int Hour { get; set; }

		[ForeignKey(typeof(State))]
		public long StateId { get; set; }

		[ForeignKey(typeof(NamedItem), Name = "FK_FaultMetric_Action")]
		public long ActionId { get; set; }

		[ForeignKey(typeof(NamedItem), Name = "FK_FaultMetric_Parameter")]
		public long ParameterId { get; set; }

		[ForeignKey(typeof(NamedItem), Name = "FK_FaultMetric_Element")]
		public long ElementId { get; set; }

		[ForeignKey(typeof(NamedItem), Name = "FK_FaultMetric_Mutator")]
		public long MutatorId { get; set; }

		[ForeignKey(typeof(NamedItem), Name = "FK_FaultMetric_Dataset")]
		public long DatasetId { get; set; }
	}
}
