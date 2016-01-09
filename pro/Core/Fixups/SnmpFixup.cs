using System;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using Peach.Core;
using Peach.Core.Dom;
using System.Collections.Generic;
using NLog;
using System.Security.Cryptography;
using Peach.Core.IO;
using System.IO;

namespace Peach.Pro.Core.Fixups
{
	[Fixup("SNMPv3", true)]
	[Parameter("Message", typeof(DataElement), "Reference to whole message")]
	[Parameter("EngineId", typeof(DataElement), "Reference to engine ID")]
	[Parameter("Password", typeof(string), "Authentication password")]
	[Description("Implements the HMAC-MD5-96 Authentication Protocol as specified in RFC 3414 section 6.")]
	[Serializable]
	public class SnmpFixup : Fixup
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public string Password { get; protected set; }
		public DataElement _Message { get; protected set; }
		public DataElement _EngineId { get; protected set; }

		public SnmpFixup(DataElement parent, Dictionary<string, Variant> args) 
			: base(parent, args, "Message", "EngineId")
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var msg = elements["Message"];
			var authFlag = msg.find("authFlag");
			if (authFlag == null || ((int)authFlag.DefaultValue != 1))
				return new Variant(new byte[0]);

			logger.Debug("authFlag is set");

			var engineId = elements["EngineId"];
			var algorithm = KeyedHashAlgorithm.Create("HMACMD5");
			algorithm.Key = PasswordToKey(Password, engineId.Value);

			var digest = ComputeHash(algorithm, msg, parent);

			var final = new BitStream();
			final.Write(digest, 0, 12);
			final.Seek(0, SeekOrigin.Begin);

			return new Variant(final);
		}

		byte[] ComputeHash(KeyedHashAlgorithm algorithm, DataElement msg, DataElement msgAuthenticationParameters)
		{
			try
			{
				SizeRelation.DisableRecursionCheck = true;

				msgAuthenticationParameters.DefaultValue = new Variant(new byte[12]);

				if (logger.IsTraceEnabled)
					logger.Trace("Message\n{0}", Utilities.HexDump(msg.Value));

				return algorithm.ComputeHash(msg.Value);
			}
			finally
			{
				SizeRelation.DisableRecursionCheck = false;
			}
		}

		internal static byte[] PasswordToKey(string password, Stream engineId)
		{
			var key1 = PasswordToKeyStep1(password);
			var key2 = PasswordToKeyStep2(key1, engineId);
			return key2;
		}

		internal static byte[] PasswordToKeyStep1(string password)
		{
			const int BufSize = 64 * 1024; // 64K
			const int Iterations = 1024 * 1024; // 1M

			var buf = new byte[BufSize];
			var passwordBytes = Encoding.ASCII.GetBytes(password);

			using (var algorithm = HashAlgorithm.Create("MD5"))
			using (var cs = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
			{
				var i = 0;
				for (var j = 0; j < Iterations; j += BufSize)
				{
					for (var k = 0; k < BufSize; k++)
						buf[k] = passwordBytes[i++ % passwordBytes.Length];

					cs.Write(buf, 0, BufSize);
				}

				cs.FlushFinalBlock();
				return algorithm.Hash;
			}
		}

		internal static byte[] PasswordToKeyStep2(byte[] key, Stream engineId)
		{
			using (var algorithm = HashAlgorithm.Create("MD5"))
			using (var cs = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
			{
				cs.Write(key, 0, key.Length);
				engineId.Seek(0, SeekOrigin.Begin);
				engineId.CopyTo(cs);
				engineId.Seek(0, SeekOrigin.Begin);
				cs.Write(key, 0, key.Length);

				cs.FlushFinalBlock();
				return algorithm.Hash;
			}
		}
	}
}
