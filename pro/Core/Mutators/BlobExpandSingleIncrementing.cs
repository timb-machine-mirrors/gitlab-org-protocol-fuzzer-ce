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
	/// Pick a starting value between 0 and 255-size.
	/// Add size bytes starting at position where each byte increments by one
	/// </summary>
	[Mutator("BlobExpandSingleIncrementing")]
	[Description("Expand the blob by filling it with incrementing values")]
	public class BlobExpandSingleIncrementing : Utility.BlobMutator
	{
		public BlobExpandSingleIncrementing(DataElement obj)
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

			// Add length bytes starting at random value
			// If we grow by 1 byte, start value can be [0,255] 
			// If we grow by 2 bytes, start value can be [0,254]
			// If we grow by 254 bytes, start value can be [0,2]
			// If we grow by 255 bytes, start value can be [0,1]
			var val = (byte)context.Random.Next(0, 257 - length);
			var buf = new byte[length];
			for (int i = 0; i < buf.Length; ++i)
				buf[i] = val++;
			ret.Add(new BitStream(buf));

			// Slice off from start to end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}
	}
}

