

using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("UNIX style crypt.")]
	[Transformer("Crypt", true)]
	[Transformer("crypto.Crypt")]
	[Serializable]
	public class Crypt : Transformer
	{
		public Crypt(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var reader = new BitReader(data);
			string dataAsString = reader.ReadString();
			string salt = dataAsString.Substring(0, 2);
			string result = UnixCryptTool.Crypt(salt, dataAsString);

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteString(result);
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
