using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Dom;
using Double = Peach.Core.Dom.Double;
using String = Peach.Core.Dom.String;

namespace Peach.Pro.Core.Analyzers
{
	[Analyzer("JsonDepricated", true)]
	[Serializable]
	public class JsonDepricatedAnalyzer : Analyzer
	{
		// ReSharper disable InconsistentNaming
		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = true;
		public new static readonly bool supportTopLevel = false;
		// ReSharper restore InconsistentNaming

		// This is required inorder for the pit parser to create the analyzer.
		// ReSharper disable once UnusedParameter.Local
		public JsonDepricatedAnalyzer(Dictionary<string, Variant> args)
		{
		}

		// This is required inorder to run the commandline analyzer
		public JsonDepricatedAnalyzer()
		{
		}

		public override void asCommandLine(Dictionary<string, string> args)
		{
			var extra = args.Values.ToList();

			if (extra.Count < 2)
			{
				Console.WriteLine("Syntax: <infile> <outfile>");
				return;
			}

			var inFile = extra[0];
			var outFile = extra[1];
			var data = new BitStream(File.ReadAllBytes(inFile));
			var model = new DataModel((Path.GetFileName(inFile) ?? inFile).Replace(".", "_"))
			{
				new String {stringType = StringType.utf8}
			};

			model[0].DefaultValue = new Variant(data);

			asDataElement(model[0], null);

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			using (var sout = new FileStream(outFile, FileMode.Create))
			using (var xml = XmlWriter.Create(sout, settings))
			{
				xml.WriteStartDocument();
				xml.WriteStartElement("Peach");

				model.WritePit(xml);

				xml.WriteEndElement();
				xml.WriteEndDocument();
			}
		}


		private static bool HandleNextJsonObject(ref DataElementContainer elem, JsonReader jsonReader, Stack<DataElementContainer> containers )
		{
			DataElement newElm = null;
			string propName = null;

			if (!jsonReader.Read())
				return false;

			if (jsonReader.TokenType == JsonToken.PropertyName)
			{
				propName = (string)jsonReader.Value;
				if (!jsonReader.Read())
					return false;
			}

			switch (jsonReader.TokenType)
			{
				case JsonToken.StartObject:
					newElm = MakeBlock(propName);
					containers.Push(elem);
					elem.Add(newElm);
					elem = (DataElementContainer)newElm;
					return true;
				case JsonToken.EndObject:
					elem = containers.Pop();
					return true;
				case JsonToken.StartArray:
					newElm = MakeSequence(propName);
					containers.Push(elem);
					elem.Add(newElm);
					elem = (DataElementContainer)newElm;
					return true;
				case JsonToken.EndArray:
					elem = containers.Pop();
					return true;
				case JsonToken.Integer:
					newElm = MakeNumber(propName, jsonReader.Value, jsonReader.ValueType);
					break;
				case JsonToken.String:
					newElm = MakeString(propName, (string)jsonReader.Value);
					break;
				case JsonToken.Float:
					newElm = MakeDouble(propName, (double)jsonReader.Value);
					break;
				case JsonToken.Boolean:
					newElm = MakeBool(propName, (bool)jsonReader.Value);
					break;
				case JsonToken.Bytes:
					throw new PeachException("Sorry, Bytes are not supported at this time.");
				case JsonToken.Null:
					newElm = MakeNull(propName);
					break;
				case JsonToken.Comment:
					break;
				default:
					throw new JsonException();
			}

			elem.Add(newElm);

			return true;
		}

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			var strElement = parent as String;
			if (strElement == null)
				throw new PeachException("Error, JsonAnalyzer analyzer only operates on String elements!");

			JsonTextReader jsonReader;
			try
			{
				var stream = (BitStream)strElement.Value;
				if (stream.Length == 0)
					return;

				var stringReader = new StringReader((string)strElement.InternalValue);
				jsonReader = new JsonTextReader(stringReader);
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, JsonAnalyzer failed to analyze element '" + parent.Name + "'.  " + ex.Message, ex);
			}

			var elem = (DataElementContainer)new Json(strElement.Name);

			jsonReader.Read();
			if (jsonReader.TokenType != JsonToken.StartObject)
				throw new PeachException("Error, Invalid Json.");

			var containers = new Stack<DataElementContainer>();
			containers.Push(elem);

			try
			{
				while (HandleNextJsonObject(ref elem, jsonReader, containers))
				{					
				}
			}
			catch (JsonException ex)
			{
				throw new PeachException("Error, JsonAnalyzer failed to analyze element '" + parent.Name + "'.  " + ex.Message, ex);
			}

			parent.parent[parent.Name] = elem;
		}

		private static Block MakeBlock(string name)
		{
			var blk = name == null ? new Block() : new Block(name);

			return blk;
		}

		private static Null MakeNull(string name)
		{
			var none = name == null ? new Null() : new Null(name);

			var hint = new Hint("Peach.TypeTransform", "false");
			none.Hints.Add(hint.Name, hint);

			return none;
		}

		private static Bool MakeBool(string name, bool value)
		{
			var b = name == null ? new Bool() : new Bool(name);

			if (value)
				b.DefaultValue = new Variant(1);

			var hint = new Hint("Peach.TypeTransform", "false");
			b.Hints.Add(hint.Name, hint);

			return b;
		}

		private static Sequence MakeSequence(string name)
		{
			var array = name == null ? new Sequence() : new Sequence(name);

			return array;
		}

		private static String MakeString(string name, string value)
		{
			var str = name == null ? new String() : new String(name);

			str.DefaultValue = new Variant(value);

			var hint = new Hint("Peach.TypeTransform", "false");
			str.Hints.Add(hint.Name, hint);

			return str;
		}

		private static Double MakeDouble(string name, double value)
		{
			var db = name == null ? new Double() : new Double(name);

			db.DefaultValue = new Variant(value);

			var hint = new Hint("Peach.TypeTransform", "false");
			db.Hints.Add(hint.Name, hint);

			return db;
		}

		private static Number MakeNumber(string name, object value, Type type)
		{
			var num = name == null ? new Number() : new Number(name);

			num.length = 64;

			switch (type.Name)
			{
				case "Int64":
					num.Signed = true;
					num.DefaultValue = new Variant((Int64)value);
					break;
				case "BigInteger":
					var bigInt = (BigInteger)value;

					if (bigInt > UInt64.MaxValue)
						throw new PeachException("Error, the given integer is greater than uint64 max value.");

					num.DefaultValue = new Variant((UInt64)bigInt);
					break;
			}
			var hint = new Hint("Peach.TypeTransform", "false");
			num.Hints.Add(hint.Name, hint);

			return num;
		}
	}
}
