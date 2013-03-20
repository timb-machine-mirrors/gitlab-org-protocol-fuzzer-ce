using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using Peach.Core.Dom;

namespace Peach.Core.Fixups
{
	[Description("Radius Password Encrytpion")]
	[Fixup("RadiusFixup", true)]
	[Fixup("radius.RadiusFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("password", typeof(string), "User's password")]
	[Parameter("secret", typeof(string), "Shared secret between server and user")]
    [Serializable]
    public class RadiusFixup : Fixup
    {

        public RadiusFixup(DataElement parent, Dictionary<string, Variant> args) : base(parent, args, "ref")
        {
			if (!args.ContainsKey("password"))
                throw new PeachException("Error, RadiusFixup requires a 'password' argument!");
			if (!args.ContainsKey("secret"))
                throw new PeachException("Error, RadiusFixup requires a 'secret' argument!");
        }

        protected override Variant fixupImpl()
        {
			byte[] password = System.Text.Encoding.ASCII.GetBytes((string)args["password"]);
			byte[] secret = System.Text.Encoding.ASCII.GetBytes((string)args["secret"]);
			
			var elem = elements["ref"];
			byte[] data = elem.Value.Value;
			
			int cryptSize;
			if(password.Length % 16 == 0) {
				cryptSize = password.Length;
			} else {
				cryptSize = (password.Length / 16 + 1) * 16;
			}
			byte[] crypt = new byte[cryptSize];
            byte[] paddedPassword = new byte[cryptSize];
            password.CopyTo(paddedPassword, 0); // the remainder will be 0's by default

            MD5 md5Tool = MD5.Create();
            for (int i = 0; i < paddedPassword.Length; i += 16)
            {
                byte[] cypher = new byte[16];
                byte[] secretConcatWithRA = new byte[secret.Length + data.Length];

                secret.CopyTo(secretConcatWithRA, 0);
                data.CopyTo(secretConcatWithRA, secret.Length);
                byte[] raHashed = md5Tool.ComputeHash(secretConcatWithRA);
                System.Diagnostics.Debug.Assert(raHashed.Length == 16);
                
                //Password XOR MD5(secret + request auth)
                for (int j = 0; j < 16; j++)
                {
                    byte pXorHash = (byte)(paddedPassword[j + i] ^ raHashed[j]);
                    cypher[j] = (byte)pXorHash;
                    crypt[j+i] = (byte)pXorHash;
                }
                data = cypher;
            }
			return new Variant(crypt);
        }
    }
}

// end
