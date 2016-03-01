using System;
using System.Collections.Generic;
using System.Xml;
using NLog;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Dom
{
	[PitParsable("Frag")]
	[DataElement("Frag", DataElementTypes.All)]
	[DescriptionAttribute("Fragmentation element")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("fragLength", typeof(int), "Fragment size in bytes", "")]
	[Parameter("class", typeof(string), "Fragment extension class", "ByLength")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("payloadOptional", typeof(bool), "Protocol allows for null payload", "false")]
	[Parameter("totalLengthField", typeof(string), "Name of total length field in template model.", "")]
	[Parameter("fragmentLengthField", typeof(string), "Name of fragment length field in template model.", "")]
	[Parameter("fragmentOffsetField", typeof(string), "Name of fragment offset field in template model.", "")]
	[Parameter("fragmentIndexField", typeof(string), "Name of fragment index field in template model.", "")]
	[Serializable]
	public class Frag : Block
	{
		protected static Logger Logger = LogManager.GetCurrentClassLogger();

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

		public bool PayloadOptional { get; set; }

		public FragSequence Rendering { get { return (FragSequence)this["Rendering"]; } }

		public bool viewPreRendering = true;

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Frag")
				return null;

			var block = Generate<Frag>(node, parent);
			block.parent = parent;

			block.Class = node.getAttr("class", "ByLength");
			block.FragLength = node.getAttr("fragLength", 0);
			block.PayloadOptional = node.getAttr("payloadOptional", false);
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

			if (!string.IsNullOrEmpty(block.TotalLengthField))
				if (block["Template"].find(block.TotalLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find totalLengthField '{1}' in template model.",
						block.Name, block.TotalLengthField));

			if (!string.IsNullOrEmpty(block.FragmentLengthField))
				if (block["Template"].find(block.FragmentLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentLengthField '{1}' in template model.",
						block.Name, block.FragmentLengthField));

			if (!string.IsNullOrEmpty(block.FragmentOffsetField))
				if (block["Template"].find(block.FragmentOffsetField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentOffsetField '{1}' in template model.",
						block.Name, block.FragmentOffsetField));

			if (!string.IsNullOrEmpty(block.FragmentIndexField))
				if (block["Template"].find(block.FragmentIndexField) == null)
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
			if (!viewPreRendering)
				GenerateDefaultValue();
			
			return base.Children();
		}

		protected override DataElement GetChild(string name)
		{
			// Make sure we re-generate if needed
			if (!viewPreRendering)
				GenerateDefaultValue();

			return base.GetChild(name);
		}

		protected virtual void OnPayloadInvalidated(object o, EventArgs e)
		{
			_payloadInvalidated = true;
		}

		protected override void OnInsertItem(DataElement item)
		{
			if (item.Name == "Payload")
			{
				Logger.Debug(">>OnInsertItem");
				var payload = this["Payload"];
				if(payload != null)
					payload.Invalidated -= OnPayloadInvalidated;

				item.Invalidated += OnPayloadInvalidated;
			}

			base.OnInsertItem(item);
		}

		protected override void OnRemoveItem(DataElement item, bool cleanup = true)
		{
			if (item.Name == "Payload")
				item.Invalidated -= OnPayloadInvalidated;

			base.OnRemoveItem(item, cleanup);
		}

		protected override void OnSetItem(DataElement oldItem, DataElement newItem)
		{
			if (oldItem.Name == "Payload")
			{
				oldItem.Invalidated -= OnPayloadInvalidated;
				newItem.Invalidated += OnPayloadInvalidated;
			}

			base.OnSetItem(oldItem, newItem);
		}

		[OnCloned]
		private void OnCloned(Frag original, object context)
		{
			this["Payload"].Invalidated += OnPayloadInvalidated;
		}

		protected override Variant GenerateDefaultValue()
		{
			// On first call re-locate our template
			if (Template == null)
			{
				Template = this["Template"];
				Remove(Template, false);

				viewPreRendering = false;
			}

			if (!_childrenDict.ContainsKey("Payload") ||
			    !_childrenDict.ContainsKey("Rendering"))
			{
				return new Variant(new byte[] {});
			}

			// Only perform regeneration if payload is invalidated
			if (_payloadInvalidated || !_generatedFragments)
			{
				_generatedFragments = true;
				_payloadInvalidated = false;

				Logger.Debug("Generating fragments...");
				FragmentAlg.Fragment(Template, this["Payload"], this["Rendering"] as Sequence);
			}

			return new Variant(this["Rendering"].Value);
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			if (Rendering.Count > 0)
			{
				context.Log("Cracking Payload");

				if (FragmentAlg.NeedFragment())
					throw new SoftException("Error, still waiting on fragments prior to reassembly.");

				var reassembledData = FragmentAlg.Reassemble();
				context.CrackData(this["Payload"], reassembledData);
			}
			else
			{
				context.Log("Cracking Fragments");

				var noPayload = false;
				var startPos = data.Position;
				var endPos = startPos;
				var template = Template ?? this["Template"];

				var fragment = template.Clone("Frag_0");
				Rendering.Add(fragment);

				var cracker = context.Clone();
				cracker.CrackData(fragment, data);

				endPos = data.Position;

				var fragDataElement = fragment.find("FragData");
				if (fragDataElement == null && PayloadOptional)
				{
					Logger.Trace("FragData not found, optional payload enabled.");
					noPayload = true;
				}
				else if (fragDataElement == null)
					throw new SoftException("Unable to locate FragData element during infrag action.");

				Logger.Trace("Fragment {3}: pos: {0} length: {1} crack consumed: {2} bytes",
					endPos, data.Length, endPos - startPos, 0);

				if (FragmentAlg.NeedFragment())
					throw new SoftException("Error, still waiting on fragments prior to reassembly.");

				if (noPayload)
					return;

				context.Log("Cracking Payload");
				var reassembledData = FragmentAlg.Reassemble();
				context.CrackData(this["Payload"], reassembledData);
			}
		}
	}
}
