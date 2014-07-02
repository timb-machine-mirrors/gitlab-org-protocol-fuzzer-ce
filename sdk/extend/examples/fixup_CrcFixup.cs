using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.Fixups.Libraries;
using System.Runtime.Serialization;

namespace Peach.Core.Fixups
{
	[Description("CRC Fixup library including CRC32 as defined by ISO 3309.")]
	[Fixup("Crc", true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("type", typeof(CRCTool.CRCCode), "Type of CRC to run [CRC32, CRC16, CRC_CCITT]", "CRC32")]
	[Serializable]
	public class CrcFixup : Fixup
	{
		static void Parse(string str, out DataElement val)
		{
			val = null;
		}

		protected DataElement _ref { get; set; }
		protected CRCTool.CRCCode type { get; set; }

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

			CRCTool crcTool = new CRCTool();
			crcTool.Init(type);

			return new Variant((uint)crcTool.crctablefast(data));
		}
	}
}

// end
