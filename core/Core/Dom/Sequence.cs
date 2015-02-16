﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Peach.Core.Analyzers;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Sequence element
	/// </summary>
	[DataElement("Sequence")]
	[PitParsable("Sequence")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("ref", typeof(string), "Element to reference", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Sequence : Block
	{
		protected bool Expanded;

		protected BitwiseStream ExpandedValue;
		// ReSharper disable once InconsistentNaming
		protected int? countOverride;

		public virtual int? CountOverride
		{
			get
			{
				return countOverride;
			}

			set
			{
				countOverride = value;

				ExpandedValue = this[Count - 1].Value;

				Invalidate();
			}
		}

		public virtual int GetCountOverride()
		{
			// Called from CountRelation to get our size.
			// Ensure we have expanded before checking this.Count
			//if (!expanded)
			//    ExpandTo(occurs);

			return countOverride.GetValueOrDefault(Count);
		}

		public Sequence()
		{
		}

		public Sequence(string name)
			: base(name)
		{
		}

		public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Sequence")
				return null;

			Sequence sequence;

			if (node.hasAttr("ref"))
			{
				var name = node.getAttr("name", null);
				var refName = node.getAttrString("ref");
				var dom = ((DataModel)parent.root).dom;
				var refObj = dom.getRef(refName, parent);

				if (refObj == null)
					throw new PeachException("Error, Sequence {0}could not resolve ref '{1}'. XML:\n{2}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, node.OuterXml));

				if (!(refObj is Sequence))
					throw new PeachException("Error, Sequence {0}resolved ref '{1}' to unsupported element {2}. XML:\n{3}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, refObj.debugName, node.OuterXml));

				if (string.IsNullOrEmpty(name))
					name = new Sequence().name;

				sequence = refObj.Clone(name) as Sequence;
				if (sequence != null)
				{
					sequence.parent = parent;
					sequence.isReference = true;
					sequence.referenceName = refName;
				}
			}
			else
			{
				sequence = Generate<Sequence>(node, parent);
				sequence.parent = parent;
			}

			context.handleCommonDataElementAttributes(node, sequence);
			context.handleCommonDataElementChildren(node, sequence);
			context.handleDataElementContainer(node, sequence);

			return sequence;
		}

		protected override Variant GenerateDefaultValue()
		{
			var remain = countOverride.GetValueOrDefault(Count);

			var stream = new BitStreamList() { Name = fullName };

			for (var i = 0; remain > 0 && i < Count; ++i, --remain)
				stream.Add(this[i].Value);

			if (remain == 0)
				return new Variant(stream);

			// If we are here, it is because of CountOverride being set!
			Debug.Assert(countOverride.HasValue);
			Debug.Assert(ExpandedValue != null);

			var halves = new Stack<Tuple<long, bool>>();
			halves.Push(null);

			while (remain > 1)
			{
				var carry = remain % 2 == 1;
				remain /= 2;
				halves.Push(new Tuple<long, bool>(remain, carry));
			}

			var value = ExpandedValue;
			var toAdd = value;

			var item = halves.Pop();

			while (item != null)
			{
				var lst = new BitStreamList
				{
					toAdd, 
					toAdd
				};
				if (item.Item2)
					lst.Add(value);

				toAdd = lst;
				item = halves.Pop();
			}

			stream.Add(toAdd);

			return new Variant(stream);
		}
	}
}

// end
