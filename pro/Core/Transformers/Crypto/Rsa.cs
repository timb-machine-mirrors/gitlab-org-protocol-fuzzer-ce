using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NLog;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Transformer("Rsa", true, Internal = true)]
	[Description("RSA encryption and decryption")]
	[Parameter("PublicKey", typeof(HexString), "Public key modulus", "")]
	[Parameter("CertXpath", typeof(string), "XPath to collection of certificates in handshake TLS message", "")]
	[Parameter("PublicExponent", typeof(HexString), "Public key exponent", "010001")]
	[Serializable]
	public class Rsa : Transformer
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		public HexString PublicKey { get; set; }
		public HexString PublicExponent { get; set; }
		public string CertXpath { get; set; }

		public Rsa(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		/// <summary>
		/// Find the cert via xpath and parse out the RsaKeyParameters
		/// </summary>
		/// <param name="xpath"></param>
		/// <returns></returns>
		protected virtual RsaKeyParameters FromCertXPath(string xpath)
		{
			var context = ((DataModel) parent.getRoot()).actionData.action.parent.parent.parent.context;

			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(context.dom);
			var iter = navi.Select(xpath, resolver);

			DataElement certElement = null;

			while (iter.MoveNext())
			{
				var valueElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (valueElement == null)
					throw new SoftException("Error, CertXpath did not return a Data Element. [" + xpath + "]");

				if (valueElement.InScope())
				{
					certElement = valueElement;
					break;
				}
			}

			if (certElement == null)
				throw new SoftException("Error, CertXpath did not return a Data Element. [" + xpath + "].");

			var certStream = certElement.Value;
			if (certStream.Length < 10)
				return null;

			certStream.Position = 0;
			var serverCertificate = Certificate.Parse(certStream);

			if (serverCertificate == null || serverCertificate.IsEmpty)
				throw new SoftException("Error, No certificates found via CertXpath. [" + xpath + "].");

			var x509Cert = serverCertificate.GetCertificateAt(0);
			var keyInfo = x509Cert.SubjectPublicKeyInfo;

			try
			{
				return (RsaKeyParameters) PublicKeyFactory.CreateKey(keyInfo);
			}
			catch (Exception)
			{
				throw new SoftException("Error, Rsa transformer found an unsupported cert via CertXpath. [" + xpath + "].");
			}
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			RsaKeyParameters rsaServerPublicKey = null;

			if (!string.IsNullOrEmpty(CertXpath))
			{
				rsaServerPublicKey = FromCertXPath(CertXpath);

				// During record prior to getting data the cert can be null
				// in this case just return our clear text data.
				if (rsaServerPublicKey == null)
					return data;
			}
			else
			{
				var modulus = new BigInteger(PublicKey.Value);
				var exponent = new BigInteger(PublicExponent.Value);

				rsaServerPublicKey = new RsaKeyParameters(false, modulus, exponent);
			}

			var random = new SecureRandom();
			var encoding = new Pkcs1Encoding(new RsaBlindedEngine());
			encoding.Init(true, new ParametersWithRandom(rsaServerPublicKey, random));

			if (data.Length > encoding.GetInputBlockSize())
			{
				logger.Error("Data length greater than block size, returning unencrypted data");
				return data;
			}

			var clear = new BitReader(data).ReadBytes((int)data.Length);

			Console.Write(string.Format("RSA ENCRYPT({0}): ", clear.Length));
			foreach (var b in clear)
				Console.Write(string.Format("{0:X2} ", b));
			Console.WriteLine();

			var encrypted = encoding.ProcessBlock(clear, 0, clear.Length);
			return new BitStream(encrypted);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			RsaKeyParameters rsaServerPublicKey = null;

			if (!string.IsNullOrEmpty(CertXpath))
			{
				rsaServerPublicKey = FromCertXPath(CertXpath);

				// During record prior to getting data the cert can be null
				// in this case just return our clear text data.
				if (rsaServerPublicKey == null)
					return data;
			}
			else
			{
				var modulus = new BigInteger(PublicKey.Value);
				var exponent = new BigInteger(PublicExponent.Value);

				rsaServerPublicKey = new RsaKeyParameters(false, modulus, exponent);
			}

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
