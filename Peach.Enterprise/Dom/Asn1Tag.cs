using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.IO;

namespace Peach.Enterprise.Dom
{
	[PitParsable("Asn1Tag")]
	[DataElement("Asn1Tag", DataElementTypes.NonDataElements)]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("forceMultiByteIdentifier", typeof(bool), "Use multibyte encoding", "false")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(Peach.Core.Dom.ValueType), "Format of value attribute", "string")]
	[Serializable]
	public class Asn1Tag : Number
	{
		bool _forceMultiByteIdentifier = false;

		public Asn1Tag()
			: base()
		{
			lengthType = LengthType.Bits;
			length = 64;
			Signed = false;
			LittleEndian = false;
		}

		public Asn1Tag(string name)
			: base(name)
		{
			lengthType = LengthType.Bits;
			length = 64;
			Signed = false;
			LittleEndian = false;
		}

		public bool ForceMultiByteIdentifier
		{
			get
			{
				return _forceMultiByteIdentifier;
			}
			set
			{
				if (value != _forceMultiByteIdentifier)
				{
					_forceMultiByteIdentifier = value;
					Invalidate();
				}
			}
		}

		public override bool hasLength
		{
			get 
			{ 
				 return false;
			}
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			var value = (ulong)InternalValue;

			var ret = new BitStream();

			if (value < 31 && !ForceMultiByteIdentifier)
			{
				ret.WriteBits(value, 5);
			}
			else
			{
				ret.WriteBits(0x1f, 5);

				int n = 1;
				for (ulong tmp = value; tmp > 0x7f; tmp >>= 7)
					++n;

				while (n-- > 0)
				{
					byte b = (byte)((value >> (n * 7)) & (0x7F));
					if (n >= 1) b |= 0x80;
					ret.WriteByte(b);
				}
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}
