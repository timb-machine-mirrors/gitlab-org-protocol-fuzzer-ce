//
// Copyright (c) Deja vu Security
//

using System;
using System.Text;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringCaseRandom")]
	[Description("Change the string to be a random case.")]
	public class StringCaseRandom : Mutator
	{
		int mutations;

		public StringCaseRandom(DataElement obj)
		{
			var str = (string)obj.InternalValue;

			// For strings <= 20, the unique mutations is 2^n
			// For strings > 20, the unique permutations is
			// n! / (20! * (n - 20)!)
			// and each permutation has 2^20 mutations

			if (str.Length <= 16)
				mutations = 1 << str.Length;
			else
				mutations = ushort.MaxValue + 1;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
			{
				// Esure the string changes when changing the case.
				// TODO: Investigate if it is faster to go 1 char at a time.
				var str = (string)obj.InternalValue;

				if (str != str.ToUpper())
					return true;

				if (str != str.ToLower())
					return true;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return mutations;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			var sb = new StringBuilder((string)obj.InternalValue);

			// Legacy implementation:
			// if str.Length <= 20, randomly flip all characters
			// if str.Length > 20, pick 20 characters and randomly flip them

			var indices = context.Random.Permutation(sb.Length, 20);

			for (int i = 0; i < indices.Length; ++i)
			{
				// Permutation is [1,Length] inclusive
				var idx = indices[i] - 1;

				// TODO: Should we do a case toggle?

				if (context.Random.NextBoolean())
					sb[idx] = char.ToUpper(sb[idx]);
				else
					sb[idx] = char.ToLower(sb[idx]);
			}

			obj.MutatedValue = new Variant(sb.ToString());
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
