using System.Linq;
using System.Reflection;
using Nancy.ModelBinding;
using Nancy.Serialization.JsonNet;
using Nancy.Testing;
using Peach.Pro.WebApi.Utility;

namespace Peach.Pro.Test.WebApi
{
	static class Extensions
	{
		public static TModel DeserializeJson<TModel>(this BrowserResponse resp)
		{
			var serializer = new CustomJsonSerializer();
			var deserializer = new JsonNetBodyDeserializer(serializer);
			var ctx = new BindingContext
			{
				DestinationType = typeof(TModel),
				ValidModelBindingMembers = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new BindingMemberInfo(p))
			};

			var ret = (TModel)deserializer.Deserialize(resp.ContentType, resp.Body.AsStream(), ctx);

			return ret;
		}
	}
}
