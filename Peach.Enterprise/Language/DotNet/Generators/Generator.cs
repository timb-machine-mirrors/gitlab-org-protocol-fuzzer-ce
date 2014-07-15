
using System;
using System.Collections.Generic;
using System.Text;

namespace Peach.Enterprise.Language.DotNet.Generators
{
	public abstract class Generator : IGenerator
	{
		IGroup _group = null;
		//ITransformer _transformer = null;

		public Generator()
		{
		}

		public virtual object GetValue()
		{
			return GetRawValue();
		}

		public IGroup Group
		{
			get { return _group; }
			set { _group = value; }
		}

		public abstract object GetRawValue();
		public abstract void Next();
		public abstract void Reset();

	}
}
