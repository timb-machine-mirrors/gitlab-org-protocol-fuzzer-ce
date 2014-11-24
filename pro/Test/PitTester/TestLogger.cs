using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;

using Action = Peach.Core.Dom.Action;

namespace PitTester
{
	public class TestLogger : Logger
	{
		TestData.Test testData;
		List<string> xpathIgnore;
		int index;
		Action action;
		bool verify;
		List<Tuple<Action, DataElement>> ignores;

		public string ActionName { get; private set; }

		public TestLogger(TestData.Test testData, IEnumerable<string> xpathIgnore)
		{
			this.testData = testData;
			this.xpathIgnore = new List<string>(xpathIgnore);
		}

		public T Verify<T>(string publisherName) where T : TestData.Action
		{
			if (!verify)
				return null;

			//Ignore implicit closes that are called at end of state model
			if (action == null)
				return null;

			try
			{
				if (index >= testData.Actions.Count)
					throw new PeachException("Missing record in test data");

				var d = testData.Actions[index++];


				if (typeof(T) != d.GetType())
				{
					var msg = "Encountered unexpected action type.\nAction Name: {0}\nExpected: {1}\nGot: {2}".Fmt(ActionName, typeof(T).Name, d.GetType().Name);
					throw new PeachException(msg);
				}

				if (d.PublisherName != publisherName)
					throw new PeachException("Publisher names didn't match. Expected {0} but got {1}".Fmt(publisherName, d.PublisherName));

				if (d.ActionName != ActionName)
					throw new PeachException("Action names didn't match.\n\tExpected: {0}\n\tBut got: {1}\n".Fmt(ActionName, d.ActionName));

				return (T)d;
			}
			catch
			{
				// don't perform anymore verification
				verify = false;

				throw;
			}
		}

		public IEnumerable<Tuple<Action, DataElement>> Ignores
		{
			get
			{
				return ignores;
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			ignores = new List<Tuple<Action, DataElement>>();

			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(context.dom);

			foreach (var item in xpathIgnore)
			{
				var iter = navi.Select(item, resolver);
				if (!iter.MoveNext())
					throw new PeachException("Error, ignore xpath returned no values. [" + item + "]");

				do
				{
					var valueElement = ((PeachXPathNavigator)iter.Current).currentNode as DataElement;
					if (valueElement == null)
						throw new PeachException("Error, ignore xpath did not return a Data Element. [" + item + "]");

					// Only track elements that are attached to actions, not free form data models
					var dm = valueElement.root as DataModel;
					if (dm.actionData != null)
						ignores.Add(new Tuple<Action, DataElement>(dm.actionData.action, valueElement));
				}
				while (iter.MoveNext());
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			ignores = null;
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			verify = true;
			index = 0;

			PitTester.OnIterationStarting(context, currentIteration, totalIterations);
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			// TODO: Assert we made it all the way through TestData.Actions
			//if (verify && index != testData.Actions.Count)
			//	throw new PeachException("Didn't make it all the way through the expected data");

			// Don't perform anymore verification
			// This prevents publisher stopping that happens
			// after the iteration from causing problems
			verify = false;
		}

		protected override void ActionStarting(RunContext context, Action action)
		{
			this.action = action;

			ActionName = string.Join(".", new[] { action.parent.parent.name, action.parent.name, action.name });
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			// If the action errored, don't do anymore verification
			if (action.error)
				verify = false;

			this.action = null;

			ActionName = null;
		}
	}
}
