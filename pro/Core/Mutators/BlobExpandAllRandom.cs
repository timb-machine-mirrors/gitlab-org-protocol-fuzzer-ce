//
// Copyright (c) Deja vu Security
//

using System;
using System.IO;

using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Expand the blob by a random size between 1 and 255.
	/// Pick a random start position in the blob.
	/// Add size bytes starting at position where each byte is a
	/// different randomly selected value.
	/// </summary>
	[Mutator("BlobExpandAllRandom")]
	[Description("Expand the blob by filling it with randomly selected values")]
	public class BlobExpandAllRandom : Utility.BlobMutator
	{
		public BlobExpandAllRandom(DataElement obj)
			: base(obj)
		{
		}

		protected override long MaxLength
		{
			get
			{
				return byte.MaxValue;
			}
		}

		protected override bool ClampLength
		{
			get
			{
				return false;
			}
		}

		protected override BitwiseStream PerformMutation(BitStream data, long start, long length)
		{
			var ret = new BitStreamList();

			// Slice off data up to start
			if (start > 0)
				ret.Add(data.SliceBits(start * 8));

			// Add length bytes where each byte is a new random value
			var val = (byte)context.Random.Next(0, 256);
			var buf = new byte[length];
			for (int i = 0; i < buf.Length; ++i)
				buf[i] = (byte)context.Random.Next(0, 256);
			ret.Add(new BitStream(buf));

			// Slice off from start to end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}
	}
}

