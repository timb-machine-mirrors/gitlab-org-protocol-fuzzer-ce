//
// Copyright (c) Deja vu Security
//

using System;
using System.Linq;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
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
				data = data.GrowTo(length);
			else
				data = data.GrowToBits(length);

			objOf.MutatedValue = new Variant(data);

		}

		// Artificially limit the maximum expansion to be 65k
		// This is to work around OutOfMemoryExceptions when
		// we try and do BitStream.GrowBy((uint/MaxValue / 4) - 1)
		const long maxExpansion = ushort.MaxValue;

		/// <summary>
		/// Returns the maximum number of bytes the element can be
		/// and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxSize(DataElement obj)
		{
			// For testing.  Figure out a way to not have this check in here
			var root = obj.root as DataModel;
			if (root == null)
				return maxExpansion;
			if (root.actionData == null)
				return maxExpansion;

			var max = root.actionData.MaxOutputSize;
			if (max == 0)
				return maxExpansion;

			var used = (ulong)root.Value.LengthBits;
			var size = (ulong)obj.Value.LengthBits;
			var limit = ((8 * max) - used + size + 7) / 8;

			return (long)Math.Min(maxExpansion, limit);
		}

		/// <summary>
		/// Returns the maximum number of bytes the element can be expanded
		/// by and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxExpansion(DataElement obj)
		{
			// For testing.  Figure out a way to not have this check in here
			var root = obj.root as DataModel;
			if (root == null)
				return maxExpansion;
			if (root.actionData == null)
				return maxExpansion;

			var max = root.actionData.MaxOutputSize;
			if (max == 0)
				return maxExpansion;

			var used = (ulong)root.Value.LengthBits;
			var limit = ((8 * max) - used + 7) / 8;

			return (long)Math.Min(maxExpansion, limit);
		}

		/// <summary>
		/// Returns the maximum number of times the element can be duplicated
		/// by and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxDuplication(DataElement obj)
		{
			// For testing.  Figure out a way to not have this check in here
			var root = obj.root as DataModel;
			if (root == null)
				return maxExpansion;
			if (root.actionData == null)
				return maxExpansion;

			var max = root.actionData.MaxOutputSize;
			if (max == 0)
				return maxExpansion;

			var used = (ulong)root.Value.LengthBits;
			var size = (ulong)obj.Value.LengthBits;

			if (size == 0)
				return maxExpansion;

			var avail = (8 * max) - used;
			var ret = avail / size;

			return (long)Math.Min(maxExpansion, ret);
		}

		public static void ExpandStringTo(DataElement obj, long value)
		{
			var src = (string)obj.InternalValue;
			var dst = ExpandTo(src, value);

			obj.MutatedValue = new Variant(dst);
			obj.mutationFlags = MutateOverride.Default;
		}

		static string ExpandTo(string value, long length)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new string('A', (int)length);
			}
			else if (value.Length >= length)
			{
				return value.Substring(0, (int)length);
			}

			var sb = new StringBuilder();

			while (sb.Length + value.Length < length)
				sb.Append(value);

			sb.Append(value.Substring(0, (int)(length - sb.Length)));

			return sb.ToString();
		}
	}
}
