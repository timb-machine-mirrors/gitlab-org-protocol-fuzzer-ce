using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("ChoiceMutator")]
	[Description("Changes which element is selected in a Choice statement.")]
	public class ChoiceMutator : Mutator
	{
		uint pos = 0;
		int _count = 0;

		public ChoiceMutator(DataElement obj)
		{
			pos = 0;
			name = "ChoiceMutator";

			Choice choice = obj as Choice;
			_count = choice.choiceElements.Count;
		}

		public override uint mutation
		{
			get { return pos; }
			set { pos = value; }
		}

		public override int count { get { return _count; } }

		public new static bool supportedDataElement(DataElement obj)
		{
			return (obj is Choice && obj.isMutable);
		}

		public override void sequentialMutation(DataElement obj)
		{
			Choice choice = obj as Choice;

			choice.SelectedElement = choice.choiceElements[0];
			obj.mutationFlags = MutateOverride.Default;
		}

		public override void randomMutation(DataElement obj)
		{
			Choice choice = obj as Choice;

			choice.SelectedElement = context.Random.Choice(choice.choiceElements.Values);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

// end
