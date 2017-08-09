using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.Core
{
	/// <summary>
	/// Json encoding to use
	/// </summary>
	public enum JsonType
	{
		/// <summary>
		/// Encode as standard json
		/// </summary>
		json,
		/// <summary>
		/// Encode as binary-json (bson)
		/// </summary>
		bson
	}

	class JsonPathConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return true;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var str = (string)value;
			serializer.Serialize(writer, str.Replace("\\", "/"));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var value = serializer.Deserialize<string>(reader);
			return value.Replace('/', Path.DirectorySeparatorChar);
		}
	}

	public static class JsonUtilities
	{
		public static JsonSerializerSettings GetSettings()
		{
			return new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				ContractResolver = new CamelCasePropertyNamesContractResolver
				{
					IgnoreSerializableAttribute = true
				},
				NullValueHandling = NullValueHandling.Ignore,
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				// NOTE: Don't ignore default values so integers and booleans get included in json

				Converters = new JsonConverter[]
				{
					new TimeSpanJsonConverter(),
					new StringEnumConverter { CamelCaseText = true }
				}
			};
		}

		public static JsonSerializer CreateSerializer()
		{
			return JsonSerializer.Create(GetSettings());
		}
	}

	/// <summary>
	/// Extend JRaw to allow binary data
	/// </summary>
	public class JTypeTransform : JRaw
	{
		public JTypeTransform(byte[] obj)
			: base(obj)
		{
		}

		public JTypeTransform(Stream obj)
			: base(obj)
		{
		}

		public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
		{
			var sb = Value as Stream;
			if (sb != null)
			{
				((CustomJsonWriter)writer).WriteTypeTransformValue(sb);
				return;
			}

			((CustomJsonWriter)writer).WriteTypeTransformValue((byte[])Value);
		}
	}

	/// <summary>
	/// Allow writing out Binary data
	/// </summary>
	public class CustomJsonWriter : JsonTextWriter
	{
		private readonly StreamWriter _writer;

		public CustomJsonWriter(StreamWriter writer)
			: base(writer)
		{
			_writer = writer;
		}

		public void WriteTypeTransformValue(Stream bs)
		{
			WriteRaw("");

			Flush();

			var origPosition = bs.Position;
			bs.Position = 0;
			bs.CopyTo(_writer.BaseStream);
			bs.Position = origPosition;

			WriteRawValue("");
		}

		public void WriteTypeTransformValue(byte[] buf)
		{
			WriteRaw("");

			Flush();

			_writer.BaseStream.Write(buf, 0, buf.Length);

			WriteRawValue("");
		}
	}
}
