using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Peach.Core.Dom;

namespace Peach.Core.Fixups
{
	[Description("Radius Accounting Authenticator")]
	[Fixup("RadiusAcctFixup", true)]
	[Fixup("radius.RadiusAcctFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("secret", typeof(string), "Shared secret between server and user")]
    [Serializable]
    public class RadiusAcctFixup : Fixup
    {

        public RadiusAcctFixup(DataElement parent, Dictionary<string, Variant> args) : base(parent, args, "ref")
        {

			if (!args.ContainsKey("secret"))
                throw new PeachException("Error, RadiusAcctFixup requires a 'secret' argument!");
        }

        protected override Variant fixupImpl()
        {
	        
			byte[] secret = System.Text.Encoding.ASCII.GetBytes((string)args["secret"]);
			var elem = elements["ref"];
			byte[] data = elem.Value.Value;
            int hashedSize = data.Length + secret.Length;
			byte[] temp = new byte[hashedSize];
            data.CopyTo(temp, 0);
			secret.CopyTo(temp, data.Length );
			MD5 md5Tool = MD5.Create();
			byte[] m = md5Tool.ComputeHash(temp);
			return new Variant(m);
        }
    }
}

// end
