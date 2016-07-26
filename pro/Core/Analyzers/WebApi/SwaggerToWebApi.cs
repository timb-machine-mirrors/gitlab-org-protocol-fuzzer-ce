﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Peach.Core.Dom;
using Peach.Pro.Core.Dom;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	/// <summary>
	/// Convert swagger api json to WebApiEndPoint
	/// </summary>
	public class SwaggerToWebApi
	{
		private SwaggerToWebApi()
		{
		}

		/// <summary>
		/// Convert a swagger json to WebApiEndPoint
		/// </summary>
		/// <param name="swaggerJson"></param>
		/// <returns></returns>
		public static WebApiEndPoint Convert(string swaggerJson)
		{
			var swagger = JObject.Parse(swaggerJson);
			return Convert(swagger);
		}

		/// <summary>
		/// Convert a swagger json to WebApiEndPoint
		/// </summary>
		/// <param name="swagger"></param>
		/// <returns></returns>
		public static WebApiEndPoint Convert(JObject swagger)
		{
			var endPoint = new WebApiEndPoint {Host = swagger["host"].Value<string>()};
			var models = ConvertDefinitions(swagger);
			var basePath = swagger["basePath"].Value<string>();

			foreach (var scheme in (JArray) swagger["schemes"])
			{
				WebApiScheme s;
				if (!WebApiScheme.TryParse(scheme.Value<string>().ToUpper(), out s))
					throw new ApplicationException("Error parsing swagger schme \"" + scheme.Value<string>() + "\".");

				endPoint.Schemes.Add(s);
			}

			foreach (KeyValuePair<string, JToken> path in ((JObject)swagger["paths"]))
				endPoint.Paths.Add(ConvertPath(path.Key, (JObject)path.Value, models, basePath));

			return endPoint;
		}

		static WebApiPath ConvertPath(string pathPropertyName, JObject path, List<DataElement> models, string basePath)
		{
			var apiPath = new WebApiPath();

			if (basePath != null)
			{
				if (pathPropertyName.StartsWith("/"))
					apiPath.Path = basePath + pathPropertyName;
				else
					apiPath.Path = basePath + "/" + pathPropertyName;
			}
			else
				apiPath.Path = pathPropertyName;

			foreach (KeyValuePair<string, JToken> opKeyValue in path)
			{
				var op = ConvertOperation(opKeyValue.Key, (JObject) opKeyValue.Value, models);
				op.Path = apiPath;

				apiPath.Operations.Add(op);
			}

			return apiPath;
		}

		static WebApiOperation ConvertOperation(string opKey, JObject op, List<DataElement> models)
		{
			JToken token;
			var apiOp = new WebApiOperation();

			WebApiOperationType opType;
			if (!WebApiOperationType.TryParse(opKey.ToUpper(), out opType))
				throw new ApplicationException("Error parsing swagger operation type \"" + opKey + "\".");

			apiOp.Type = opType;
			apiOp.OperationId = op["operationId"].Value<string>();

			if (op.TryGetValue("parameters", out token))
			{
				foreach (var param in (JArray) op["parameters"])
				{
					apiOp.Parameters.Add(ConvertParameter((JObject) param, models));
				}
			}

			return apiOp;
		}

		static WebApiParameter ConvertParameter(JObject param, List<DataElement> models)
		{
			JToken token;
			var apiParam = new WebApiParameter {Name = param["name"].Value<string>()};


			var sbIn = new StringBuilder(param["in"].Value<string>());
			sbIn[0] = char.ToUpperInvariant(sbIn[0]);

			WebApiParameterIn paramIn;
			if (!Enum.TryParse(sbIn.ToString(), out paramIn))
				throw new ApplicationException("Error parsing swagger param in \"" + param["in"].Value<string>() + "\".");

			apiParam.In = paramIn;

			if (param.TryGetValue("type", out token))
			{
				var sbType = new StringBuilder(param["type"].Value<string>());
				sbType[0] = char.ToUpperInvariant(sbType[0]);

				WebApiParameterType paramType;
				if (!Enum.TryParse(sbType.ToString(), out paramType))
					throw new ApplicationException("Error parsing swagger param type \"" + param["tpe"].Value<string>() + "\".");

				apiParam.Type = paramType;
			}

			if(param.TryGetValue("required", out token))
				apiParam.Required = token.Value<bool>();
			else
				apiParam.Required = false;

			if (apiParam.In == WebApiParameterIn.Body)
			{
					var swaggerRef = NameFromSwaggerRef(param["schema"]["$ref"].Value<string>());
					apiParam.DataElement = models.First(i => ((IJsonElement)i).PropertyName == swaggerRef).Clone();
			}
			else
			{
				apiParam.DataElement = new Peach.Core.Dom.String();
				apiParam.DataElement.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
			}

			return apiParam;
		}

		static List<DataElement> ConvertDefinitions(JObject json)
		{
			var models = new List<DataElement>();
			var definitions = (JObject) json["definitions"];

			foreach (var def in definitions)
			{
				var item = (JObject) definitions[def.Key];
				var name = def.Key.Replace(".", "_");
				DataElement elem = null;

				switch (item["type"].Value<string>())
				{
					case "object":
						elem = DefinitionObject(name, def.Key, item);
						break;
					case "array":
						elem = DefinitionArray(name, def.Key, item);
						break;
					case "string":
						elem = DefinitionString(name, def.Key, item);
						break;
					case "integer":
						elem = DefinitionInteger(name, def.Key, item);
						break;
					case "number":
						elem = DefinitionNumber(name, def.Key, item);
						break;
					case "boolean":
						elem = DefinitionBoolean(name, def.Key, item);
						break;
				}

				models.Add(elem);
			}

			return models;
		}

		static Stack<string> DefinitionToElement_Stack = new Stack<string>(); 

		static IJsonElement DefinitionToElement(string name, string propertyName, JObject item)
		{
			if (DefinitionToElement_Stack.Contains(name))
				return new JsonObject(name) { PropertyName = propertyName};

			DefinitionToElement_Stack.Push(name);

			try
			{

				JToken token;
				if (item.TryGetValue("type", out token))
				{
					switch (token.Value<string>())
					{
						case "object":
							return DefinitionObject(name, propertyName, item);
						case "string":
							return DefinitionString(name, propertyName, item);
						case "integer":
							return DefinitionInteger(name, propertyName, item);
						case "boolean":
							return DefinitionBoolean(name, propertyName, item);
						case "number":
							return DefinitionNumber(name, propertyName, item);
						case "array":
							return DefinitionArray(name, propertyName, item);
						default:
							throw new ApplicationException("Unable to convert definition, type unknown: \"" + item["type"].Value<string>() +
							                               "\"");
					}
				}

				if (item.TryGetValue("$ref", out token))
				{
					var swaggerRef = GetSwaggerRef(token.Value<string>(), (JObject) item.Root["definitions"]);
					return DefinitionToElement(name, propertyName, swaggerRef);
				}

				throw new ApplicationException("Unable to convert definition to element: " + name);
			}
			finally
			{
				DefinitionToElement_Stack.Pop();
			}
		}

		static JsonObject DefinitionObject(string name, string propertyName, JObject obj)
		{
			var jsonObject = new JsonObject(name);
			jsonObject.PropertyName = propertyName;

			JToken value;
			if (obj.TryGetValue("allOf", out value))
			{
				var allOfArray = (JArray) value;
				var allOf = (JObject) allOfArray[0];
				var allOfRef = allOf["$ref"];

				var swaggerRef = GetSwaggerRef(allOfRef.Value<string>(), (JObject)obj.Root["definitions"]);
				jsonObject = DefinitionObject(name, propertyName, swaggerRef);

				if (allOfArray.Count > 1)
					obj = (JObject)allOfArray[1];
				else
					return jsonObject;
			}

			foreach (var item in (JObject)obj["properties"])
			{
				jsonObject.Add((DataElement)DefinitionToElement(item.Key.Replace(".", "_"), item.Key, (JObject)item.Value));
			}

			return jsonObject;
		}

		static JsonString DefinitionString(string name, string propertyName, JObject obj)
		{
			var jsonString = new JsonString(name);
			jsonString.PropertyName = propertyName;

			return jsonString;
		}

		static JsonInteger DefinitionInteger(string name, string propertyName, JObject obj)
		{
			var jsonInteger = new JsonInteger(name);
			jsonInteger.PropertyName = propertyName;

			return jsonInteger;
		}

		static JsonBool DefinitionBoolean(string name, string propertyName, JObject obj)
		{
			var jsonBool = new JsonBool(name);
			jsonBool.PropertyName = propertyName;

			return jsonBool;
		}

		static JsonDouble DefinitionNumber(string name, string propertyName, JObject obj)
		{
			var jsonDouble = new JsonDouble(name);
			jsonDouble.PropertyName = propertyName;

			return jsonDouble;
		}

		static JsonArray DefinitionArray(string name, string propertyName, JObject obj)
		{
			var jsonArray = new JsonArray(name);
			jsonArray.PropertyName = propertyName;

			JToken token;
			var items = (JObject) obj["items"];
			if (items.TryGetValue("$ref", out token))
			{
				var swaggerRef = token.Value<string>();
				var item = GetSwaggerRef(swaggerRef, (JObject) obj.Root["definitions"]);
				var refName = NameFromSwaggerRef(swaggerRef);
				var elem = DefinitionToElement(refName, refName, item);

				jsonArray.Add((DataElement)elem);

				return jsonArray;
			}

			token = items["type"];
			switch (token.Value<string>())
			{
				case "integer":
					jsonArray.Add(new JsonInteger("Item"));
					break;
				case "number":
					jsonArray.Add(new JsonDouble("Item"));
					break;
				case "string":
					jsonArray.Add(new JsonString("Item"));
					break;
				case "boolean":
					jsonArray.Add(new JsonBool("Item"));
					break;
				default:
					throw new ApplicationException("Unknown type \""+token.Value<string>()+"\" for array");
			}

			return jsonArray;
		}

		private static string NameFromSwaggerRef(string swaggerRef)
		{
			if (!swaggerRef.StartsWith("#/definitions/"))
				throw new ApplicationException("Unknown $ref type: " + swaggerRef);

			return swaggerRef.Substring("#/definitions/".Length);
		}

		static JObject GetSwaggerRef(string swaggerRef, JObject definitions)
		{
			return (JObject)definitions[NameFromSwaggerRef(swaggerRef)];
		}
	}
}
