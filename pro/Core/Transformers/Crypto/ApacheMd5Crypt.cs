

using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("Apache style MD5 crypt.")]
	[Transformer("ApacheMd5Crypt", true)]
	[Transformer("crypto.ApacheMd5Crypt")]
	[Serializable]
	public class ApacheMd5Crypt : Transformer
	{
		public ApacheMd5Crypt(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var reader = new BitReader(data);
			string dataAsString = reader.ReadString();
			string salt = dataAsString.Substring(0, 2);
			string result = UnixMd5CryptTool.crypt(dataAsString, salt, "$apr1$");

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
