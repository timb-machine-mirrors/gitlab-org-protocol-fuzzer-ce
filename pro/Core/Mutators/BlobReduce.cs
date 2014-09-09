﻿//
// Copyright (c) Deja vu Security
//

using System;
using System.IO;

using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Picks a random range of bytes inside the blob and removes it.
	/// </summary>
	[Mutator("BlobReduce")]
	[Description("Reduce the size of a blob")]
	public class BlobReduce : Utility.BlobMutator
	{
		public BlobReduce(DataElement obj)
			: base(obj)
		{
		}

		protected override long MaxLength
		{
			get
			{
				return long.MaxValue;
			}
		}

		protected override bool ClampLength
		{
			get
			{
				return true;
			}
		}

		protected override BitwiseStream PerformMutation(BitStream data, long start, long length)
		{
			var ret = new BitStreamList();

			// Slice off up to start
			if (start > 0)
				ret.Add(data.SliceBits(start * 8));

			// Slip next length bytes
			data.Seek(length, SeekOrigin.Current);

			// Slice off end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}
	}
}
