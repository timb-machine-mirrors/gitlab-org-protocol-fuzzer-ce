using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard MD5 checksum.")]
	[Fixup("Md5", true)]
	[Fixup("MD5Fixup")]
	[Fixup("checksums.MD5Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class MD5Fixup : Fixup
	{
		public HexString DefaultValue { get; protected set; }
		public DataElement _ref { get; protected set; }

		public MD5Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var data = from.Value;
			var hashTool = new MD5CryptoServiceProvider();

			data.Seek(0, System.IO.SeekOrigin.Begin);

			var hash = hashTool.ComputeHash(data);
			return new Variant(new BitStream(hash));
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return DefaultValue != null ? new Variant(DefaultValue.Value) : base.GetDefaultValue(obj);
		}
	}
}
