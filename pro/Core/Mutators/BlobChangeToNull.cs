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
	/// Alter the blob by a random number of bytes between 1 and 100.
	/// Pick a random start position in the blob.
	/// Alter size bytes starting at positionand set each byte to null.
	/// </summary>
	[Mutator("BlobChangeToNull")]
	[Description("Change the blob by replacing bytes with null bytes")]
	[Hint("BlobChangeToNull-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobChangeToNull-N", "Standard deviation of number of bytes to change")]
	public class BlobChangeToNull : Utility.BlobMutator
	{
		public BlobChangeToNull(DataElement obj)
			: base(obj, 100, false)
		{
		}

		protected override BitwiseStream PerformMutation(BitStream data, long start, long length)
		{
			var ret = new BitStreamList();

			// Slice off data up to start
			if (start > 0)
				ret.Add(data.SliceBits(start * 8));

			// Add length bytes of null
			var buf = new byte[length];
			ret.Add(new BitStream(buf));

			// Skip length bytes from data
			data.Seek(length, SeekOrigin.Current);

			// Slice off from start + length to end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}
	}
}
