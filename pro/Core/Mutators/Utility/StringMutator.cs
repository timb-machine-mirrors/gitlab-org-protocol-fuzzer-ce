//
// Copyright (c) Deja vu Security
//

using System;
using System.Text;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Generate random strings using characters randomly selected
	/// from the specified range.  By default this mutator only
	/// supports unicode strings.
	/// </summary>
	public abstract class StringMutator : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		Func<int> gen;

		/// <summary>
		/// Construct base string mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		/// <param name="minCodePoint">Minimum unicode code point to select.</param>
		/// <param name="maxCodePoint">Maximum unicode code point to select.</param>
		public StringMutator(DataElement obj, int minCodePoint, int maxCodePoint)
			: base(obj)
		{
			gen = () => context.Random.Next(minCodePoint, maxCodePoint + 1);
		}

		/// <summary>
		/// Construct base string mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		/// <param name="codePoints">List of code points to select from.</param>
		public StringMutator(DataElement obj, int[] codePoints)
			: base(obj)
		{
			gen = () => codePoints[context.Random.Next(0, codePoints.Length)];
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asStr = obj as Dom.String;
			if (asStr != null && asStr.isMutable && asStr.stringType != StringType.ascii)
				return true;

			return false;
		}

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var str = (string)obj.InternalValue;
			var len = (long)str.Length;

			signed = false;
			min = 1;
			max = ushort.MaxValue;
			value = Math.Min(len, max);
			value = Math.Max(min, value);
		}

		protected sealed override void performMutation(DataElement obj, long value)
		{
			System.Diagnostics.Debug.Assert(value <= int.MaxValue);

			var sb = new StringBuilder((int)value);

			for (long i = 0; i < value; ++i)
			{
				var cp = gen();
				var ch = char.ConvertFromUtf32(cp);

				sb.Append(ch);
			}

			obj.MutatedValue = new Variant(sb.ToString());
			obj.mutationFlags = MutateOverride.Default;
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}
