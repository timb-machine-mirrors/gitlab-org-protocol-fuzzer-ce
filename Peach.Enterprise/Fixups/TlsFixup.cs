using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;

using Peach.Core.Dom;
using Peach.Core.Fixups;
using Peach.Core.IO;
using Peach.Core;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;

namespace Peach.Enterprise.Fixups
{
	[Description("Tls")]
	[Fixup("Tls", true)]
	[Serializable]
	public class TlsFixup : VolatileFixup
	{
		#region Static Helpers For PRF()

		static TlsFixup()
		{
			var utils = typeof(TlsUtilities);
			miPrf = utils.GetMethod("PRF", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			if (miPrf == null)
				throw new MissingMethodException("Org.BouncyCastle.Crypto.Tls.TlsUtilities", "PRF");
		}

		static MethodInfo miPrf;

		static byte[] PRF(byte[] secret, string asciiLabel, byte[] seed, int size)
		{
			var args = new object[] { secret, asciiLabel, seed, size };
			var ret = miPrf.Invoke(null, args);
			return (byte[])ret;
		}

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
			byte[] c = new byte[a.Length + b.Length];
			System.Array.Copy(a, 0, c, 0, a.Length);
			System.Array.Copy(b, 0, c, a.Length, b.Length);
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
			public TlsContext(byte[] masterSecret, byte[] clientRandom, byte[] serverRandom)
			{
				SecureRandom = new SecureRandom();
				SecurityParameters = new SecurityParameters();

				Set(SecurityParameters, "masterSecret", masterSecret);
				Set(SecurityParameters, "clientRandom", clientRandom);
				Set(SecurityParameters, "serverRandom", serverRandom);
			}

			static void Set(object obj, string fieldName, object fieldValue)
			{
				var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (fi == null)
					throw new MissingFieldException(obj.GetType().FullName, fieldName);
				fi.SetValue(obj, fieldValue);
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
		}

		#endregion

		public TlsFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override Variant OnActionRun(RunContext ctx)
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

			var masterSecret = PRF(pms, "master secret", Concat(clientRandom, serverRandom), 48);

			ctx.iterationStateStore["TlsBlockCipher"] = new TlsBlockCipher(
				new TlsContext(masterSecret, clientRandom, serverRandom),
				new CbcBlockCipher(new AesFastEngine()),
				new CbcBlockCipher(new AesFastEngine()),
				new Sha1Digest(),
				new Sha1Digest(),
				16);

			/*
			* RFC 2246 7.4.9. The value handshake_messages includes all
			* handshake messages starting at client hello up to, but not
			* including, this finished message. [..] Note: [Also,] Hello Request
			* messages are omitted from handshake hashes.
			*/
			var md5sha1 = GetMd5Sha1(msgs);

			/*
			 *  RFC2246 7.4.9
			 */
			var verify_data = PRF(masterSecret, "client finished", md5sha1, 12);

			// Encryption of the verify_data is handled by the Tls transformer
			return new Variant(new BitStream(verify_data));
		}
	}
}
