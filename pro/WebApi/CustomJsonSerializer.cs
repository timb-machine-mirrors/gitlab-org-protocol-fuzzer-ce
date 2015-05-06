using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Peach.Pro.WebApi
{
	// This class is public so it gets picked up by the Bootstrapper
	public sealed class CustomJsonSerializer : JsonSerializer
	{
		class CamelCaseStringEnumConverter : StringEnumConverter
		{
			public CamelCaseStringEnumConverter()
			{
				CamelCaseText = true;
			}
		}

		public CustomJsonSerializer()
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver();
			NullValueHandling = NullValueHandling.Ignore;
		}

		public override JsonConverterCollection Converters
		{
			get
			{
				var ret = base.Converters;
				ret.Insert(0, new CamelCaseStringEnumConverter());
				return ret;
			}
		}
	}
}
