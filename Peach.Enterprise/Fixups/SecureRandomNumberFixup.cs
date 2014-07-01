using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Peach.Core.Dom;
using Peach.Core.Fixups;
using Peach.Core.IO;
using Peach.Core;

namespace Peach.Enterprise.Fixups
{
	[Description("Secure Random Number Fixup.")]
	[Fixup("SecureRandomNumber", true, IsTest = true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("Length", typeof(int), "Length in bytes to return")]
	[Serializable]
	public class SecureRandomNumberFixup : VolatileFixup
	{
		static void Parse(string str, out DataElement val)
		{
			val = null;
		}

		public int Length { get; set; }
		protected DataElement _ref { get; set; }

		public SecureRandomNumberFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);

			if (Length <= 0)
				throw new PeachException("The length must be greater than 0.");
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			if (elements["ref"].hasLength && Length > elements["ref"].length)
				throw new PeachException("Length is greater than 'ref' elements size.");

			var buf = new byte[Length];
			var rng = new RNGCryptoServiceProvider();

			rng.GetBytes(buf);
			
			return new Variant(new BitStream(buf));
		}
	}
}
