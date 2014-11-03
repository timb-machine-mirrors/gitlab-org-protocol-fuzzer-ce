//
// Copyright (c) Deja vu Security
//

using System;
using System.IO;
using System.Linq;

using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;

namespace Peach.Core.Mutators.Utility
{
	public static class SizedHelpers
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static void ExpandTo(DataElement obj, long length, bool overrideRelation)
		{
			obj.mutationFlags = MutateOverride.Default;

			var sizeRelation = obj.relations.From<SizeRelation>().FirstOrDefault();
			if (sizeRelation == null)
			{
				logger.Error("Error, sizeRelation == null, unable to perform mutation.");
				return;
			}

			var objOf = sizeRelation.Of;
			if (objOf == null)
			{
				logger.Error("Error, sizeRelation.Of == null, unable to perform mutation.");
				return;
			}

			objOf.mutationFlags = MutateOverride.Default;
			objOf.mutationFlags |= MutateOverride.TypeTransform;
			objOf.mutationFlags |= MutateOverride.Transformer;

			if (overrideRelation)
			{
				// Indicate we are overrideing the relation
				objOf.mutationFlags |= MutateOverride.Relations;

				// Keep size indicator the same
				obj.MutatedValue = obj.InternalValue;
			}

			var data = objOf.Value;

			if (sizeRelation.lengthType == LengthType.Bytes)
				data = GrowBytes(data, length);
			else
				data = GrowBits(data, length);

			objOf.MutatedValue = new Variant(data);

		}

		static BitwiseStream GrowBytes(BitwiseStream data, long tgtLen)
		{
			var dataLen = data.Length;

			if (tgtLen <= 0)
			{
				// Return empty if size is negative
				data = new BitStream();
			}
			else if (data.Length == 0)
			{
				// If objOf is a block, data is a BitStreamList
				data = new BitStream();

				// Fill with 'A' if we don't have any data
				while (--tgtLen > 0)
					data.WriteByte((byte)'A');

				// Ensure we are at the start of the stream
				data.Seek(0, SeekOrigin.Begin);
			}
			else
			{
				// Loop data over and over until we get to our target length
				var lst = new BitStreamList();

				while (tgtLen > dataLen)
				{
					lst.Add(data);
					tgtLen -= dataLen;
				}

				var buf = new byte[BitwiseStream.BlockCopySize];
				var dst = new BitStream();

				data.Seek(0, System.IO.SeekOrigin.Begin);

				while (tgtLen > 0)
				{
					int len = (int)Math.Min(tgtLen, buf.Length);
					len = data.Read(buf, 0, len);

					if (len == 0)
						data.Seek(0, System.IO.SeekOrigin.Begin);
					else
						dst.Write(buf, 0, len);

					tgtLen -= len;
				}

				lst.Add(dst);

				data = lst;
			}

			return data;
		}

		static BitwiseStream GrowBits(BitwiseStream data, long tgtLen)
		{
			var dataLen = data.LengthBits;

			if (tgtLen <= 0)
			{
				// Return empty if size is negative
				data = new BitStream();
			}
			else if (data.LengthBits == 0)
			{
				// If objOf is a block, data is a BitStreamList
				data = new BitStream();

				// Fill with 'A' if we don't have any data
				for (long i = data.LengthBits + 7 / 8; i > 0; --i)
					data.WriteByte((byte)'A');

				// Truncate to the correct bit length
				data.SetLengthBits(tgtLen);

				// Ensure we are at the start of the stream
				data.SeekBits(0, SeekOrigin.Begin);
			}
			else
			{
				// Loop data over and over until we get to our target length
				var lst = new BitStreamList();

				while (tgtLen > dataLen)
				{
					lst.Add(data);
					tgtLen -= dataLen;
				}

				var dst = new BitStream();

				data.Seek(0, System.IO.SeekOrigin.Begin);

				while (tgtLen > 0)
				{
					ulong bits;
					int len = data.ReadBits(out bits, (int)Math.Min(tgtLen, 64));

					if (len == 0)
						data.Seek(0, System.IO.SeekOrigin.Begin);
					else
						dst.WriteBits(bits, len);

					tgtLen -= len;
				}

				lst.Add(dst);

				data = lst;
			}

			return data;
		}

		/// <summary>
		/// Returns the maximum number of bytes the element can be expanded
		/// by and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxExpansion(DataElement obj)
		{
			var root = (DataModel)obj.root;

			// This should only be called on elements inside of a
			// data model that is a child of an Action, not a top
			// level DataModel.
			System.Diagnostics.Debug.Assert(root.actionData != null);

			var max = root.actionData.MaxOutputSize;
			if (max == 0)
				return long.MaxValue;

			var used = (ulong)root.Value.Length;

			return (long)Math.Min((ulong)long.MaxValue, max - used);
		}

		/// <summary>
		/// Returns the maximum number of times the element can be duplicated
		/// by and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxDuplication(DataElement obj)
		{
			var root = (DataModel)obj.root;

			// This should only be called on elements inside of a
			// data model that is a child of an Action, not a top
			// level DataModel.
			System.Diagnostics.Debug.Assert(root.actionData != null);

			var max = root.actionData.MaxOutputSize;
			if (max == 0)
				return long.MaxValue;

			var used = (ulong)root.Value.Length;
			var size = (ulong)obj.Value.Length;

			var avail = max - used;
			var ret = avail / size;

			return (long)Math.Min((ulong)long.MaxValue, ret);
		}
	}
}
