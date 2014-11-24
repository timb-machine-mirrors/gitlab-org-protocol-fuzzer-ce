using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Analyzers
{
	[Serializable]
	public abstract class BasePythonAnalyzer : Analyzer
	{
		public BasePythonAnalyzer()
		{
		}

		public BasePythonAnalyzer(Dictionary<string, Variant> args)
		{
		}

		[OnCloning]
		private bool OnCloning(object context)
		{
			return false;
		}
	}
}
