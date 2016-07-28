using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using Peach.Pro.Core.Publishers;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	/// <summary>
	/// Convert a WebApiCollection to Peach Dom
	/// </summary>
	public class WebApiToDom
	{
		public static Test Convert(Peach.Core.Dom.Dom dom, WebApiCollection apiCollection)
		{
			var test = new Test
			{
				Name = "Defaults",
				maxOutputSize = 500000000
			};

			dom.tests.Add(test);
			dom.stateModels.Add(new StateModel() { Name = "Default" });
			test.stateModel = dom.stateModels[0];

			var publisher = new RestPublisher(new Dictionary<string, Variant>())
			{
				Name = Guid.NewGuid().ToString()
			};

			test.publishers.Add(publisher);

			AddGenericDataModels(dom);

			// Convert all endpoints
			foreach (var endPoint in apiCollection.EndPoints)
			{
				Convert(dom, test.stateModel, endPoint);
			}

			test.stateModel.initialState = test.stateModel.states[0];
			test.stateModel.initialStateName = test.stateModel.states[0].Name;


			return test;
		}

		public static void AddGenericDataModels(Peach.Core.Dom.Dom dom)
		{
			// WebApiResult
			var result = new DataModel("WebApiResult");
			var choice = new Choice("ResultOrEmpty");
			var str = new Peach.Core.Dom.String("Result");
			str.analyzer = new JsonAnalyzer();

			choice.Add(str);
			choice.Add(new Block("Empty"));

			result.Add(choice);
			dom.dataModels.Add(result);

			// WebApiString
			var strElement = new DataModel("WebApiString");
			var data = new Peach.Core.Dom.String("value");
			data.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));

			strElement.Add(data);
			dom.dataModels.Add(strElement);
		}

		public static void Convert(Peach.Core.Dom.Dom dom, StateModel stateModel, WebApiEndPoint endPoint)
		{
			var state = new State() {Name = Guid.NewGuid().ToString()};
			stateModel.states.Add(state);

			foreach (var path in endPoint.Paths)
			{
				Convert(dom, state, path);
			}
		}

		public static void Convert(Peach.Core.Dom.Dom dom, State state, WebApiPath apiPath)
		{
			foreach (var op in apiPath.Operations)
			{
				var call = Convert(dom, state, op);
				state.actions.Add(call);
			}
		}

		public static Call Convert(Peach.Core.Dom.Dom dom, State state, WebApiOperation operation)
		{
			var call = new Call
			{
				Name = operation.Name,
				method = string.Format("{0} {1}", operation.Type, BuildPathAndQuery(operation))
			};

			for (var cnt = 0; state.actions.ContainsKey(call.Name); cnt++ )
				call.Name = operation.Name + "_" + cnt;

			ParametersToPeach(dom, call, operation);

			if (operation.Body != null)
			{
				var body = new ActionParameter("body");
				body.dataModel = new DataModel(call.Name);
				body.dataModel.Add(operation.Body);

				call.parameters.Add(body);
				dom.dataModels.Add(body.dataModel);
			}

			switch (operation.Type)
			{
				case WebApiOperationType.GET:
				case WebApiOperationType.POST:
					call.result = new ActionResult();
					call.result.dataModel = dom.dataModels["WebApiResult"];
					break;
			}

			operation.Call = call;

			return call;
		}

		public static string BuildPathAndQuery(WebApiOperation operation)
		{
			var url = operation.Path.Path;

			var cnt = 0;
			foreach (var param in operation.Parameters)
			{
				param.PathFormatId = cnt;
				cnt++;
			}

			foreach (var part in operation.Parameters.Where(item => item.In == WebApiParameterIn.Path).OrderBy(item => item.PathFormatId))
			{
				url = url.Replace("{"+part.Name+"}", "{" + part.PathFormatId + "}");
			}

			var query = "?";

			foreach (var part in operation.Parameters.Where(item => item.In == WebApiParameterIn.Query).OrderBy(item => item.PathFormatId))
			{
				if (query.Last() != '?' && query.Last() != '&')
					query += '&';

				query += string.Format("{0}={{{1}}}", part.Name, part.PathFormatId);
			}

			if (query.Length > 1)
				url += query;

			return url;
		}

		public static void ParametersToPeach(Peach.Core.Dom.Dom dom, Call call, WebApiOperation operation)
		{
			foreach (var part in operation.Parameters.Where(item => item.In == WebApiParameterIn.Path).OrderBy(item => item.PathFormatId))
			{
				var id = Guid.NewGuid().ToString();

				var param = new ActionParameter(id) {dataModel = dom.dataModels["WebApiString"]};
				part.DataElement = param.dataModel;

				var dataSet = new DataSet() {Name = Guid.NewGuid().ToString()};
				var data = new DataField(dataSet);
				data.Fields.Add(new DataField.Field() { Name = "Value" });
				dataSet.Add(data);
				param.dataSets.Add(dataSet);

				call.parameters.Add(param);
			}

			foreach (var part in operation.Parameters.Where(item => item.In == WebApiParameterIn.Query).OrderBy(item => item.PathFormatId))
			{
				var id = Guid.NewGuid().ToString();

				var param = new ActionParameter(id);
				param.dataModel = dom.dataModels["WebApiString"];
				part.DataElement = param.dataModel;

				var dataSet = new DataSet() { Name = Guid.NewGuid().ToString() };
				var data = new DataField(dataSet);
				data.Fields.Add(new DataField.Field() { Name = "Value" });
				dataSet.Add(data);
				param.dataSets.Add(dataSet);

				call.parameters.Add(param);
			}

			var body = operation.Parameters.FirstOrDefault(item => item.In == WebApiParameterIn.Body);
			if (body != null)
			{
				var model = new DataModel(call.Name + "_Body");
				model.Add(body.DataElement);
				dom.dataModels.Add(model);

				var id = Guid.NewGuid().ToString();

				var param = new ActionParameter(id);
				param.dataModel = model;

				call.parameters.Add(param);
			}
		}
	}
}
