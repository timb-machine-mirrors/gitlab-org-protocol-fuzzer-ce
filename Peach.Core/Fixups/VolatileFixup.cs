using System;
using System.Collections.Generic;
using Peach.Core.Dom;

namespace Peach.Core.Fixups
{
	/// <summary>
	/// A helper class for fixups that are volatile and need to
	/// be computed every time an action is run.
	/// Any fixup that requires per-iteration state stored in the
	/// RunContext should derive from this class and override OnActionRun.
	/// </summary>
	[Serializable]
	public abstract class VolatileFixup : Fixup
	{
		DataModel dataModel;
		Variant defaultValue;

		public VolatileFixup(DataElement parent, Dictionary<string, Variant> args, params string[] refs)
			: base(parent, args, refs)
		{
		}

		protected override Variant fixupImpl()
		{
			if (dataModel == null)
			{
				dataModel = parent.getRoot() as DataModel;
				dataModel.ActionRun += OnActionRunEvent;
			}

			return defaultValue ?? parent.DefaultValue;
		}

		/// <summary>
		/// Called before an output/call/setProperty action is run.
		/// Derivations can compute the result of the fixup
		/// from state stored on the run context.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected abstract Variant OnActionRun(RunContext ctx);

		void OnActionRunEvent(RunContext ctx)
		{
			defaultValue = OnActionRun(ctx);

			parent.Invalidate();
		}

		[OnCloned]
		void OnCloned(Fixup original, object context)
		{
			// The event is not serialized so we need to
			// resubscribe when we are cloned.
			if (dataModel != null)
				dataModel.ActionRun += OnActionRunEvent;
		}
	}
}
