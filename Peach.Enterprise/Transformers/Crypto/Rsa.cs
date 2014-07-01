
using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Linq;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;

using NLog;

namespace Peach.Core.Transformers.Crypto
{
	[Description("Rsa Transformer")]
	[Transformer("Rsa", true)]
	[Parameter("PublicKey", typeof(HexString), "Public key used to encrypt", "")]
	[Parameter("PublicExponent", typeof(HexString), "PublicKeyExponent", "010001")]
	[Serializable]
	public class Rsa : Transformer
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		public HexString PublicKey { get; set; }
		public HexString PublicExponent { get; set; }

		public Rsa(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var modulus = new BigInteger(PublicKey.Value);
			var exponent = new BigInteger(PublicExponent.Value);

			var rsaServerPublicKey = new RsaKeyParameters(false, modulus, exponent);

			var random = new SecureRandom();
			var encoding = new Pkcs1Encoding(new RsaBlindedEngine());
			encoding.Init(true, new ParametersWithRandom(rsaServerPublicKey, random));

			if (data.Length > encoding.GetInputBlockSize())
			{
				logger.Debug("Data length greater than block size, returning unencrypted data");
				return data;
			}

			var clear = new BitReader(data).ReadBytes((int)data.Length);
			var encrypted = encoding.ProcessBlock(clear, 0, clear.Length);

			return new BitStream(encrypted);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}
