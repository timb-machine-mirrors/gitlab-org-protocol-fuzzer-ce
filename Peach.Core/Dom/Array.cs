
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
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Linq;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Core.Dom
{

	/// <summary>
	/// Array of data elements.  Can be
	/// zero or more elements.
	/// </summary>
	[Serializable]
	[DataElement("Array")]
	[DataElementParentSupported(null)]
	public class Array : Block
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public int minOccurs = 1;
		public int maxOccurs = 1;
		public int occurs = 1;

		private int? countOverride;

		public int? CountOverride
		{
			get
			{
				return countOverride;
			}
			set
			{
				countOverride = value;
				Invalidate();
			}
		}

		private DataElement originalElement;

		public DataElement OriginalElement
		{
			get
			{
				if (originalElement == null && Count > 0)
					originalElement = this[0];

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

		protected override IEnumerable<DataElement> Children()
		{
			// If we have entries, just return them
			if (Count > 0)
				return this;

			// If we don't have entries, just return our original element
			if (OriginalElement != null)
				return new DataElement[1] { OriginalElement };

			// Mutation might have removed our original element
			return new DataElement[0];
		}

		protected override void OnRemoveItem(DataElement item)
		{
			base.OnRemoveItem(item);

			if (item == originalElement)
				originalElement = null;

			if (this.Count == 0 && OriginalElement == null)
				parent.Remove(this);
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			long startPos = data.PositionBits;
			BitStream sizedData = ReadSizedData(data, size);

			if (OriginalElement == null)
				throw new CrackingFailure("{0} has no original element.".Fmt(debugName), this, data);

			Clear();

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
			int remain = CountOverride.GetValueOrDefault(Count);

			var stream = new BitStreamList() { Name = fullName };

			for (int i = 0; remain > 0 && i < Count; ++i, --remain)
				stream.Add(this[i].Value);

			var elem = Count == 0 ? OriginalElement : this[Count - 1];

			while (remain-- > 0)
				stream.Add(elem.Value);

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

			if (index == 0)
				OriginalElement = clone.Clone();
			else
				clone = clone.Clone("{0}_{1}".Fmt(clone.name, index));

			return clone;
		}

		[OnCloning]
		private bool OnCloning(object context)
		{
			DataElement.CloneContext ctx = context as DataElement.CloneContext;

			if (ctx != null)
			{
				// If we are being renamed and our 1st child has the same name
				// as us, it needs to be renamed as well
				if (ctx.rename.Contains(this) && Count > 0 && name == this[0].name)
					ctx.rename.Add(this[0]);
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
			System.Diagnostics.Debug.Assert(Count > 0 || OriginalElement != null);

			// If we are empty, start by adding our OriginalElement
			for (int i = Count; i < 1 && i < count; ++i)
				Add(OriginalElement);

			// Add clones of our original element for the remainder
			for (int i = Count; i < count; ++i)
				Add(MakeElement(i));

			Invalidate();
		}
	}
}

// end
