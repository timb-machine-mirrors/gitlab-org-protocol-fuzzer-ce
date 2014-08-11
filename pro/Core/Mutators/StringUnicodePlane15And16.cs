using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{


	/// <summary>
	/// Generate string with random unicode characters in them from plane 0 (0 - 0xffff).
	/// </summary>
	[Mutator("StringUnicodePlane15And16")]
	[Description("Produce a random string from the Unicode Plane 15 and 16 character set.")]
	public class StringUnicodePlane15And16 : Mutator
	{
		uint currentCount = 0;

		public StringUnicodePlane15And16(DataElement obj)
		{
			name = "StringUnicodePlane15And16";
		}

		public override uint mutation
		{
			get { return currentCount; }
			set { currentCount = value; }
		}

		public override int count
		{
			get { return 1000; }
		}
		public new static bool supportedDataElement(DataElement obj)
		{
			// Only attach to strings that support unicode characters
			if (obj is Core.Dom.String && obj.isMutable &&
				(obj as Core.Dom.String).stringType != StringType.ascii)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			int len = 1;
			var str = "";

			if (obj.DefaultValue != null)
				len = ((string)obj.DefaultValue).Length > 0 ? ((string)obj.DefaultValue).Length : 1;

			for (; len > 0; len--)
				str = str + char.ConvertFromUtf32(context.Random.Next(0xF0000, 0x10FFFF));

			obj.MutatedValue = new Variant(str);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
