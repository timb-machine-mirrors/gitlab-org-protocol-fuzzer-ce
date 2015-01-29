﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Core.Test
{
	public class DataModelCollector : Watcher
	{
		public static Dom.Dom ParsePit(string xml)
		{
			return new PitParser().asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
		}

		protected void RunEngine(string xml)
		{
			RunEngine(ParsePit(xml));
		}

		protected void RunEngine(Dom.Dom dom)
		{
			var e = new Engine(this);

			var cfg = new RunConfiguration();

			e.startFuzzing(dom, cfg);
		}

		protected List<Variant> mutations = null;
		protected List<BitwiseStream> values = null;
		protected List<Dom.DataModel> dataModels = null;
		protected List<Dom.DataModel> mutatedDataModels = null;
		protected List<Dom.Action> actions = null;
		protected List<string> strategies = null;
		protected List<string> iterStrategies = null;
		protected List<string> allStrategies = null;
		protected bool cloneActions = false;

		[SetUp]
		public void SetUp()
		{
			cloneActions = false;
			ResetContainers();
		}

		[TearDown]
		public void TearDown()
		{
		}

		protected void ResetContainers()
		{
			values = new List<BitwiseStream>();
			mutations = new List<Variant>();
			actions = new List<Dom.Action>();
			dataModels = new List<Dom.DataModel>();
			mutatedDataModels = new List<Dom.DataModel>();
			strategies = new List<string>();
			allStrategies = new List<string>();
			iterStrategies = new List<string>();
		}

		protected override void ActionFinished(RunContext context, Dom.Action action)
		{
			if (!action.allData.Any())
				return;

			var dom = action.parent.parent.parent as Dom.Dom;

			foreach (var item in action.allData)
			{
				SaveDataModel(dom, item.dataModel);
			}

			if (cloneActions)
				actions.Add(ObjectCopier.Clone(action));
			else
				actions.Add(action);
		}

		void SaveDataModel(Dom.Dom dom, Dom.DataModel model)
		{
			// Collect mutated values only after the first run
			if (!dom.context.controlIteration)
			{
				mutations.Add(model.Count > 0 ? model[0].InternalValue : null);
				mutatedDataModels.Add(model);
			}

			// Collect transformed values, actions and dataModels always
			values.Add(model.Count > 0 ? model[0].Value : null);
			dataModels.Add(model);
		}

		protected override void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
			int len = strategies.Count;
			string item = mutator.Name + " | " + element.fullName;
			allStrategies.Add(item);
			if (len == 0 || strategies[len - 1] != item)
				strategies.Add(item);

			while (iterStrategies.Count < (actions.Count + 1))
				iterStrategies.Add("");

			if (iterStrategies[actions.Count].Length > 0)
				iterStrategies[actions.Count] += " ; ";

			iterStrategies[actions.Count] += item;
		}
	}
}
