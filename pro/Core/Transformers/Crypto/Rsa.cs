using System;
using System.Collections.Generic;
using NLog;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Transformer("Rsa", true, Internal = true)]
	[Description("RSA encryption and decryption")]
	[Parameter("PublicKey", typeof(HexString), "Public key modulus", "")]
	[Parameter("PublicExponent", typeof(HexString), "Public key exponent", "010001")]
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
				logger.Error("Data length greater than block size, returning unencrypted data");
				return data;
			}

			var clear = new BitReader(data).ReadBytes((int)data.Length);
			var encrypted = encoding.ProcessBlock(clear, 0, clear.Length);

			return new BitStream(encrypted);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			var modulus = new BigInteger(PublicKey.Value);
			var exponent = new BigInteger(PublicExponent.Value);

			var rsaServerPublicKey = new RsaKeyParameters(false, modulus, exponent);

			var random = new SecureRandom();
			var encoding = new Pkcs1Encoding(new RsaBlindedEngine());
			encoding.Init(false, new ParametersWithRandom(rsaServerPublicKey, random));

			if (data.Length > encoding.GetInputBlockSize())
			{
				logger.Error("Data length greater than block size, returning encrypted data");
				return data;
			}

			var cipher = new BitReader(data).ReadBytes((int)data.Length);
			var clear = encoding.ProcessBlock(cipher, 0, cipher.Length);

			return new BitStream(clear);
		}
	}
}
