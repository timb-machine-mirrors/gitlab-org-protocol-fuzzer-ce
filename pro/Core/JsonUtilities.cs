using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
}
