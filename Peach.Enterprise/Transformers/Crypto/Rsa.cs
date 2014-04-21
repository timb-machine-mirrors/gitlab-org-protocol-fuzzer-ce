//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//  Jordyn Puryear (jordyn@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Linq;

namespace Peach.Core.Transformers.Crypto
{
	[Description("Rsa Transformer")]
	[Transformer("Rsa", true)]
	[Parameter("PublicKey", typeof(HexString), "Public key used to encrypt", "")]
	[Parameter("PrivateKey", typeof(HexString), "Private key used to decrypt", "")]
	[Parameter("UseStateBag", typeof(bool), "Use state bag to get public key", "false")]
	[Serializable]
	public class Rsa : Transformer
	{
		public HexString PublicKey { get; set; }
		public HexString PrivateKey { get; set; }
		public bool UseStateBag { get; set; }

		public Rsa(Dictionary<string, Variant> args)
			: base(args)
		{
			ParameterParser.Parse(this, args);
		}

		private static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var key = PublicKey.Value;
			byte[] Exponent = { 1, 0, 1 };
			var rsa = new RSACryptoServiceProvider(2048);
			RSAParameters RSAKeyInfo = new RSAParameters();

			//Set RSAKeyInfo to the public key values. 
			RSAKeyInfo.Modulus = key;
			RSAKeyInfo.Exponent = Exponent;
			rsa.ImportParameters(RSAKeyInfo);
			
			var newdata = new byte[data.Length];
			data.Read(newdata, 0, newdata.Length);
			return new BitStream(rsa.Encrypt(newdata, false));
		}

		protected override BitStream internalDecode(BitStream data)
		{
			var key = PrivateKey.Value;
			var newdata = new byte[data.Length];
			var rsa = new RSACryptoServiceProvider(2048);
			rsa.ImportCspBlob(key);
			
			return new BitStream(rsa.Encrypt(newdata, true));
		}

		//private void ComputeMasterSecret()
		//{
		//    byte[] label = Utils.BitConverter.StringToByteArray(Utils.BitConverter.ConvertStringToHex("master secret", Encoding.ASCII));

		//    byte[] seed = new byte[client_random.Length + server_random.Length];
		//    Buffer.BlockCopy(client_random, 0, seed, 0, client_random.Length);
		//    Buffer.BlockCopy(server_random, 0, seed, client_random.Length, server_random.Length);

		//    master_secret = PRF(pre_master_secret, label, seed, 48);
		//}
	}
}
