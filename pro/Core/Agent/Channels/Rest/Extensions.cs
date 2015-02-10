using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.IO;

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
					return JsonDecode<T>(sr);
				}
			}
		}

		public static T JsonDecode<T>(this TextReader stream)
		{
			{
				using (var rdr = new JsonTextReader(stream))
				{
					var serializer = new JsonSerializer();
					var ret = serializer.Deserialize<T>(rdr);
					return ret;
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

		public static T ToModel<T>(this Variant v) where T : VariantMessage, new()
		{
			var ret = new T();

			var type = v.GetVariantType();

			switch (type)
			{
				case Variant.VariantType.BitStream:
					var bs = (BitwiseStream)v;
					var buf = new byte[bs.Length];
					var pos = bs.PositionBits;

					bs.SeekBits(0, SeekOrigin.Begin);
					bs.Read(buf, 0, buf.Length);
					bs.SeekBits(pos, SeekOrigin.Begin);

					ret.Type = VariantMessage.ValueType.Bytes;
					ret.Value = buf;
					break;
				case Variant.VariantType.Boolean:
					ret.Type = VariantMessage.ValueType.Bool;
					ret.Value = (bool)v;
					break;
				case Variant.VariantType.ByteString:
					ret.Type = VariantMessage.ValueType.Bytes;
					ret.Value = (byte[])v;
					break;
				case Variant.VariantType.Double:
					ret.Type = VariantMessage.ValueType.Double;
					ret.Value = (double)v;
					break;
				case Variant.VariantType.Int:
					ret.Type = VariantMessage.ValueType.Integer;
					ret.Value = (int)v;
					break;
				case Variant.VariantType.Long:
					ret.Type = VariantMessage.ValueType.Integer;
					ret.Value = (long)v;
					break;
				case Variant.VariantType.String:
					ret.Type = VariantMessage.ValueType.String;
					ret.Value = (string)v;
					break;
				case Variant.VariantType.ULong:
					ret.Type = VariantMessage.ValueType.Integer;
					ret.Value = (ulong)v;
					break;
				default:
					throw new NotSupportedException("Unable to convert variant type '{0}' to JSON.".Fmt(type));
			}

			return ret;
		}

		public static Variant ToVariant(this VariantMessage msg)
		{
			switch (msg.Type)
			{
				//case VariantMessage.ValueType.Bool:
				//	return new Variant(Convert.ToBoolean(msg.Value));
				case VariantMessage.ValueType.Bytes:
					return new Variant(Convert.FromBase64String((string)msg.Value));
				case VariantMessage.ValueType.Double:
					return new Variant(Convert.ToDouble(msg.Value));
				case VariantMessage.ValueType.String:
					return new Variant(Convert.ToString(msg.Value));
				case VariantMessage.ValueType.Integer:
					if (msg.Value is BigInteger)
					{
						var bi = (BigInteger)msg.Value;
						if (bi < long.MinValue)
							throw new NotSupportedException("Unable to convert JSON integer value to a variant, the value is less than the minimum value of a long.");
						if (bi > ulong.MaxValue)
							throw new NotSupportedException("Unable to convert JSON integer value to a variant, the value is greater than the maximum value of an unsigned long.");
						return new Variant((ulong)bi);
					}

					var asLong = (long)msg.Value;

					if (asLong < int.MinValue || asLong > int.MaxValue)
						return new Variant(asLong);

					return new Variant((int)asLong);
				default:
					throw new NotSupportedException("Unable to convert JSON value type '{0}' to a variant.".Fmt(msg.Type));
			}
		}
	}
}
