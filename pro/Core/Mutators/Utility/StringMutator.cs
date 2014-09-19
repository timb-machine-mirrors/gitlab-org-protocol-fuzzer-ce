//
// Copyright (c) Deja vu Security
//

using System;
using System.Text;

using Peach.Core.Dom;
using System.Collections.Generic;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Generate random strings using characters randomly selected
	/// from the specified range.  By default this mutator only
	/// supports unicode strings.
	/// </summary>
	public abstract class StringMutator : Mutator
	{
		Func<int> gen;
		int numMutations;

		/// <summary>
		/// Construct base string mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		/// <param name="minCodePoint">Minimum unicode code point to select.</param>
		/// <param name="maxCodePoint">Maximum unicode code point to select.</param>
		public StringMutator(DataElement obj, int minCodePoint, int maxCodePoint)
		{
			gen = () => context.Random.Next(minCodePoint, maxCodePoint + 1);

			// TODO: Do something better with numMutations
			this.numMutations = maxCodePoint - minCodePoint;
		}

		/// <summary>
		/// Construct base string mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		/// <param name="codePoints">List of code points to select from.</param>
		public StringMutator(DataElement obj, int[] codePoints)
		{
			gen = () => codePoints[context.Random.Next(0, codePoints.Length)];

			// TODO: Do something better with numMutations
			this.numMutations = codePoints.Length;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asStr = obj as Dom.String;
			if (asStr != null && asStr.isMutable && asStr.stringType != StringType.ascii)
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
				return numMutations;
			}
		}

		/// <summary>
		/// Returns the length to use when generating the mutated string.
		/// </summary>
		/// <param name="obj">Data element the mutation is part of.</param>
		/// <returns>How long the mutated string should be.</returns>
		protected virtual int GetMutatedLength(DataElement obj)
		{
			var str = (string)obj.InternalValue;
			var len = string.IsNullOrEmpty(str) ? 1 : str.Length;

			return len;
		}

		public sealed override void sequentialMutation(DataElement obj)
		{
			// Sequential is the same as random
			randomMutation(obj);
		}

		public sealed override void randomMutation(DataElement obj)
		{
			var len = GetMutatedLength(obj);
			var sb = new StringBuilder(len);

			for (int i = 0; i < len; ++i)
			{
				var cp = gen();
				var ch = char.ConvertFromUtf32(cp);

				sb.Append(ch);
			}

			obj.MutatedValue = new Variant(sb.ToString());
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
