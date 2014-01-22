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
	[PitParsable("Asn1Length")]
	[DataElement("Asn1Length", DataElementTypes.NonDataElements)]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("indefiniteLength", typeof(bool), "Use indefinite form encoding", "false")]
	[Parameter("longLength", typeof(bool), "Always use long form encoding", "false")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(Peach.Core.Dom.ValueType), "Format of value attribute", "string")]
	[Serializable]
	public class Asn1Length : Number
	{
		bool _indefiniteLength = false;
		bool _longLength = false;

		public Asn1Length()
			: base()
		{
			// Signed is true since C# streams use long for sizes
			lengthType = LengthType.Bits;
			length = 64;
			Signed = true;
			LittleEndian = false;
		}

		public Asn1Length(string name)
			: base(name)
		{
			// Signed is true since C# streams use long for sizes
			lengthType = LengthType.Bits;
			length = 64;
			Signed = true;
			LittleEndian = false;
		}

		public bool IndefiniteLength
		{
			get
			{
				return _indefiniteLength;
			}
			set
			{
				if (value != _indefiniteLength)
				{
					_indefiniteLength = value;
					Invalidate();
				}
			}
		}

		public bool LongLength
		{
			get
			{
				return _longLength;
			}
			set
			{
				if (value != _longLength)
				{
					_longLength = value;
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

			if (IndefiniteLength)
			{
				ret.WriteByte(0x80);
			}
			else if (value <= 127 && !LongLength)
			{
				ret.WriteByte((byte)value);
			}
			else
			{
				int n = 0;
				while ((value >> (n * 8)) > 0)
				{
					n++;
				}

				ret.WriteByte((byte)(0x80 | n));

				for (int i = 0; i < n; ++i)
				{
					byte b = (byte)((value >> (8 * (n - i - 1))) & 0xFF);
					ret.WriteByte(b);
				}
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}
