using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal static class HttpExtensions
	{
		private delegate void ResetReusesFunc(HttpListenerContext ctx);
		private static readonly ResetReusesFunc ResetReusesImpl;
		private static readonly PropertyInfo ConnProperty;
		private static readonly FieldInfo ReusesField;

		static HttpExtensions()
		{
			const BindingFlags attrs = BindingFlags.NonPublic | BindingFlags.Instance;

			ConnProperty = typeof(HttpListenerContext).GetProperty("Connection", attrs);
			if (ConnProperty == null)
			{
				ResetReusesImpl = NullReuses;
			}
			else
			{
				ReusesField = ConnProperty.PropertyType.GetField("reuses", attrs);
				ResetReusesImpl = ReflectReuses;
			}
		}

		static void NullReuses(HttpListenerContext ctx)
		{
		}

		static void ReflectReuses(HttpListenerContext ctx)
		{
			var conn = ConnProperty.GetValue(ctx, null);

			Debug.Assert(conn != null);

			ReusesField.SetValue(conn, 1);
		}

		public static void ResetReuses(this HttpListenerContext req)
		{
			ResetReusesImpl(req);
		}

		public static T FromJson<T>(this HttpListenerRequest req)
		{
			using (var sr = new StreamReader(req.InputStream, req.ContentEncoding))
			{
				using (var rdr = new JsonTextReader(sr))
				{
					var serializer = new JsonSerializer();
					var ret = serializer.Deserialize<T>(rdr);
					return ret;
				}
			}
		}

		public static T FromJson<T>(this HttpWebResponse resp)
		{
			var enc = System.Text.Encoding.GetEncoding(resp.CharacterSet ?? "utf-8");

			using (var strm = resp.GetResponseStream())
			{
				if (strm == null)
					return default(T);

				using (var sr = new StreamReader(strm, enc))
				{
					using (var rdr = new JsonTextReader(sr))
					{
						var serializer = new JsonSerializer();
						var ret = serializer.Deserialize<T>(rdr);
						return ret;
					}
				}
			}
		}

		public static object Consume(this HttpWebResponse resp)
		{
			using (var strm = resp.GetResponseStream())
			{
				if (strm != null)
				{
					strm.CopyTo(Stream.Null);
					strm.Close();
				}
			}

			return null;
		}

		public static void SendJson(this HttpWebRequest req, object obj)
		{
			if (req.Method == "GET")
			{
				Debug.Assert(obj == null);
				return;
			}

			if (obj == null)
			{
				req.ContentLength = 0;
				return;
			}

			var json = RouteResponse.ToJson(obj);
			var buf = System.Text.Encoding.UTF8.GetBytes(json);

			req.ContentType = "application/json;charset=utf-8";
			req.ContentLength = buf.Length;

			using (var strm = req.GetRequestStream())
				strm.Write(buf, 0, buf.Length);
		}
	}
}
