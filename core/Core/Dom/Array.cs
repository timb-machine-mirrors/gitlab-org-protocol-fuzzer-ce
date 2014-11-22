﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Core.Dom
{

	/// <summary>
	/// Array of zero or more DataElements. When a user marks an element with the attributes of
	/// occurs, minOcccurs, or maxOccurs, this element is used.
	/// </summary>
	/// <remarks>
	/// Array elements can be in one of two states, pre and post expansion. Initially an Array
	/// will have a single element called the OrigionalElement. This is the pre-expansion state. Once
	/// data is loaded into the Array, the array will have zero or more copies of OrigionalElement, each
	/// with different data. This is the post-expansion state.
	/// </remarks>
	[Serializable]
	[DataElement("Array")]
	[DataElementParentSupported(null)]
	public class Array : Block
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Minimum number of elements this Array can contain
		/// </summary>
		public int minOccurs = 1;
		/// <summary>
		/// Maximum number of elements this Array can contain
		/// </summary>
		public int maxOccurs = 1;
		/// <summary>
		/// Number of occurrence this array should have
		/// </summary>
		public int occurs = 1;

		private bool expanded;

		private BitwiseStream expandedValue;
		private int? countOverride;

		public int? CountOverride
		{
			set
			{
				countOverride = value;

				if (Count == 0)
					expandedValue = OriginalElement.Value;
				else
					expandedValue = this[Count - 1].Value;

				Invalidate();
			}
		}

		public int GetCountOverride()
		{
			// Called from CountRelation to get our size.
			// Ensure we have expanded before checking this.Count
			if (!expanded)
				ExpandTo(occurs);

			return countOverride.GetValueOrDefault(Count);
		}

		private DataElement originalElement;

		/// <summary>
		/// The original elements that was marked with the occurs, minOccurs, or maxOccurs
		/// attributes.
		/// </summary>
		public DataElement OriginalElement
		{
			get
			{
				return originalElement;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				originalElement = value;
				originalElement.parent = this;
			}
		}

		public Array()
		{
		}

		public Array(string name)
			: base(name)
		{
		}

		public override void WritePit(XmlWriter pit)
		{
			originalElement.WritePit(pit);
		}

		protected override IEnumerable<DataElement> Children()
		{
			// If we have entries, just return them
			if (expanded)
				return this;

			// If we don't have entries, just return our original element
			if (OriginalElement != null)
				return new DataElement[1] { OriginalElement };

			// Mutation might have removed our original element
			return new DataElement[0];
		}

		protected override DataElement GetChild(string name)
		{
			// If we already expanded, just search our children
			if (expanded)
				return base.GetChild(name);

			// If we haven't expanded, just check our original element
			if (OriginalElement.name == name)
				return OriginalElement;

			return null;
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			long startPos = data.PositionBits;
			BitStream sizedData = ReadSizedData(data, size);

			if (OriginalElement == null)
				throw new CrackingFailure("{0} has no original element.".Fmt(debugName), this, data);

			//Clear();

			// Use remove to undo any relation bindings on array elements
			BeginUpdate();

			while (Count > 0)
				RemoveAt(0);

			EndUpdate();

			// Mark that we have expanded since cracking will create our children
			expanded = true;

			long min = minOccurs;
			long max = maxOccurs;

			var rel = relations.Of<CountRelation>().Where(context.HasCracked).FirstOrDefault();
			if (rel != null)
				min = max = rel.GetValue();
			else if (minOccurs == 1 && maxOccurs == 1)
				min = max = occurs;

			if (((min > maxOccurs && maxOccurs != -1) || (min < minOccurs)) && min != occurs)
			{
				string msg = "{0} has invalid count of {1} (minOccurs={2}, maxOccurs={3}, occurs={4}).".Fmt(
				    debugName, min, minOccurs, maxOccurs, occurs);
				throw new CrackingFailure(msg, this, data);
			}

			for (int i = 0; max == -1 || i < max; ++i)
			{
				logger.Debug("Crack: ======================");
				logger.Debug("Crack: {0} Trying #{1}", OriginalElement.debugName, i + 1);

				long pos = sizedData.PositionBits;
				if (pos == sizedData.LengthBits)
				{
					logger.Debug("Crack: Consumed all bytes. {0}", sizedData.Progress);
					break;
				}

				var clone = MakeElement(i);
				Add(clone);

				try
				{
					context.CrackData(clone, sizedData);

					// If we used 0 bytes and met the minimum, we are done
					if (pos == sizedData.PositionBits && i == min)
					{
						RemoveAt(clone.parent.IndexOf(clone));
						break;
					}
				}
				catch (CrackingFailure)
				{
					logger.Debug("Crack: {0} Failed on #{1}", debugName, i+1);

					// If we couldn't satisfy the minimum propigate failure
					if (i < min)
						throw;

					RemoveAt(clone.parent.IndexOf(clone));
					sizedData.SeekBits(pos, System.IO.SeekOrigin.Begin);
					break;
				}
			}

			if (this.Count < min)
			{
				string msg = "{0} only cracked {1} of {2} elements.".Fmt(debugName, Count, min);
				throw new CrackingFailure(msg, this, data);
			}

			if (size.HasValue && data != sizedData)
				data.SeekBits(startPos + sizedData.PositionBits, System.IO.SeekOrigin.Begin);
		}

		protected override Variant GenerateDefaultValue()
		{
			if (!expanded)
				ExpandTo(occurs);

			int remain = countOverride.GetValueOrDefault(Count);

			var stream = new BitStreamList() { Name = fullName };

			for (int i = 0; remain > 0 && i < Count; ++i, --remain)
				stream.Add(this[i].Value);

			if (remain == 0)
				return new Variant(stream);

			// If we are here, it is because of CountOverride being set!
			System.Diagnostics.Debug.Assert(countOverride.HasValue);
			System.Diagnostics.Debug.Assert(expandedValue != null);

			var halves = new Stack<Tuple<long, bool>>();
			halves.Push(null);

			while (remain > 1)
			{
				bool carry = remain % 2 == 1;
				remain /= 2;
				halves.Push(new Tuple<long, bool>(remain, carry));
			}

			var value = expandedValue;
			var toAdd = value;

			var item = halves.Pop();

			while (item != null)
			{
				var lst = new BitStreamList();
				lst.Add(toAdd);
				lst.Add(toAdd);
				if (item.Item2)
					lst.Add(value);

				toAdd = lst;
				item = halves.Pop();
			}

			stream.Add(toAdd);

			return new Variant(stream);
		}

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			var array = DataElement.Generate<Array>(node, parent);

			if (node.hasAttr("minOccurs"))
			{
				array.minOccurs = node.getAttrInt("minOccurs");
				array.maxOccurs = -1;
				array.occurs = array.minOccurs;
			}

			if (node.hasAttr("maxOccurs"))
				array.maxOccurs = node.getAttrInt("maxOccurs");

			if (node.hasAttr("occurs"))
				array.occurs = node.getAttrInt("occurs");

			if (node.hasAttr("mutable"))
				array.isMutable = node.getAttrBool("mutable");

			return array;
		}

		private DataElement MakeElement(int index)
		{
			var clone = OriginalElement;

			clone = clone.Clone("{0}_{1}".Fmt(clone.name, index));

			return clone;
		}

		[OnCloning]
		private bool OnCloning(object context)
		{
			DataElement.CloneContext ctx = context as DataElement.CloneContext;

			if (ctx != null)
			{
				// If we are being renamed and our original element has the same name
				// as us, it needs to be renamed as well
				if (ctx.rename.Contains(this) && OriginalElement != null)
					ctx.rename.Add(OriginalElement);
			}

			return true;
		}

		/// <summary>
		/// Expands the size of the array to be 'count' long.
		/// Does this by adding the same instance of the first
		/// item in the array until the Count is count.
		/// </summary>
		/// <param name="count">The total size the array should be.</param>
		public void ExpandTo(int count)
		{
			System.Diagnostics.Debug.Assert(OriginalElement != null);

			// Once this has been called mark the array as having been expanded
			expanded = true;

			BeginUpdate();

			for (int i = Count; i < count; ++i)
				Add(MakeElement(i));

			EndUpdate();
		}
	}
}

// end
