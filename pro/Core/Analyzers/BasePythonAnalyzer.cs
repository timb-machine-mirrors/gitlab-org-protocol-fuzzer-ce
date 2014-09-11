using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;

namespace Peach.Enterprise.Analyzers
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
