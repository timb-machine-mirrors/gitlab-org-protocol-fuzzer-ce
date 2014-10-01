//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringUtf8ExtraBytes")]
	[Description("Encode string as UTF-8 with overlong encodings.")]
	public partial class StringUtf8ExtraBytes : Mutator
	{
		public StringUtf8ExtraBytes(DataElement obj)
			: base(obj)
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
			// Figure out the length required to encode the character (X)
			// Pick a random number between X and 6 inclusive (Y)
			// Encode character using Y bytes
			throw new NotImplementedException();
		}
	}
}
