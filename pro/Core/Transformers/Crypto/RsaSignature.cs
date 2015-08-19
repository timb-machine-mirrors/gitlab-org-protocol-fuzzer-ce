using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NLog;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Transformer("RsaSignature", true, Internal = true)]
	[Description("RSA signature")]
	[Parameter("Modulus", typeof(HexString), "Private key modulus", "")]
	[Parameter("Exponent", typeof(HexString), "Public key exponent", "010001")]
	[Parameter("PrivateKey", typeof(string), "Private key file in PEM format", "")]
	[Parameter("HashAlgorithm", typeof(string), "Hash algorithm to use [Raw, Combined, MD2, MD4, MD5, SHA-1, SHA-224. SHA-256, SHA-384, SHA-512]", "MD5")]
	[Serializable]
	public class RsaSignature : Transformer
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		public HexString Modulus { get; set; }
		public HexString Exponent { get; set; }
		public string PrivateKey { get; set; }
		public string HashAlgorithm { get; set; }

		public RsaSignature(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			RsaKeyParameters rsaServerPrivateKey = null;

			if (!string.IsNullOrEmpty(PrivateKey))
			{
				using(var stream = new FileStream(PrivateKey, FileMode.Open))
				using (var textReader = new StreamReader(stream))
				{
					var pem = new PemReader(textReader);
					var pemObj = pem.ReadObject();

					if (pemObj is AsymmetricCipherKeyPair)
					{
						var keys = (AsymmetricCipherKeyPair) pemObj;
						rsaServerPrivateKey = (RsaKeyParameters) keys.Private;
					}
					else if (pemObj is RsaPrivateCrtKeyParameters)
					{
						rsaServerPrivateKey = (RsaKeyParameters)pemObj;
					}
					else
					{
						throw new PeachException("Error, PrivateKey for RsaSignature transformer is in unknown format.");
					}
				}
			}
			else
			{
				var modulus = new BigInteger(Modulus.Value);
				var exponent = new BigInteger(Exponent.Value);

				rsaServerPrivateKey = new RsaKeyParameters(false, modulus, exponent);
			}

			var clear = new BitReader(data).ReadBytes((int)data.Length);

			ISigner sig;

			if (HashAlgorithm.Equals("Raw"))
			{
				var digest = new NullDigest();
				sig = new GenericSigner(new Pkcs1Encoding(new RsaBlindedEngine()), digest);
			}
			else if (HashAlgorithm.Equals("Combined"))
			{
				var digest = new CombinedHash();
				sig = new GenericSigner(new Pkcs1Encoding(new RsaBlindedEngine()), digest);
			}
			else
			{
				sig = SignerUtilities.GetSigner(string.Format("{0}withRSA", HashAlgorithm));
				if (sig == null)
				{
					throw new PeachException(
						string.Format("Error, unable to locate RSA signature algorithm '{0}'. Please verify this algorithm is supported.",
						HashAlgorithm));
				}
			}

			sig.Init(true, rsaServerPrivateKey);
			sig.BlockUpdate(clear, 0, clear.Length);
			var signature = sig.GenerateSignature();

			Console.Write(string.Format("RSA SIGN({0}): ", signature.Length));
			foreach (var b in signature)
				Console.Write(string.Format("{0:X2} ", b));
			Console.WriteLine();

			return new BitStream(signature);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			return data;
		}

		internal class CombinedHash
	: TlsHandshakeHash
		{
			protected TlsContext mContext;
			protected IDigest mMd5;
			protected IDigest mSha1;

			internal CombinedHash()
			{
				this.mMd5 = TlsUtilities.CreateHash(Org.BouncyCastle.Crypto.Tls.HashAlgorithm.md5);
				this.mSha1 = TlsUtilities.CreateHash(Org.BouncyCastle.Crypto.Tls.HashAlgorithm.sha1);
			}

			internal CombinedHash(CombinedHash t)
			{
				this.mContext = t.mContext;
				this.mMd5 = TlsUtilities.CloneHash(Org.BouncyCastle.Crypto.Tls.HashAlgorithm.md5, t.mMd5);
				this.mSha1 = TlsUtilities.CloneHash(Org.BouncyCastle.Crypto.Tls.HashAlgorithm.sha1, t.mSha1);
			}

			public virtual void Init(TlsContext context)
			{
				this.mContext = context;
			}

			public virtual TlsHandshakeHash NotifyPrfDetermined()
			{
				return this;
			}

			public virtual void TrackHashAlgorithm(byte hashAlgorithm)
			{
				throw new InvalidOperationException("CombinedHash only supports calculating the legacy PRF for handshake hash");
			}

			public virtual void SealHashAlgorithms()
			{
			}

			public virtual TlsHandshakeHash StopTracking()
			{
				return new CombinedHash(this);
			}

			public virtual IDigest ForkPrfHash()
			{
				return new CombinedHash(this);
			}

			public virtual byte[] GetFinalHash(byte hashAlgorithm)
			{
				throw new InvalidOperationException("CombinedHash doesn't support multiple hashes");
			}

			public virtual string AlgorithmName
			{
				get { return mMd5.AlgorithmName + " and " + mSha1.AlgorithmName; }
			}

			public virtual int GetByteLength()
			{
				return System.Math.Max(mMd5.GetByteLength(), mSha1.GetByteLength());
			}

			public virtual int GetDigestSize()
			{
				return mMd5.GetDigestSize() + mSha1.GetDigestSize();
			}

			public virtual void Update(byte input)
			{
				mMd5.Update(input);
				mSha1.Update(input);
			}

			/**
			 * @see org.bouncycastle.crypto.Digest#update(byte[], int, int)
			 */
			public virtual void BlockUpdate(byte[] input, int inOff, int len)
			{
				mMd5.BlockUpdate(input, inOff, len);
				mSha1.BlockUpdate(input, inOff, len);
			}

			/**
			 * @see org.bouncycastle.crypto.Digest#doFinal(byte[], int)
			 */
			public virtual int DoFinal(byte[] output, int outOff)
			{
				//if (mContext != null && TlsUtilities.IsSsl(mContext))
				//{
				//	Ssl3Complete(mMd5, Ssl3Mac.IPAD, Ssl3Mac.OPAD, 48);
				//	Ssl3Complete(mSha1, Ssl3Mac.IPAD, Ssl3Mac.OPAD, 40);
				//}

				int i1 = mMd5.DoFinal(output, outOff);
				int i2 = mSha1.DoFinal(output, outOff + i1);
				return i1 + i2;
			}

			/**
			 * @see org.bouncycastle.crypto.Digest#reset()
			 */
			public virtual void Reset()
			{
				mMd5.Reset();
				mSha1.Reset();
			}

			protected virtual void Ssl3Complete(IDigest d, byte[] ipad, byte[] opad, int padLength)
			{
				byte[] master_secret = mContext.SecurityParameters.MasterSecret;

				d.BlockUpdate(master_secret, 0, master_secret.Length);
				d.BlockUpdate(ipad, 0, padLength);

				byte[] tmp = DigestUtilities.DoFinal(d);

				d.BlockUpdate(master_secret, 0, master_secret.Length);
				d.BlockUpdate(opad, 0, padLength);
				d.BlockUpdate(tmp, 0, tmp.Length);
			}
		}

	}

}
