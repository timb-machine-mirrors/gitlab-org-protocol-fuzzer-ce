//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringUtf8Invalid")]
	[Description("Encode string as invalid UTF-8.")]
	public partial class StringUtf8Invalid : Mutator
	{
		public StringUtf8Invalid(DataElement obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			//var asStr = obj as Dom.String;

			//if (asStr == null || !asStr.isMutable)
			//	return false;

			//// Attach to ascii and utf8, since most ascii parsers are utf8
			//if (asStr.stringType == StringType.ascii || asStr.stringType == StringType.utf8)
			//	return true;

			return false;
		}

		public override int count
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			throw new NotImplementedException();
		}

		public override void randomMutation(DataElement obj)
		{
			// 1) Encode string to valid utf8
			// 2) Flip bits that control bits in the the underlying byte sequence
			throw new NotImplementedException();
		}
	}
}
