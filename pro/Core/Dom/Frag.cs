﻿using System;
using System.Collections.Generic;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Array = Peach.Core.Dom.Array;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Dom
{
	[PitParsable("Frag")]
	[DataElement("Frag", DataElementTypes.All)]
	[DescriptionAttribute("Fragmentation element")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fragLength", typeof(int), "Fragment size in bytes")]
	[Parameter("class", typeof(string), "Fragment extension class", "ByLength")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("totalLengthField", typeof(string), "Name of total length field in template model.", "")]
	[Parameter("fragmentLengthField", typeof(string), "Name of fragment length field in template model.", "")]
	[Parameter("fragmentOffsetField", typeof(string), "Name of fragment offset field in template model.", "")]
	[Parameter("fragmentIndexField", typeof(string), "Name of fragment index field in template model.", "")]
	[Serializable]
	public class Frag : Block
	{
		public Frag()
			: base()
		{
			Add(new FragSequence("Rendering"));
		}

		public Frag(string name)
			: base(name)
		{
			Add(new FragSequence("Rendering"));
		}

		#region Parameter Properties

		public string Class { get; set; }
		public int FragLength { get; set; }
		public string TotalLengthField { get; set; }
		public string FragmentLengthField { get; set; }
		public string FragmentOffsetField { get; set; }
		public string FragmentIndexField { get; set; }

		#endregion

		/// <summary>
		/// Set by Payload.Invalidated event handler
		/// </summary>
		private bool _payloadInvalidated = false;

		/// <summary>
		/// Set after we have generated fragements at least once
		/// </summary>
		private bool _generatedFragments = false;

		/// <summary>
		/// Instance of our fragement algorithm class. Can be null.
		/// </summary>
		public FragmentAlgorithm FragmentAlg { get; set; }

		/// <summary>
		/// Template to use for fragments
		/// </summary>
		public DataElement Template { get; set; }

		public FragSequence Rendering { get { return (FragSequence)this["Rendering"]; } }

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Frag")
				return null;

			var block = Generate<Frag>(node, parent);
			block.parent = parent;

			block.Class = node.getAttr("class", "ByLength");
			block.FragLength = node.getAttr("fragLength", 0);
			block.TotalLengthField = node.getAttr("totalLengthField", "");
			block.FragmentLengthField = node.getAttr("fragmentLengthField", "");
			block.FragmentOffsetField= node.getAttr("fragmentOffsetField", "");
			block.FragmentIndexField = node.getAttr("fragmentIndexField", "");
			block.isMutable = false;

			var type = ClassLoader.FindTypeByAttribute<FragmentAlgorithmAttribute>((t, a) => 0 == string.Compare(a.Name, block.Class, true));
			if (type == null)
				throw new PeachException(
					"Error, Frag element '" + parent.Name + "' has an invalid class attribute '" + block.Class + "'.");

			block.FragmentAlg = (FragmentAlgorithm)Activator.CreateInstance(type);
			block.FragmentAlg.Parent = block;

			context.handleCommonDataElementAttributes(node, block);
			context.handleCommonDataElementChildren(node, block);
			context.handleDataElementContainer(node, block);

			if (!block._childrenDict.ContainsKey("Template"))
				throw new PeachException(string.Format(
					"Error: Frag '{0}' missing child element named 'Template'.",
					block.Name));

			if (!block._childrenDict.ContainsKey("Payload"))
				throw new PeachException(string.Format(
					"Error: Frag '{0}' missing child element named 'Payload'.",
					block.Name));

			if (block.Count != 3)
				throw new PeachException(string.Format(
					"Error: Frag '{0}' element has too many children. Expecting 3, found {1}.",
					block.Name, block.Count - 1));

			block.Template = block["Template"];
			block.Remove(block.Template, false);

			if (!string.IsNullOrEmpty(block.TotalLengthField))
				if (block.Template.find(block.TotalLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find totalLengthField '{1}' in template model.",
						block.Name, block.TotalLengthField));

			if (!string.IsNullOrEmpty(block.FragmentLengthField))
				if (block.Template.find(block.FragmentLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentLengthField '{1}' in template model.",
						block.Name, block.FragmentLengthField));

			if (!string.IsNullOrEmpty(block.FragmentOffsetField))
				if (block.Template.find(block.FragmentOffsetField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentOffsetField '{1}' in template model.",
						block.Name, block.FragmentOffsetField));

			if (!string.IsNullOrEmpty(block.FragmentIndexField))
				if (block.Template.find(block.FragmentIndexField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentIndexField '{1}' in template model.",
						block.Name, block.FragmentIndexField));

			return block;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Frag");

			//pit.WriteAttributeString("name", Name);

			pit.WriteAttributeString("class", Class);
			pit.WriteAttributeString("fragLength", FragLength.ToString());

			if (!string.IsNullOrEmpty(TotalLengthField))
				pit.WriteAttributeString("totalLengthField", TotalLengthField);

			if (!string.IsNullOrEmpty(FragmentLengthField))
				pit.WriteAttributeString("fragmentLengthField", FragmentLengthField);

			if (!string.IsNullOrEmpty(FragmentOffsetField))
				pit.WriteAttributeString("fragmentOffsetField", FragmentOffsetField);

			if (!string.IsNullOrEmpty(FragmentIndexField))
				pit.WriteAttributeString("fragmentIndexField", FragmentIndexField);
			
			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			Template.WritePit(pit);
			this["Payload"].WritePit(pit);

			pit.WriteEndElement();
		}

		protected override IEnumerable<DataElement> Children()
		{
			// Make sure we re-generate if needed
			GenerateDefaultValue();
			return base.Children();
		}

		protected override DataElement GetChild(string name)
		{
			// Make sure we re-generate if needed
			GenerateDefaultValue();
			return base.GetChild(name);
		}

		protected override Variant GenerateDefaultValue()
		{
			if (!this._childrenDict.ContainsKey("Payload") ||
			    !this._childrenDict.ContainsKey("Rendering"))
			{
				return new Variant(new byte[] {});
			}

			// Subscribe to this event once
			if (!_generatedFragments)
			{
				this["Payload"].Invalidated += (sender, args) => { _payloadInvalidated = true; };
			}

			// Only perform regeneration if payload is invalidated
			if (_payloadInvalidated || !_generatedFragments)
			{
				_generatedFragments = true;
				_payloadInvalidated = false;

				FragmentAlg.Fragment(Template, this["Payload"], this["Rendering"] as Sequence);
			}

			return this["Rendering"].DefaultValue;
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			if (FragmentAlg.NeedFragment())
				throw new SoftException("Error, still waiting on fragments prior to reassembly.");

			var reassembledData = FragmentAlg.Reassemble();
			this["Payload"].Crack(context, reassembledData, reassembledData.LengthBits);
		}
	}
}
