using System;
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
	[Parameter("class", typeof(string), "Frag extension class", "")]
	[Parameter("script", typeof(string), "Frag script", "")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Serializable]
	public class Frag : Block
	{
		public Frag()
			: base()
		{
			Add(new Sequence("Rendering"));
		}

		public Frag(string name)
			: base(name)
		{
			Add(new Sequence("Rendering"));
		}

		#region Parameter Properties

		public string Class { get; set; }
		public string Script { get; set; }

		#endregion

		public FragmentAlgorithm FragmentAlg { get; set; }

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Frag")
				return null;

			var block = Generate<Frag>(node, parent);
			block.parent = parent;

			block.Class = node.getAttr("class", null);
			block.Script = node.getAttr("script", null);
			block.isMutable = false;

			if (!string.IsNullOrEmpty(block.Class))
			{
				var type = ClassLoader.FindTypeByAttribute<FragmentAlgorithmAttribute>((t, a) => 0 == string.Compare(a.Name, block.Class, true));
				if (type == null)
					throw new PeachException(
						"Error, state '" + parent.Name + "' has an invalid action type '" + block.Class + "'.");

				block.FragmentAlg = (FragmentAlgorithm)Activator.CreateInstance(type);
				block.FragmentAlg.Parent = block;
			}

			context.handleCommonDataElementAttributes(node, block);
			context.handleCommonDataElementChildren(node, block);
			context.handleDataElementContainer(node, block);

			if (string.IsNullOrEmpty(block.Class) && string.IsNullOrEmpty(block.Script))
				throw new PeachException(string.Format(
					"Error: Frag '{0}' missing both class and script attributes.",
					block.Name));
			
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
					"Error: Frag '{0}' element has too many children. Expecting 2, found {1}.",
					block.Name, block.Count - 1));

			return block;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Frag");

			//pit.WriteAttributeString("name", Name);

			if(!string.IsNullOrEmpty(Class))
				pit.WriteAttributeString("class", Class.ToString());
			
			if (!string.IsNullOrEmpty(Script))
				pit.WriteAttributeString("script", Script.ToString());

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			this["Template"].WritePit(pit);
			this["Payload"].WritePit(pit);

			pit.WriteEndElement();
		}

		protected override Variant GenerateDefaultValue()
		{
			if (!this._childrenDict.ContainsKey("Template") ||
			    !this._childrenDict.ContainsKey("Payload") ||
			    !this._childrenDict.ContainsKey("Rendering"))
			{
				return new Variant(new byte[]{});
			}

			if (FragmentAlg != null)
				FragmentAlg.Fragment(this["Template"], this["Payload"], this["Rendering"] as Sequence);

			// TODO -- Handle script property

			return this["Rendering"].DefaultValue;
		}
	}
}
