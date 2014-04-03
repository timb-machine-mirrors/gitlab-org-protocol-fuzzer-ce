using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;

using NLog;
using Peach.Core.IO;

using Newtonsoft.Json;
using System.Reflection;

namespace Peach.Enterprise.Loggers
{
	[Logger("Visualizer", true)]
	public class VisualizerLogger :Peach.Core.Logger
	{

		public string json;

		public static string startUpPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "PeachView" + Path.DirectorySeparatorChar;
		private string _elementName = "";
		private string _mutatorName = "";
		private uint _totalIterations = 0;

		public VisualizerLogger(Dictionary<string, Variant> args)
		{
		}

		protected override void MutationStrategy_DataMutating(ActionData actionData, DataElement element, Mutator mutator)
		{
			_elementName = element.fullName;
			_mutatorName = mutator.name;
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (totalIterations != null)
				_totalIterations = (uint)totalIterations;

			// Remove any data models from last iteration
			dataModelsFromActions.Clear();
		}

		/// <summary>
		/// Collection of data models from Action_Finished event.
		/// </summary>
		List<Tuple<string, DataModel>> dataModelsFromActions = new List<Tuple<string, DataModel>>();

		protected override void Action_Finished(Peach.Core.Dom.Action action)
		{
			//base.Action_Finished(action);

			// TODO - Handle parameters
			var name = action.dataModel.name;
			foreach (var data in action.allData)
			{
				dataModelsFromActions.Add(new Tuple<string, DataModel>(data.dataModel.name, data.dataModel));
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			try
			{
				if (context.controlIteration || context.controlRecordingIteration)
					return;

				StringBuilder stringBuilder = new StringBuilder();
				StringWriter stringWriter = new StringWriter(stringBuilder);

				using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter))
				{
					jsonWriter.WriteStartArray();
					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName("IterationNumber");
					jsonWriter.WriteValue(Convert.ToString(currentIteration));
					jsonWriter.WritePropertyName("TotalIteration");
					jsonWriter.WriteValue(Convert.ToString(_totalIterations));
					jsonWriter.WritePropertyName("ElementName");
					jsonWriter.WriteValue(_elementName);
					jsonWriter.WritePropertyName("MutatorName");
					jsonWriter.WriteValue(_mutatorName);
					jsonWriter.WriteEndObject();

					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName("DataModels");
					jsonWriter.WriteStartArray();

					foreach (var item in dataModelsFromActions)
					{
						// StateModel.dataActions is now the serialized data model
						// in order to properly keep the data around when actions
						// have been re-entered.  This code will need to be updated
						// to hook ActionFinished event and serialize each action
						// to json when it runs.  This way our serialized json is correct
						// when actions have been re-entered.
						//throw new NotImplementedException("Needs fixing!");

						// EDDINGTON
						// Easy fix was to store datamodels during Action_Finished.

						jsonWriter.WriteStartObject();
						DataModelToJson(item.Item1, item.Item2, jsonWriter);
						jsonWriter.WriteEndObject();
					}

					jsonWriter.WriteEndArray();
					jsonWriter.WriteEndObject();
					jsonWriter.WriteEndArray();
				}

				json = stringBuilder.ToString();
			}
			catch (Exception e)
			{
				throw new PeachException("Failure writing Peach JSON Model for Visualizer", e);
			}
		}

		private void DataModelToJson(string name, DataElementContainer model, JsonWriter writer)
		{
			writer.WritePropertyName("name");
			writer.WriteValue(name);
			writer.WritePropertyName("children");
			writer.WriteStartArray();
			foreach (var item in model)
			{
				writer.WriteStartObject();

				if (item is Peach.Core.Dom.Array)
				{
					DataModelToJson(item.name, (DataElementContainer)item, writer);
				}

				if (item is Peach.Core.Dom.Block)
				{
					DataModelToJson(item.name, (DataElementContainer)item, writer);
				}

				if (item is Peach.Core.Dom.Flag)
				{
					DataModelToJson(item.name, (DataElementContainer)item, writer);
				}

				if (item is Peach.Core.Dom.Choice)
				{
					DataModelToJson(item.name, (DataElementContainer)item, writer);
				}

				if (item is Peach.Core.Dom.String)
				{
					writer.WritePropertyName("name");
					writer.WriteValue(item.name);
					writer.WritePropertyName("type");
					writer.WriteValue("String");

				}

				if (item is Number)
				{
					writer.WritePropertyName("name");
					writer.WriteValue(item.name);
					writer.WritePropertyName("type");
					writer.WriteValue("Number");

				}

				if (item is Blob)
				{
					writer.WritePropertyName("name");
					writer.WriteValue(item.name);
					writer.WritePropertyName("type");
					writer.WriteValue("Number");
				}

				if (item is XmlElement)
				{
					writer.WritePropertyName("name");
					writer.WriteValue(item.name);
					writer.WritePropertyName("type");
					writer.WriteValue("XmlElement");
				}

				writer.WriteEndObject();
			}

			writer.WriteEndArray();
		}

		private string StateModelToJson(StateModel model)
		{
			return "StateModel";
		}

		private string AgentToJson(Peach.Core.Dom.Agent agent)
		{
			return "Agent";
		}
	}
}
