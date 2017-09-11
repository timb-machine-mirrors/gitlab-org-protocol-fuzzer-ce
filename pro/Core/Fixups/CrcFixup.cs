
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Mikhail Davidov (sirus@haxsys.net)
//	 Mick Ayzenberg	(mick@dejavusecurity.com)
//
// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Fixups.Libraries;

namespace Peach.Pro.Core.Fixups
{
	[Description("CRC Fixup library including CRC32 as defined by ISO 3309.")]
	[Fixup("Crc", true)]
	[Fixup("CrcFixup")]
	[Fixup("checksums.CrcFixup")]
	[Fixup("Crc32Fixup")]
	[Fixup("checksums.Crc32Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("type", typeof(CRCTool.CRCCode), "Type of CRC to run [CRC32, CRC32_16, CRC16, CRC16_Modbus, CRC_CCITT, DNP3, CRC8_MOD256]", "CRC32")]
	[Serializable]
	public class CrcFixup : Fixup
	{
		public DataElement _ref { get; protected set; }
		public CRCTool.CRCCode type { get; protected set; }

		public CrcFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];
			var data = elem.Value;

			data.Seek(0, System.IO.SeekOrigin.Begin);

			if (type == CRCTool.CRCCode.DNP3)
			{
				var buff = new byte[data.Length];
				data.Read(buff, 0, buff.Length);

				return new Variant((ushort)Crc16Dnp3.ComputeChecksum(buff));
			}

			if (type == CRCTool.CRCCode.CRC8_MOD256)
			{
				byte crc = 0;
				unchecked
				{
					while (data.Position < data.Length)
					{
						crc += (byte) data.ReadByte();
					}
				}

				return new Variant((byte)crc);
			}

			var crcTool = new CRCTool();
			crcTool.Init(type);

			return new Variant((uint)crcTool.crctablefast(data));
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return new Variant(0);
		}
	}
}

// end
