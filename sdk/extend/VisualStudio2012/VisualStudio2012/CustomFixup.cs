
// Copyright (c) Deja vu Security

// Example stub code for a custom Peach Fixup

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Peach.Core;
using Peach.Core.Dom;

namespace VisualStudio2012
{
	// Use the [Fixup] attribute to indicate the class can be used as a Fixup and
	// provide the name used in the XML. (<Fixup class="Custom" />)
	[Fixup("Custom", true)]
	// Define zero or more parameters with name, type, descriptiom, and optional default value.
	// parameters w/o a default value will be required.
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	// Optional description of fixup to display via --showenv
	[Description("Example custom fixup from Peach SDK.")]
	[Serializable]
	public class CustomFixup : Fixup
	{
		static void Parse(string str, out DataElement val)
		{
			val = null;
		}

		protected DataElement _ref { get; set; }

		public CustomFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			// Automatically resolve parameters
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];
			var data = elem.Value;

			// Make sure we are at the start of our data stream
			data.Seek(0, System.IO.SeekOrigin.Begin);

			// TODO - Implement fixup logic here

			return new Variant(42 /* place output here */);
		}
	}
}

// end
