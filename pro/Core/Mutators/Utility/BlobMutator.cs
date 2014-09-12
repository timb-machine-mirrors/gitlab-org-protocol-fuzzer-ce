//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;
using Peach.Core.IO;
using System.Diagnostics;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Generate integer edge cases. The numbers produced are distributed 
	/// over a bell curve with the edge case as the center.
	/// </summary>
	public abstract class BlobMutator : Mutator
	{
		long variance;
		bool clamp;
		double stddev;

		public BlobMutator(DataElement obj)
		{
		}

		/// <summary>
		/// Construct base blob mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		/// <param name="variance">Variance for mutation length.</param>
		/// <param name="clamp">Keep mutation length less than element length.</param>
		public BlobMutator(DataElement obj, long variance, bool clamp)
		{
			this.clamp = clamp;

			Hint h;

			if (obj.Hints.TryGetValue("{0}-N".Fmt(GetType().Name), out h))
			{
				this.variance = long.Parse(h.Value);
			}
			else if (obj.Hints.TryGetValue("BlobMutator-N", out h))
			{
				this.variance = long.Parse(h.Value);
			}
			else
			{
				this.variance = variance;
			}

			if (clamp)
				this.variance = Math.Min(variance, ((BitStream)obj.InternalValue).Length);

			// We generate 1/2 of a gaussian distribution so the
			// standard deviation is 1/3 of the variance

			this.stddev = this.variance / 3.0;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.Blob && obj.isMutable)
				return true;

			return false;
		}

		public sealed override uint mutation
		{
			get;
			set;
		}

		public sealed override int count
		{
			get
			{
				return (int)this.variance;
			}
		}

		protected virtual long MaxLength
		{
			get { return 0; }
		}

		protected virtual bool ClampLength
		{
			get { return false; }
		}

		public sealed override void sequentialMutation(DataElement obj)
		{
			var mutated = performMutation(obj, mutation + 1);

			// Sequential should always succeed!
			System.Diagnostics.Debug.Assert(mutated);
		}

		public sealed override void randomMutation(DataElement obj)
		{
			while (true)
			{
				// Gaussian distribution, positive and centered on 1
				var next = context.Random.NextGaussian(0, stddev);
				var asLong = (long)Math.Abs(Math.Round(next)) + 1;

				if (performMutation(obj, asLong))
					return;
				else
					CountBadRandom();
			}
		}

		bool performMutation(DataElement obj, long length)
		{
			var data = (BitStream)obj.InternalValue;

			long start;

			if (clamp)
			{
				if (data.Length == 0)
					return true;

				if (length > data.Length)
					return false;

				start = context.Random.Next(0, data.Length - length);
			}
			else
			{
				start = context.Random.Next(0, data.Length);
			}

			var bs = PerformMutation(data, start, length);

			obj.mutationFlags = MutateOverride.Default;
			obj.MutatedValue = new Variant(bs);

			return true;
		}

		internal uint BadRandom
		{
			get;
			set;
		}

		[Conditional("DEBUG")]
		void CountBadRandom()
		{
			++BadRandom;
		}
		/// <summary>
		/// Perform mutation of a sequence of bytes.
		/// </summary>
		/// <remarks>
		/// The 'start' argument will always fall within the length of the bit stream.
		/// The 'length' argument can potentially run past the end of the bit stream
		/// if 'ClampLength' is false.  When 'ClampLength' is true, 'length' will never
		/// run past the end of the bit stream.
		/// </remarks>
		/// <param name="data">The source data to alter.</param>
		/// <param name="start">The start position to begin altering.</param>
		/// <param name="length">The number of bytes to alter.</param>
		/// <returns>The altered data.</returns>
		protected abstract BitwiseStream PerformMutation(BitStream data, long start, long length);
	}
}

