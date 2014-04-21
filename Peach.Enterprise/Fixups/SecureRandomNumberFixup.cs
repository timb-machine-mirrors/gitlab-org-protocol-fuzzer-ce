
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Jordyn Puryear (jordyn@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core;

namespace Peach.Enterprise.Fixups
{
	[Description("Secure Random Number Fixup.")]
	[Fixup("SecureRandomNumber", true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("Length", typeof(int), "Length in bytes to return")]
	[Serializable]
	public class SecureRandomNumberFixup : Fixup
	{
		static void Parse(string str, out DataElement val)
		{
			val = null;
		}

		void StateModel_Finished(StateModel model)
		{
			Core.Dom.Action.Starting -= Action_Starting;
			Core.Dom.StateModel.Finished -= StateModel_Finished;
		}

		void Action_Starting(Peach.Core.Dom.Action action)
		{
			var root = parent.getRoot() as DataModel;

			foreach (var item in action.outputData)
			{
				if (item.dataModel == root)
				{
					parent.Invalidate();
					Update();
				}
			}
		}

		public int Length { get; set; }
		protected DataElement _ref { get; set; }

		public SecureRandomNumberFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);

			if (Length <= 0)
				throw new PeachException("The length must be greater than 0.");
		}

		protected Variant Update()
		{
			if (elements["ref"].hasLength && Length > elements["ref"].length)
				throw new PeachException("Length is greater than 'ref' elements size.");

			var bs = new BitStream();
			var random = new byte[Length];
			RandomNumberGenerator rng = new RNGCryptoServiceProvider();

			rng.GetBytes(random);
			
			bs.Write(random, 0, Length);

			return new Variant(bs);
		}

		protected override Variant fixupImpl()
		{
			DataModel dm = parent.getRoot() as DataModel;

			if (dm == null || dm.action == null)
				return parent.DefaultValue;

			return Update();
		}

		[OnCloned]
		private void OnCloned(SecureRandomNumberFixup original, object context)
		{
			Core.Dom.Action.Starting += new ActionStartingEventHandler(Action_Starting);
			Core.Dom.StateModel.Finished += new StateModelFinishedEventHandler(StateModel_Finished);
		}
	}
}
