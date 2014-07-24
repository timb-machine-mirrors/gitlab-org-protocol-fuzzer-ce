using System;

namespace Peach.Core.Runtime
{
	public class SyntaxException : Exception
	{
		public SyntaxException()
			: base("")
		{
		}

		public SyntaxException(string message)
			: base(message)
		{
		}
	}
}
