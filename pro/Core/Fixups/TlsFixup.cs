using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Array = System.Array;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Fixups
{
	[Description("Tls")]
	[Fixup("Tls", true, Internal = true)]
	[Parameter("TlsVersion", typeof(TlsVersion), "TLS Version", "TLSv10")]
	[Parameter("IsServer", typeof(bool), "Is this the server side?", "false")]
	[Serializable]
	public class TlsFixup : Peach.Core.Fixups.VolatileFixup
	{
		#region Static Helpers For PRF()

		internal static byte[] ToBytes(object obj, bool useValue = false, bool useDefault = false)
		{
			if (obj == null)
				return null;


			var intern = (DataElement)obj;
			if(useValue)
				intern.Invalidate();

			var bs = useValue ? intern.Value : (BitwiseStream)intern.InternalValue;
			if (useDefault)
				bs = (BitwiseStream) intern.DefaultValue;

			var pos = bs.PositionBits;
			bs.Seek(0, SeekOrigin.Begin);
			var ret = new byte[bs.Length];

			bs.Read(ret, 0, ret.Length);
			bs.Seek(pos, SeekOrigin.Begin);

			return ret;
		}

		/// <summary>
		/// Build a buffer that matches the input for KeyExchange.
		/// </summary>
		/// <remarks>
		/// There was no easy way to directly get this from the data model. The
		/// inner key must still be encrypted.
		/// </remarks>
		/// <param name="obj"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		internal static byte[] KeyExchangeToBytes(object obj, RunContext context)
		{
			byte[] buff;
			var payload = (DataElementContainer) obj;

			var cipher = (byte[])context.iterationStateStore["ClientKeyExchangeInput"];

			var lengthElem = (Number) payload["Length"];
			var length = new Number();
			length.length = lengthElem.length;
			length.lengthType = lengthElem.lengthType;
			length.LittleEndian = lengthElem.LittleEndian;

			var keyLengthElem = (Number)payload.find("RSAEncryptedPreMasterSecret.PubKeyength");
			var keyLength = new Number();
			keyLength.length = keyLengthElem.length;
			keyLength.lengthType = keyLengthElem.lengthType;
			keyLength.LittleEndian = keyLengthElem.LittleEndian;

			keyLength.DefaultValue = new Variant(cipher.Length);
			length.DefaultValue = new Variant(cipher.Length + keyLength.Value.Length);

			var sout = new MemoryStream();
			buff = ToBytes(payload["HandshakeType"], true);
			sout.Write(buff, 0, buff.Length);
			buff = ToBytes(length, true);
			sout.Write(buff, 0, buff.Length);
			buff = ToBytes(keyLength, true);
			sout.Write(buff, 0, buff.Length);
			sout.Write(cipher, 0, cipher.Length);

			return sout.ToArray();
		}

		internal static byte[] Concat(byte[] a, byte[] b)
		{
			var c = new byte[a.Length + b.Length];
			Array.Copy(a, 0, c, 0, a.Length);
			Array.Copy(b, 0, c, a.Length, b.Length);
			return c;
		}

		byte[] GetMd5Sha1(RunContext context, IEnumerable<object> msgs)
		{
			var md5 = new MD5Digest();
			var sha1 = new Sha1Digest();

			foreach (var obj in msgs)
			{
				var bytes = ToBytes(obj, true);
				if (IsServer && bytes[0] == 0x10)
					bytes = KeyExchangeToBytes(obj, context);

				//Console.WriteLine("Msg: ");
				//foreach (var b in bytes)
				//	Console.Write("{0:X2} ", b);
				//Console.WriteLine();

				md5.BlockUpdate(bytes, 0, bytes.Length);
				sha1.BlockUpdate(bytes, 0, bytes.Length);
			}

			var combined = new byte[md5.GetDigestSize() + sha1.GetDigestSize()];
			md5.DoFinal(combined, 0);
			sha1.DoFinal(combined, md5.GetDigestSize());

			return combined;
		}

		byte[] GetSha256(RunContext context, IEnumerable<object> msgs)
		{
			var sha = new Sha256Digest();

			foreach (var obj in msgs)
			{
				var bytes = ToBytes(obj, true);
				if (IsServer && bytes[0] == 0x10)
					bytes = KeyExchangeToBytes(obj, context);

				//Console.WriteLine("Msg: ");
				//foreach (var b in bytes)
				//	Console.Write("{0:X2} ", b);
				//Console.WriteLine();

				sha.BlockUpdate(bytes, 0, bytes.Length);
			}

			var hash = new byte[sha.GetDigestSize()];
			sha.DoFinal(hash, 0);

			return hash;
		}

		static T GetState<T>(RunContext ctx, string key) where T : class
		{
			object obj;
			if (ctx.iterationStateStore.TryGetValue(key, out obj))
				return (T)obj;
			return default(T);
		}

		#endregion

		#region TlsContext

		internal class TlsContext : TlsClientContext
		{
			private readonly IRandomGenerator _mNonceRandom;

			public ProtocolVersion ProtocolVersion { get; set; }

			public TlsContext(ProtocolVersion protocolVersion, byte[] clientRandom, byte[] serverRandom)
			{
				SecureRandom = new SecureRandom();
				SecurityParameters = new SecurityParameters();

				if (protocolVersion.Equals(ProtocolVersion.TLSv12) ||
				    protocolVersion.Equals(ProtocolVersion.DTLSv12))

					Set(SecurityParameters, "prfAlgorithm", PrfAlgorithm.tls_prf_sha256);
				else
					Set(SecurityParameters, "prfAlgorithm", PrfAlgorithm.tls_prf_legacy);

				ProtocolVersion = protocolVersion;

				Set(SecurityParameters, "clientRandom", clientRandom);
				Set(SecurityParameters, "serverRandom", serverRandom);

				var d = TlsUtilities.CreateHash(HashAlgorithm.sha256);
				var seed = new byte[d.GetDigestSize()];
				SecureRandom.NextBytes(seed);

				_mNonceRandom = new DigestRandomGenerator(d);
				_mNonceRandom.AddSeedMaterial(Times.NanoTime());
				_mNonceRandom.AddSeedMaterial(seed);
			}

			//static void SetPrfAlg(SecurityParameters secParms, int alg)
			//{
			//	var typeSecParms = typeof (Org.BouncyCastle.Crypto.Tls.SecurityParameters);
			//	var fieldPrfAlg = typeSecParms.GetField("prfAlgorithm", BindingFlags.Instance| BindingFlags.NonPublic);
			//	if (fieldPrfAlg == null)
			//		throw new PeachException("Error, unable to set prfAlgorithm in BouncyCastle SecurityParameters.");

			//	fieldPrfAlg.SetValue(secParms, alg);
			//}

			private static void Set(object obj, string fieldName, object fieldValue)
			{
				var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (fi == null)
					throw new MissingFieldException(obj.GetType().FullName, fieldName);
				fi.SetValue(obj, fieldValue);
			}

			private static object Get(object obj, string fieldName)
			{
				var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (fi == null)
					throw new MissingFieldException(obj.GetType().FullName, fieldName);
				return fi.GetValue(obj);
			}

			public byte[] MasterSecret
			{
				set
				{
					Set(SecurityParameters, "masterSecret", value);
				}
			}

			public SecureRandom SecureRandom
			{
				get;
				private set;
			}

			public SecurityParameters SecurityParameters
			{
				get;
				private set;
			}

			public object UserObject
			{
				get;
				set;
			}

			public IRandomGenerator NonceRandomGenerator
			{
				get { return _mNonceRandom; }
			}

			public bool IsServer { get; set; }

			public ProtocolVersion ClientVersion
			{
				get { return ProtocolVersion; }
			}

			public ProtocolVersion ServerVersion
			{
				get { return ProtocolVersion; }
			}

			public TlsSession ResumableSession { get; set; }

			internal virtual void SetResumableSession(TlsSession session)
			{
				ResumableSession = session;
			}

			public byte[] ExportKeyingMaterial(string asciiLabel, byte[] context_value, int length)
			{
				if (context_value != null && !TlsUtilities.IsValidUint16(context_value.Length))
					throw new ArgumentException("must have length less than 2^16 (or be null)", "context_value");

				var sp = SecurityParameters;
				byte[] cr = sp.ClientRandom, sr = sp.ServerRandom;

				var seedLength = cr.Length + sr.Length;
				if (context_value != null)
				{
					seedLength += (2 + context_value.Length);
				}

				var seed = new byte[seedLength];
				var seedPos = 0;

				Array.Copy(cr, 0, seed, seedPos, cr.Length);
				seedPos += cr.Length;
				Array.Copy(sr, 0, seed, seedPos, sr.Length);
				seedPos += sr.Length;
				if (context_value != null)
				{
					TlsUtilities.WriteUint16(context_value.Length, seed, seedPos);
					seedPos += 2;
					Array.Copy(context_value, 0, seed, seedPos, context_value.Length);
					seedPos += context_value.Length;
				}

				if (seedPos != seedLength)
					throw new InvalidOperationException("error in calculation of seed for export");

				return TlsUtilities.PRF(this, sp.MasterSecret, asciiLabel, seed, length);
			}
		}

		#endregion

		public bool IsServer { get; set; }
		public TlsVersion TlsVersion { get; set; }

		public ProtocolVersion ProtocolVersion
		{
			get
			{
				switch (TlsVersion)
				{
					case TlsVersion.TLSv10:
						return ProtocolVersion.TLSv10;
					case TlsVersion.TLSv11:
						return ProtocolVersion.TLSv11;
					case TlsVersion.TLSv12:
						return ProtocolVersion.TLSv12;
					case TlsVersion.DTLSv10:
						return ProtocolVersion.DTLSv10;
					case TlsVersion.DTLSv12:
						return ProtocolVersion.DTLSv12;
					default:
						throw new PeachException("TlsFixup does not yet support selected TlsVersion.");
				}
			}
		}

		public TlsFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		internal static TlsContext CreateTlsContext(RunContext ctx, bool isServer, ProtocolVersion ProtocolVersion)
		{
			var pms = ToBytes(GetState<DataElement>(ctx, "PreMasterSecret"));
			if (pms == null)
				throw new SoftException("Error in Tls fixup, no handshake messages exists in the state store.");

			var clientRandom = ToBytes(GetState<DataElement>(ctx, "ClientRandom"));
			if (clientRandom == null)
				throw new SoftException("Error in Tls fixup, client random doesn't exist in the state store.");

			var serverRandom = ToBytes(GetState<DataElement>(ctx, "ServerRandom"));
			if (serverRandom == null)
				throw new SoftException("Error in Tls fixup, server random doesn't exist in the state store.");

			var tlsContext = new TlsContext(ProtocolVersion, clientRandom, serverRandom);
			var masterSecret = TlsUtilities.PRF(tlsContext, pms, "master secret",
				Concat(clientRandom, serverRandom), 48);
			
			tlsContext.MasterSecret = masterSecret;
			tlsContext.IsServer = isServer;

			Console.WriteLine("Pre-Master: ");
			foreach (var b in pms)
				Console.Write("{0:X2} ", b);
			Console.WriteLine();
			Console.WriteLine("Master Secret: ");
			foreach (var b in tlsContext.SecurityParameters.MasterSecret)
				Console.Write("{0:X2} ", b);
			Console.WriteLine();
			Console.WriteLine("Client Random: ");
			foreach (var b in tlsContext.SecurityParameters.ClientRandom)
				Console.Write("{0:X2} ", b);
			Console.WriteLine();
			Console.WriteLine("Server Random: ");
			foreach (var b in tlsContext.SecurityParameters.ServerRandom)
				Console.Write("{0:X2} ", b);
			Console.WriteLine();

			return tlsContext;
		}

		internal static TlsBlockCipher CreateBlockCipher(RunContext ctx, TlsContext tlsContext, bool store = true)
		{
			var tlsBlockCipher = new TlsBlockCipher(
				tlsContext,
				new CbcBlockCipher(new AesFastEngine()),
				new CbcBlockCipher(new AesFastEngine()),
				new Sha1Digest(),
				new Sha1Digest(),
				16); // AES-CBC block size

			if (store)
			{
				if (ctx.iterationStateStore.ContainsKey("TlsBlockCipher"))
					throw new SoftException("Error in Tls fixup, TlsBlockCipher object already exists in the state store.");

				ctx.iterationStateStore["TlsContext"] = tlsContext;
				ctx.iterationStateStore["TlsBlockCipher"] = tlsBlockCipher;
			}

			return tlsBlockCipher;
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			try
			{
				var tlsContext = CreateTlsContext(ctx, IsServer, ProtocolVersion);
				CreateBlockCipher(ctx, tlsContext);

				ctx.iterationStateStore["TlsProtocolVersion"] = TlsVersion.ToString();

				var msgs = GetState<IEnumerable<object>>(ctx, "HandshakeMessage");
				if (msgs == null)
					throw new SoftException("Error in Tls fixup, no handshake messages exists in the state store.");

				var verifyHash = TlsVersion == TlsVersion.TLSv12 ? GetSha256(ctx, msgs) : GetMd5Sha1(ctx, msgs);
				var verifyData = TlsUtilities.PRF(
					tlsContext, 
					tlsContext.SecurityParameters.MasterSecret, 
					IsServer ? "server finished" : "client finished", 
					verifyHash, 
					12);

				//Console.WriteLine("IsServer: "+IsServer);
				//Console.WriteLine("Verify Hash: ");
				//foreach (var b in verifyHash)
				//	Console.Write(string.Format("{0:X2} ", b));
				//Console.WriteLine();
				//Console.WriteLine("Verify Data: ");
				//foreach (var b in verifyData)
				//	Console.Write(string.Format("{0:X2} ", b));
				//Console.WriteLine();

				// Encryption of the verify_data is handled by the Tls transformer
				return new Variant(new BitStream(verifyData));
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}
	}
}
