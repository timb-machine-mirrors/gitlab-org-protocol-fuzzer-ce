using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Cracker;

using Peach.Enterprise.Dom;

namespace Peach.Enterprise.Analyzers
{
	[Analyzer("Asn1", true)]
	[Description("Convert BER encoded ASN.1 into Peach Data model")]
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

			var block = new Block(blob.name);

			// Decode ASN.1
			Decode(block, data, positions, 0);

			// Replace blob with block
			blob.parent[blob.name] = block;

			// Copy over positions
			if (positions != null)
				positions[block] = new Position(0, data.LengthBits);
		}

		#region Asn1Node

		/// <summary>
		/// Helper class for parsing the Identifier & Length from a stream
		/// </summary>
		class Asn1Node
		{
			public enum ClassType
			{
				UNIVERSAL = 0,
				APPLICATION = 1,
				CONTEXT_SPECIFIC = 2,
				PRIVATE = 3,
			}

			public enum ClassTag
			{
				UNKNOWN = 0,
				BOOLEAN = 1,
				INTEGER = 2,
				BIT_STRING = 3,
				OCTET_STRING = 4,
				NULL = 5,
				OBJECT_IDENTIFIER = 6,
				OBJECT_DESCRIPTOR = 7,
				EXTERNAL = 8,
				REAL = 9,
				ENUMERATED = 10,
				EMBEDDED_PDV = 11,
				UTF8_STRING = 12,
				RELATIVE_OID = 13,
				RESERVED_A = 14,
				RESERVED_B = 15,
				SEQUENCE = 16,
				SET = 17,
				NUMERIC_STRING = 18,
				PRINTABLE_STRING = 19,
				T61_STRING = 20,
				VIDEOTEX_STRING = 21,
				IA5_STRING = 22,
				UTC_TIME = 23,
				GENERALIZED_TIME = 24,
				GRAPHIC_STRING = 25,
				VISIBLE_STRING = 26,
				GENERAL_STRING = 27,
				UNIVERSAL_STRING = 28,
				CHARACTER_STRING = 29,
				BMP_STRING = 30,
				NON_PRIMITIVE = 31,
			}

			public static Asn1Node Parse(BitwiseStream stream)
			{
				var node = new Asn1Node();

				node.StartPos = stream.Position;
				node.ReadIdentifier(stream);
				node.LengthPos = stream.Position;
				node.ReadLength(stream);
				node.DataPos = stream.Position;
				node.EndPos = node.DataPos + node.Length;

				node.IsEOC =
					node.Class == ClassType.UNIVERSAL &&
					node.Constructed == false &&
					node.TagNumber == 0 &&
					node.IndefiniteLength == false &&
					node.Length == 0;

				if (node.Class != ClassType.UNIVERSAL)
					node.Description = node.Class.ToString();
				else if (node.IsEOC)
					node.Description = "EOC";
				else if (node.TagNumber >= (ulong)ClassTag.NON_PRIMITIVE)
					node.Description = ClassTag.NON_PRIMITIVE.ToString();
				else
					node.Description = ((ClassTag)node.TagNumber).ToString();

				return node;
			}

			private Asn1Node()
			{
			}

			private void ReadIdentifier(BitwiseStream stream)
			{
				var b = stream.ReadByte();
				if (b < 0)
					throw new IOException("Unexpected end of ASN.1 data while reading identifier.");

				//  8  7 | 6 | 5  4  3  2  1
				// Class |P/C|   Tag number
				Class = (ClassType)(b >> 6);
				Constructed = (b & 0x20) != 0;

				if ((b & 0x1f) != 0x1f)
				{
					// Single byte identifier
					TagNumber = (byte)(b & 0x1f);
					ForceMultiByteIdentifier = false;
				}
				else
				{
					// Multi byte identifier
					TagNumber = 0;
					ForceMultiByteIdentifier = true;

					var availBits = 64;

					do
					{
						if (availBits < 7)
							throw new IOException("Overflow detected while reading multi-byte identifier.");

						b = stream.ReadByte();

						if (b < 0)
							throw new IOException("Unexpected end of ASN.1 data while reading multi-byte identifier.");

						TagNumber <<= 7;
						TagNumber |= (byte)(b & 0x7F);

						if (TagNumber == 0)
							throw new IOException("Invalid ASN.1 identifier while reading multi-byte identifier).");

						availBits -= 7;
					}
					while ((b & 0x80) != 0);
				}
			}

			private void ReadLength(BitwiseStream stream)
			{
				var b = stream.ReadByte();
				if (b == -1)
					throw new IOException("No bytes available to read length.");

				if ((b & 0x80) == 0)
				{
					// Short form length
					LongLength = false;
					IndefiniteLength = false;
					Length = b & 0x7f;
				}
				else
				{
					// Long form length
					LongLength = true;

					if ((b ^ 0x80) == 0)
					{
						// indefinte length; terminated by EOC
						IndefiniteLength = true;
						Length = 0;
					}
					else
					{
						IndefiniteLength = false;

						var len = b ^ 0x80;
						if (len > 8)
							throw new IOException("Can't handle length field of more than 8 bytes.");

						var buf = new byte[len];
						len = stream.Read(buf, 0, buf.Length);

						if (len != buf.Length)
							throw new IOException("Not enough bytes to read length.");

						Length = 0;

						for (int i = 0; i < buf.Length; ++i)
						{
							Length <<= 8;
							Length |= buf[i];
						}

						// Guard against signed/unsigned overflow
						if (Length < 0)
							throw new IOException("Can't handle length field larger than max signed 64-bit number.");

						// Guard against overflowing the stream
						if (stream.Position > long.MaxValue - Length)
							throw new IOException("Overflow detected trying to read " + Length.ToString() + " bytes.");
					}
				}
			}

			public string Description;

			public long StartPos;
			public long LengthPos;
			public long DataPos;
			public long EndPos;

			public ClassType Class;
			public bool Constructed;
			public bool ForceMultiByteIdentifier;
			public ulong TagNumber;
			public bool IndefiniteLength;
			public bool LongLength;
			public long Length;
			public bool IsEOC;
		}

		#endregion

		private static Asn1Node Decode(DataElementContainer parent, BitwiseStream stream, Dictionary<DataElement, Position> positions, long offset)
		{
			// Parse the Identifier & Length out of the stream
			var node = Asn1Node.Parse(stream);

			// Ensure the node description is unique
			node.Description = parent.UniqueName(node.Description);

			// Build the dom elements for the Identifier & Length
			var cont = MakeElements(node, positions, offset);

			if (node.Constructed)
			{
				// Constructed nodes are always made up of child ASN.1 nodes
				var value = new Block("Value");

				while (stream.Position < node.EndPos || node.IndefiniteLength)
				{
					var child = Decode(value, stream, positions, offset);
					if (child.IsEOC)
						break;
				}

				cont.Add(value);
				node.EndPos = stream.Position;
			}
			else
			{
				// Primitive BitStream and OctetStream nodes might be made up of
				// child ASN.1 Nodes.  Try and recurse and if an error occurs
				// just use a blob.

				var data = ReadData(stream, node.Length);

				var tag = Asn1Node.ClassTag.UNKNOWN;

				if (node.Class == Asn1Node.ClassType.UNIVERSAL && node.TagNumber < (ulong)Asn1Node.ClassTag.NON_PRIMITIVE)
					tag = (Asn1Node.ClassTag)node.TagNumber;

				switch (tag)
				{
					case Asn1Node.ClassTag.BIT_STRING:
						cont.Add(TryDecode(data, true, positions, offset + node.DataPos));
						break;
					case Asn1Node.ClassTag.OCTET_STRING:
						cont.Add(TryDecode(data, false, positions, offset + node.DataPos));
						break;
					case Asn1Node.ClassTag.PRINTABLE_STRING:
					case Asn1Node.ClassTag.IA5_STRING:
					case Asn1Node.ClassTag.UTC_TIME:
					case Asn1Node.ClassTag.NUMERIC_STRING:
						cont.Add(new Peach.Core.Dom.String("Value")
						{
							// NumericString hint is automatically added if value is a number
							stringType = StringType.ascii,
							DefaultValue = new Variant(data),
						});
						break;
					case Asn1Node.ClassTag.UTF8_STRING:
						cont.Add(new Peach.Core.Dom.String("Value")
						{
							stringType = StringType.utf8,
							DefaultValue = new Variant(data),
						});
						break;
					default:
						cont.Add(new Blob("Value")
						{
							DefaultValue = new Variant(data)
						});
						break;
				}
			}

			if (positions != null)
			{
				var startPos = (offset + node.StartPos) * 8;
				var endPos = (offset + node.EndPos) * 8;
				var dataPos = (offset + node.DataPos) * 8;

				positions[cont] = new Position(startPos, endPos);
				positions[cont["Value"]] = new Position(dataPos, endPos);
			}

			if (!node.IndefiniteLength)
			{
				var rel = new SizeRelation(cont["Length"]);
				rel.Of = cont["Value"];
			}

			parent.Add(cont);

			return node;
		}

		static DataElementContainer MakeElements(Asn1Node node, Dictionary<DataElement, Position> positions, long offset)
		{
			var root = new Block(node.Description);

			var tag = new Block("Identifier");

			var cls = new Number("Class")
			{
				lengthType = LengthType.Bits,
				length = 2,
				DefaultValue = new Variant((int)node.Class),
			};

			var pc = new Number("PC")
			{
				lengthType = LengthType.Bits,
				length = 1,
				DefaultValue = new Variant(node.Constructed ? 1 : 0),
			};

			var num = new Asn1Tag("Tag")
			{
				DefaultValue = new Variant(node.TagNumber),
				ForceMultiByteIdentifier = node.ForceMultiByteIdentifier,
			};

			var len = new Asn1Length(node.IndefiniteLength ? "IndefiniteLength" : "Length")
			{
				IndefiniteLength = node.IndefiniteLength,
				LongLength = node.LongLength,
				DefaultValue = new Variant(node.Length)
			};

			tag.Add(cls);
			tag.Add(pc);
			tag.Add(num);

			root.Add(tag);
			root.Add(len);

			if (positions != null)
			{
				var startPos = (offset + node.StartPos) * 8;
				var lenPos = (offset + node.LengthPos) * 8;
				var dataPos = (offset + node.DataPos) * 8;

				positions[tag] = new Position(startPos, dataPos);
				positions[cls] = new Position(startPos, startPos + 2);
				positions[pc] = new Position(startPos + 2, startPos + 3);
				positions[num] = new Position(startPos + 3, lenPos);
				positions[len] = new Position(lenPos, dataPos);
			}

			return root;
		}

		static DataElement TryDecode(BitStream stream, bool unused, Dictionary<DataElement, Position> positions, long offset)
		{
			if (unused)
			{
				int b = stream.ReadByte();
				if (b < 0)
					throw new IOException("Unable to read unused bit count.");

				var unusedLen = new Number("UnusedLen")
				{
					lengthType = LengthType.Bits,
					length = 8,
					DefaultValue = new Variant(b),
				};

				if (stream.LengthBits < (8 + b))
					throw new IOException("Unused bit length longer than data.");

				var slice = stream.SliceBits(stream.LengthBits - (8 + b));

				DataElement value;

				if (b == 0)
					value = TryDecode(slice, false, positions, offset + 1);
				else
					value = new Blob("Value") { DefaultValue = new Variant(slice) };

				var unusedBits = new Blob("UnusedBits")
				{
					DefaultValue = new Variant(stream.SliceBits(b))
				};

				var pad = new Padding("Padding") { alignment = 8 };

				var blk = new Block("Value");
				blk.Add(unusedLen);
				blk.Add(value);
				blk.Add(unusedBits);
				blk.Add(pad);

				var rel = new SizeRelation(unusedLen) { lengthType = LengthType.Bits };
				rel.Of = unusedBits;

				if (positions != null)
				{
					var startPos = offset * 8;
					var dataPos = startPos + 8;
					var padPos = startPos + stream.LengthBits - b;
					var endPos = padPos + b;

					positions[unusedLen] = new Position(startPos, dataPos);
					positions[value] = new Position(dataPos, padPos);
					positions[unusedBits] = new Position(padPos, endPos);
					positions[pad] = new Position(endPos, endPos);
				}

				return blk;
			}

			try
			{
				var blk = new Block("Value");
				Decode(blk, stream, positions, offset);
				return blk;
			}
			catch (IOException)
			{
				return new Blob("Value") { DefaultValue = new Variant(stream) };
			}
		}

		static BitStream ReadData(BitwiseStream stream, long length)
		{
			var ret = new BitStream();
			var buf = new byte[BitwiseStream.BlockCopySize];

			while (length > 0)
			{
				var len = stream.Read(buf, 0, (int)Math.Min(buf.Length, length));
				if (len == 0)
					throw new IOException("Not enough data available.");

				ret.Write(buf, 0, len);
				length -= len;
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}
