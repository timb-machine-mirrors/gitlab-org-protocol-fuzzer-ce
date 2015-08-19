using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Fixups
{
	[Description("UNIX time (seconds since the midnight starting Jan 1, 1970)")]
	[Fixup("UnixTime", true)]
	[Parameter("Gmt", typeof(bool), "Is time in GMT?", "true")]
	[Serializable]
	public class UnixTime : Fixup
	{
		public bool Gmt { get; protected set; }

		public UnixTime(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, Gmt ? DateTimeKind.Utc : DateTimeKind.Local));
			var unixTime = (int)span.TotalSeconds;

			return new Variant(unixTime);
		}
	}
}
