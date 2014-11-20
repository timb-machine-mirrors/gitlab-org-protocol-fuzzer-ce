using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	public abstract class BasePythonFixup : Fixup
	{
		public BasePythonFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		[OnCloning]
		private bool OnCloning(object context)
		{
			return false;
		}
	}
}
