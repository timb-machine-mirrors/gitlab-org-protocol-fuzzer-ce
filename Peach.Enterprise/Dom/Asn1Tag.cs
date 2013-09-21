using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Enterprise.Analyzers.ASN1;
using System.IO;

namespace Peach.Enterprise.Dom
{
	[PitParsable("Asn1Tag")]
	[DataElement("Asn1Tag")]
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

				int n = 0;
				while ((value >> (n * 7)) > 0)
				{
					n++;
				}

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
