

using System;
using System.Collections.Generic;
using System.Globalization;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Type
{
	[Description("Transforms an integer into hex.")]
	[Transformer("IntToHex", true)]
	[Transformer("type.IntToHex")]
	[Serializable]
	public class IntToHex : Transformer
	{
		public IntToHex(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			try
			{

				string dataAsStr = new BitReader(data).ReadString();
				int dataAsInt = Int32.Parse(dataAsStr);
				string dataAsHexStr = dataAsInt.ToString("X");
				var ret = new BitStream();
				var writer = new BitWriter(ret);
				writer.WriteString(dataAsHexStr);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}

		protected override BitStream internalDecode(BitStream data)
		{
			try
			{
				string dataAsHexStr = new BitReader(data).ReadString();
				int dataAsInt = Int32.Parse(dataAsHexStr, NumberStyles.HexNumber);
				string dataAsStr = dataAsInt.ToString();
				var ret = new BitStream();
				var writer = new BitWriter(ret);
				writer.WriteString(dataAsStr);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}
	}
}

// end
