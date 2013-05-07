using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

using Peach.Enterprise.Analyzers.ASN1;
using Peach.Enterprise.DataElements;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Enterprise.Analyzers
{
	[Analyzer("Asn1", true)]
	[Description("Convert DER encoded ASN.1 into Peach Data model")]
	[Serializable]
	public class Asn1Analyzer : Analyzer
	{
        static Asn1Analyzer()
        {
            supportParser = false;
            supportDataElement = true;
            supportCommandLine = false;
            supportTopLevel = false;
        }

        public Asn1Analyzer()
        {
        }

		public Asn1Analyzer(Dictionary<string, Variant> args)
        {
        }

		public override void asDataElement(DataElement parent, object dataBuffer)
		{
			if (!(parent is Blob))
				throw new PeachException("Error, Asn1 analyzer only operates on Blob elements!");

			var blob = parent as Blob;
			var data = blob.Value;

			if (data.LengthBytes == 0)
				return;

			var parser = new ASN1.Asn1Parser();
			parser.LoadData(data.Stream);
			var rootNode = parser.RootNode;

			var block = new Block(blob.name);
			block.Add(HandleAsn1Node(block, rootNode));

			block.parent = blob.parent;
			block.parent[blob.name] = block;
		}

		DataElementContainer MakeAsn1Der(DataElement data, Asn1Node node)
		{
			var container = new Block();
			var type = new Number();
			var asn1Length = new Asn1DerLength();
			var length = new Number();
			var lengthRelation = new SizeRelation();

			type.length = 8;
			type.Signed = false;
			type.LittleEndian = false;
			type.DefaultValue = new Variant(node.Tag);

			length.length = 32;
			length.Signed = false;
			length.LittleEndian = false;
			asn1Length.Add(length);

			container.Add(type);
			container.Add(asn1Length);
			container.Add(data);

			lengthRelation.FromName = length.name;
			lengthRelation.OfName = data.name;
			length.relations.Add(lengthRelation);

			return container;
		}

		DataElementContainer HandleSequence(DataElementContainer parent, Asn1Node sequence)
		{
			var block = new Block();

			for (int count = 0; count < sequence.ChildNodeCount; count++)
			{
				var child = sequence.GetChildNode(count);
				block.Add(HandleAsn1Node(block, child));
			}

			return MakeAsn1Der(block, sequence);
		}

		DataElement HandleAsn1Node(DataElementContainer parent, Asn1Node node)
		{
			switch (node.Tag & Asn1Tag.TAG_MASK)
			{
				case Asn1Tag.NUMERIC_STRING:
				case Asn1Tag.PRINTABLE_STRING:
				case Asn1Tag.T61_STRING:
				case Asn1Tag.VIDEOTEXT_STRING:
				case Asn1Tag.IA5_STRING:
				case Asn1Tag.UTC_TIME:
				case Asn1Tag.GENERALIZED_TIME:
				case Asn1Tag.GRAPHIC_STRING:
				case Asn1Tag.VISIBLE_STRING:
				case Asn1Tag.GENERAL_STRING:
				case Asn1Tag.UNIVERSAL_STRING:
				case Asn1Tag.BMPSTRING:
				case Asn1Tag.INTEGER:
				// handle on own

				case Asn1Tag.BIT_STRING:
				case Asn1Tag.OCTET_STRING:
				case Asn1Tag.UTF8_STRING:
				case Asn1Tag.RELATIVE_OID:
				case Asn1Tag.BOOLEAN:
				case Asn1Tag.TAG_NULL:
				case Asn1Tag.OBJECT_IDENTIFIER:
				case Asn1Tag.OBJECT_DESCRIPTOR:
				case Asn1Tag.EXTERNAL:
				case Asn1Tag.REAL:
				case Asn1Tag.ENUMERATED:
					{
						var blob = new Blob();
						blob.DefaultValue = new Variant(node.Data);

						return MakeAsn1Der(blob, node);
					}
				case Asn1Tag.SEQUENCE:
					return HandleSequence(parent, node);
				
				case Asn1Tag.SET:
					return HandleSequence(parent, node);
			}

			switch(node.Tag & Asn1TagClasses.CLASS_MASK)
			{
				case Asn1TagClasses.CONTEXT_SPECIFIC:
					return HandleSequence(parent, node);

				default:
					throw new PeachException("Error parsing ASN.1: " + node.Tag.ToString());
			}
		}
	}
}
