using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Peach.Core.IO;
using Peach.Pro.Core.Dom;
using Peach.Pro.Core.WebServices;
using Stream = System.IO.Stream;

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
				((IRawWriter)writer).WriteTypeTransformValue(sb);
				return;
			}

			((IRawWriter)writer).WriteTypeTransformValue((byte[])Value);
		}
	}

	public interface IRawWriter
	{
		void WriteTypeTransformValue(Stream bs);
		void WriteTypeTransformValue(byte[] buf);
	}

	public static class CustomElementWriter
	{
		public static BitwiseStream GenerateInternalValue(IJsonElement elem)
		{
			if (elem.Type == JsonType.json)
			{
				var bs = new BitStream();
				var sw = new StreamWriter(bs, Peach.Core.Encoding.UTF8.RawEncoding);
				var jsonWriter = new CustomJsonWriter(sw);

				elem.SerializeToJson(jsonWriter);

				jsonWriter.Flush();
				sw.Flush();

				bs.Seek(0, SeekOrigin.Begin);
				return bs;
			}

			// else bson
			var bbs = new BitStream();
			var bsonWriter = new CustomBsonWriter(bbs);

			elem.SerializeToJson(bsonWriter);
			bsonWriter.Flush();
			bbs.Flush();
			bbs.Seek(0, SeekOrigin.Begin);

			return bbs;
		}
	}

	/// <summary>
	/// Allow writing out Binary data
	/// </summary>
	public class CustomBsonWriter : BsonWriter, IRawWriter
	{
		private readonly Stream _writer;

		public CustomBsonWriter(Stream writer)
			: base(writer)
		{
			_writer = writer;
		}

		public void WriteTypeTransformValue(Stream bs)
		{
			Flush();

			var origPosition = bs.Position;
			bs.Position = 0;
			bs.CopyTo(_writer);
			bs.Position = origPosition;
		}

		public void WriteTypeTransformValue(byte[] buf)
		{
			Flush();

			_writer.Write(buf, 0, buf.Length);
		}
	}

	/// <summary>
	/// Allow writing out Binary data
	/// </summary>
	public class CustomJsonWriter : JsonTextWriter, IRawWriter
	{
		private readonly StreamWriter _writer;

		public CustomJsonWriter(StreamWriter writer)
			: base(writer)
		{
			_writer = writer;
		}

		public void WriteTypeTransformValue(Stream bs)
		{
			WriteRawValue("");

			Flush();

			var origPosition = bs.Position;
			bs.Position = 0;
			bs.CopyTo(_writer.BaseStream);
			bs.Position = origPosition;
		}

		public void WriteTypeTransformValue(byte[] buf)
		{
			WriteRawValue("");

			Flush();

			_writer.BaseStream.Write(buf, 0, buf.Length);

		}
	}
}
