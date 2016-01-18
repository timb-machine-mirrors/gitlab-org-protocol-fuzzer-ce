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
using Peach.Pro.Core.Transformers.Crypto;
using Array = System.Array;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Fixups
{
	[Description("Sets the signature value that must be signed.")]
	[Fixup("TlsSignature", true, Internal = true)]
	[Parameter("TlsVersion", typeof(TlsVersion), "TLS Version", "DTLSv10")]
	[Serializable]
	public class TlsSignature : Peach.Core.Fixups.VolatileFixup
	{
		#region Static Helpers For PRF()

		static byte[] ToBytes(object obj)
		{
			if (obj == null)
				return null;


			var intern = (DataElement)obj;
			var v = intern.InternalValue;
			var bs = (BitwiseStream)v;
			var pos = bs.PositionBits;
			bs.Seek(0, System.IO.SeekOrigin.Begin);
			var ret = new byte[bs.Length];

			bs.Read(ret, 0, ret.Length);
			bs.Seek(pos, System.IO.SeekOrigin.Begin);

			return ret;
		}

		static byte[] Concat(byte[] a, byte[] b)
		{
			var c = new byte[a.Length + b.Length];
			Array.Copy(a, 0, c, 0, a.Length);
			Array.Copy(b, 0, c, a.Length, b.Length);
			return c;
		}

		static byte[] GetMd5Sha1(IEnumerable<object> msgs)
		{
			var md5 = new MD5Digest();
			var sha1 = new Sha1Digest();

			foreach (var obj in msgs)
			{
				var bytes = ToBytes(obj);

				md5.BlockUpdate(bytes, 0, bytes.Length);
				sha1.BlockUpdate(bytes, 0, bytes.Length);
			}

			var combined = new byte[md5.GetDigestSize() + sha1.GetDigestSize()];
			md5.DoFinal(combined, 0);
			sha1.DoFinal(combined, md5.GetDigestSize());

			return combined;
		}

		static byte[] GetSha256(IEnumerable<object> msgs)
		{
			var sha = new Sha256Digest();

			foreach (var obj in msgs)
			{
				var bytes = ToBytes(obj);

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

		class TlsContext : TlsClientContext
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

				//Set(SecurityParameters, "masterSecret", masterSecret);
				Set(SecurityParameters, "clientRandom", clientRandom);
				Set(SecurityParameters, "serverRandom", serverRandom);

				var d = TlsUtilities.CreateHash(HashAlgorithm.sha256);
				var seed = new byte[d.GetDigestSize()];
				SecureRandom.NextBytes(seed);

				this._mNonceRandom = new DigestRandomGenerator(d);
				_mNonceRandom.AddSeedMaterial(Times.NanoTime());
				_mNonceRandom.AddSeedMaterial(seed);
			}

			private static void Set(object obj, string fieldName, object fieldValue)
			{
				var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (fi == null)
					throw new MissingFieldException(obj.GetType().FullName, fieldName);
				fi.SetValue(obj, fieldValue);
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

			public bool IsServer
			{
				get { return false; }
			}

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

		public TlsSignature(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			try
			{
				if (ctx.iterationStateStore.ContainsKey("TlsBlockCipher"))
					throw new SoftException("Error in Tls fixup, TlsBlockCipher object already exists in the state store.");

				var msgs = GetState<IEnumerable<object>>(ctx, "HandshakeMessage");
				if (msgs == null)
					throw new SoftException("Error in Tls fixup, no handshake messages exists in the state store.");

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

				Console.Write("Pre-Master: ");
				foreach (var b in pms)
					Console.Write("{0:X2} ", b);
				Console.WriteLine();
				Console.Write("Client Random: ");
				foreach (var b in clientRandom)
					Console.Write("{0:X2} ", b);
				Console.WriteLine();
				Console.Write("Server Random: ");
				foreach (var b in serverRandom)
					Console.Write("{0:X2} ", b);
				Console.WriteLine();

				if (TlsVersion != TlsVersion.TLSv12)
				{
					var hash = new RsaSignature.CombinedHash();
					hash.Init(null);

					//hash.BlockUpdate(clientRandom, 0, clientRandom.Length);
					//hash.BlockUpdate(serverRandom, 0, serverRandom.Length);

					foreach (var msg in msgs)
					{
						var bytes = ToBytes(msg);

						if (TlsVersion == TlsVersion.DTLSv10 || TlsVersion == TlsVersion.DTLSv12)
						{
							try
							{
								Console.WriteLine("----- msg: {0}", ((DataElement)msg).fullName);

								var frag = (Dom.Frag)((DataElement)msg).parent;
								var rendering = (Dom.FragSequence) ((DataElementContainer) frag["Rendering"]);
								var message = (DataElementContainer)((DataElementContainer)rendering[0])["Message"];
								var bitstream = new BitStream();
								var fragBitWriter = new BitWriter(bitstream);

								var def = new Number() {DefaultValue = new Variant(0)};

								var type = message.find("HandshakeType") ?? def;
								var length = message.find("Length") ?? def;
								var seq = message.find("MessageSequence") ?? def;

								fragBitWriter.WriteByte((byte)((int)type.DefaultValue));
								fragBitWriter.WriteBits((ulong)length.DefaultValue, 24);
								fragBitWriter.WriteUInt16((ushort)((int)seq.DefaultValue));
								fragBitWriter.WriteBits(0, 24);  //epoc?
								fragBitWriter.WriteBits((ulong)length.DefaultValue, 24);

								var buf = new MemoryStream();
								bitstream.Position = 0;
								bitstream.CopyTo(buf);

								var bufArray = buf.ToArray();
								hash.BlockUpdate(bufArray, 0, bufArray.Length);

								Console.WriteLine("bitestream: {0} bufArray: {1}", bitstream.Length, bufArray.Length);

								foreach (var b in bufArray)
									Console.Write("{0:X2} ", b);
								foreach (var b in bytes)
									Console.Write("{0:X2} ", b);
								Console.WriteLine();
							}
							catch (Exception ex)
							{
								Console.Error.WriteLine("\nEXCEPTION: " + ex.Message);
								throw;
							}
						}

						hash.BlockUpdate(bytes, 0, bytes.Length);
					}

					var verifyHash = new byte[hash.GetDigestSize()];
					hash.DoFinal(verifyHash, 0);

					Console.Write("Verify Hash: ");
					foreach (var b in verifyHash)
						Console.Write("{0:X2} ", b);
					Console.WriteLine();

					return new Variant(new BitStream(verifyHash));
				}

				// For TLS 1.2, just stick in the data.

				var bitStream = new BitStream();
				var bitWriter = new BitWriter(bitStream);

				foreach (var msg in msgs)
				{
					bitWriter.WriteBytes(ToBytes(msg));
				}

				bitStream.Position = 0;
				return new Variant(bitStream);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}

		}
	}

	[Serializable]
	public enum TlsVersion
	{
		SSLv3,
		TLSv10,
		TLSv11,
		TLSv12,
		DTLSv10,
		DTLSv12
	}

}
