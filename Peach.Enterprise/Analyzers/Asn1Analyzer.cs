using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

//using Peach.Enterprise.Analyzers.ASN1;
using Peach.Enterprise.DataElements;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Cracker;

using Asn1Lib;

namespace Peach.Enterprise.Analyzers
{
	[Analyzer("Asn1", true)]
	[Description("Convert DER encoded ASN.1 into Peach Data model")]
	[Serializable]
	public class Asn1Analyzer : Analyzer
	{
//		string CurrentTag = null;

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

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			var blob = parent as Blob;

			if (blob == null)
				throw new PeachException("Error, Asn1 analyzer only operates on Blob elements!");

			var data = blob.Value;

			if (data.Length == 0)
				return;

			data.Seek(0, SeekOrigin.Begin);

			var asn1 = Asn1.Decode(data);

			var block = new Block(blob.name);

			// Move over relations
			foreach (var relation in blob.relations)
				block.relations.Add(relation);
			blob.relations.Clear();

			WalkAsn1Tree(block, asn1);

			// Replace blob with block
			block.parent = blob.parent;
			block.parent[blob.name] = block;
		}

		void WalkAsn1Tree(DataElementContainer parent, Asn1Tag tag)
		{
		}

		//static int NameCounter = 0;
		//DataElementContainer MakeAsn1Der(DataElement data, Asn1Node node)
		//{
		//    NameCounter++;

		//    var container = new Block(CurrentTag + NameCounter);
		//    var type = new Number("Type");
		//    var asn1Length = new Asn1DerLength("Asn1DerLength");
		//    var length = new Number("Length");
		//    var lengthRelation = new SizeRelation();

		//    type.length = 8;
		//    type.Signed = false;
		//    type.LittleEndian = false;
		//    type.DefaultValue = new Variant(node.Tag);

		//    length.length = 32;
		//    length.Signed = false;
		//    length.LittleEndian = false;
		//    length.DefaultValue = new Variant(node.DataLength);
		//    asn1Length.Add(length);

		//    container.Add(type);
		//    container.Add(asn1Length);
		//    container.Add(data);

		//    length.relations.Add(lengthRelation);
		//    data.relations.Add(lengthRelation);
		//    lengthRelation.FromName = length.name;
		//    lengthRelation.OfName = data.name;

		//    return container;
		//}

		//DataElementContainer HandleSequence(DataElementContainer parent, Asn1Node sequence)
		//{
		//    var block = new Block(CurrentTag + NameCounter);

		//    for (int count = 0; count < sequence.ChildNodeCount; count++)
		//    {
		//        var child = sequence.GetChildNode(count);
		//        block.Add(HandleAsn1Node(block, child));
		//    }

		//    return MakeAsn1Der(block, sequence);
		//}

		//DataElementContainer HandleBitString(DataElementContainer parent, Asn1Node sequence)
		//{
		//    var block = new Block(CurrentTag + NameCounter);

		//    var num = new Number();
		//    num.length = 8;
		//    num.Signed = false;
		//    num.LittleEndian = false;
		//    num.DefaultValue = new Variant(0);
		//    block.Add(num);

		//    if (sequence.ChildNodeCount == 0)
		//    {
		//        var data = new Blob();
		//        data.DefaultValue = new Variant(sequence.Data);
		//        block.Add(data);
		//    }
		//    else for (int count = 0; count < sequence.ChildNodeCount; count++)
		//        {
		//            var child = sequence.GetChildNode(count);
		//            block.Add(HandleAsn1Node(block, child));
		//        }

		//    return MakeAsn1Der(block, sequence);
		//}

		//DataElement HandleAsn1Node(DataElementContainer parent, Asn1Node node)
		//{
		//    NameCounter++;

		//    switch ((Asn1TagClasses)(node.Tag & (byte)Asn1TagClasses.CLASS_MASK))
		//    {
		//        case Asn1TagClasses.CONTEXT_SPECIFIC:
		//            CurrentTag = Asn1TagClasses.CONTEXT_SPECIFIC.ToString();
		//            return HandleSequence(parent, node);

		//    }

		//    CurrentTag = ((Asn1Tag)(node.Tag & (byte)Asn1Tag.TAG_MASK)).ToString();

		//    switch ((Asn1Tag)(node.Tag & (byte)Asn1Tag.TAG_MASK))
		//    {

		//        case Asn1Tag.PRINTABLE_STRING:
		//        case Asn1Tag.IA5_STRING:
		//            {
		//                var str = new Peach.Core.Dom.String("String");
		//                str.DefaultValue = new Variant(System.Text.Encoding.Default.GetString(node.Data));
		//                return MakeAsn1Der(str, node);
		//            }
		//        case Asn1Tag.UTC_TIME:
		//        case Asn1Tag.NUMERIC_STRING:
		//            {
		//                var numstr = new Peach.Core.Dom.String("NumericString");
		//                numstr.DefaultValue = new Variant(System.Text.Encoding.Default.GetString(node.Data));
		//                // TODO add hint for numeric
		//                return MakeAsn1Der(numstr, node);
		//            }
		//        case Asn1Tag.UTF8_STRING:
		//            {
		//                var ustr = new Peach.Core.Dom.String("Utf8String");
		//                ustr.stringType = StringType.utf8;
		//                ustr.DefaultValue = new Variant(System.Text.Encoding.Default.GetString(node.Data));
		//                return MakeAsn1Der(ustr, node);
		//            }

		//        case Asn1Tag.REAL:
		//        case Asn1Tag.INTEGER:
		//        // Issue cannot have bigger than 8 byte Number lengths... 
		//        /*{
		//            var num = new Number();
		//            num.length = node.Data.Length * 8;
		//            num.Signed = false;
		//            num.LittleEndian = false;
		//            num.DefaultValue = new Variant(node.Data);
		//            return MakeAsn1Der(num, node);
		//        }*/
		//        case Asn1Tag.T61_STRING:
		//        case Asn1Tag.VIDEOTEXT_STRING:
		//        case Asn1Tag.GENERALIZED_TIME:
		//        case Asn1Tag.GRAPHIC_STRING:
		//        case Asn1Tag.VISIBLE_STRING:
		//        case Asn1Tag.GENERAL_STRING:
		//        case Asn1Tag.UNIVERSAL_STRING:
		//        case Asn1Tag.BMPSTRING:
		//        case Asn1Tag.OCTET_STRING:
		//        case Asn1Tag.RELATIVE_OID:
		//        case Asn1Tag.BOOLEAN:
		//        case Asn1Tag.TAG_NULL:
		//        case Asn1Tag.OBJECT_IDENTIFIER:
		//        case Asn1Tag.OBJECT_DESCRIPTOR:
		//        case Asn1Tag.EXTERNAL:

		//        case Asn1Tag.ENUMERATED:
		//            {
		//                var blob = new Blob("Blob" + NameCounter);
		//                blob.DefaultValue = new Variant(node.Data);

		//                return MakeAsn1Der(blob, node);
		//            }

		//        case Asn1Tag.BIT_STRING: // TODO Double check this
		//            return HandleBitString(parent, node);

		//        case Asn1Tag.SEQUENCE:
		//            return HandleSequence(parent, node);

		//        case Asn1Tag.SET:
		//            return HandleSequence(parent, node);
		//        default:
		//            throw new PeachException("Error parsing ASN.1: " + node.Tag.ToString());
		//    }

		//}
	}
}
