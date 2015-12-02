using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class MonitorMetadata
	{
		public static List<ParamDetail> Generate(List<string> calls)
		{
			return new MonitorMetadata(calls)._details;
		}

		enum ItemType { Group, Space, Monitor, Param }

		class TypedItem
		{
			public string Name { get; set; }
			public ItemType Type { get; set; }
			public List<TypedItem> Items { get; set; }
		}

		class MonitorInfo : INamed
		{
			[Obsolete]
			string INamed.name { get { return Key; } }
			string INamed.Name { get { return Key; } }

			public string Key { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public string OS { get; set; }
			public Type Type { get; set; }
			public bool Visited { get; set; }
		}

		private readonly List<string> _calls;
		private readonly List<ParamDetail> _details;

		private MonitorMetadata(List<string> calls)
		{
			var groupings = GetGroupings();
			var monitors = new NamedCollection<MonitorInfo>(GetAllMonitors());

			_calls = calls;
			_details = AsParameter(groupings, null);
		}

		private static List<TypedItem> GetGroupings()
		{
			var asm = Assembly.GetExecutingAssembly();

			using (var strm = asm.GetManifestResourceStream("Peach.Pro.Core.Resources.MonitorMetadata.json"))
			{
				if (strm == null)
					return new List<TypedItem>();

				var rdr = new JsonTextReader(new StreamReader(strm));
				var s = new JsonSerializer();

				return s.Deserialize<List<TypedItem>>(rdr);
			}
		}

		private static IEnumerable<MonitorInfo> GetAllMonitors()
		{
			return ClassLoader.GetAllByAttribute<MonitorAttribute>()
				.Where(FilterInternal)
				.Select(kv => new MonitorInfo
				{
					Key = kv.Key.Name,
					Name = KeyToName(kv.Key.Name),
					Description = GetDescription(kv.Value),
					OS = GetOS(kv.Key),
					Type = kv.Value,
					Visited = false
				});
		}

		private static bool FilterInternal(KeyValuePair<MonitorAttribute, Type> kv)
		{
#if !DEBUG
			return !kv.Key.Internal;
#else
			return true;
#endif
		}

		private static string KeyToName(string key)
		{
			// TODO: Change "CamelCase" to "Camel Case"
			return key;
		}

		private static string GetDescription(Type t)
		{
			return t
				.GetAttributes<System.ComponentModel.DescriptionAttribute>()
				.Select(a => a.Description)
				.FirstOrDefault() ?? "";
		}

		private static string GetOS(MonitorAttribute attr)
		{
			//var os = "";
			//if (attr.OS == Platform.OS.Unix)
			//{
			//	var ex = new NotSupportedException("Monitor {0} specifies unsupported OS {1}".Fmt(attr.Name, attr.OS));
			//	if (ValidationEventHandler != null)
			//		ValidationEventHandler(this, new ValidationEventArgs(ex, ""));
			//}
			//else if (attr.OS != Platform.OS.All)
			//{
			//	os = attr.OS.ToString();
			//}
			return "";
		}

		private List<ParamDetail> AsParameter(List<TypedItem> items, List<ParameterAttribute> parameters)
		{
			if (items == null)
				return null;

			var ret = new List<ParamDetail>();

			foreach (var item in items)
			{
				switch (item.Type)
				{
					case ItemType.Group:
						ret.Add(new ParamDetail
						{
							Name = item.Name,
							Type = ParameterType.Group,
							Items = AsParameter(item.Items, parameters)
						});

						break;
					case ItemType.Monitor:
						var monitor = ClassLoader.FindPluginByName<MonitorAttribute>(item.Name);
						if (monitor == null)
							throw new NotSupportedException();

						parameters = monitor.GetAttributes<ParameterAttribute>().ToList();

						ret.Add(new ParamDetail
						{
							Name = item.Name,
							Type = ParameterType.Monitor,
							Description = monitor.GetAttributes<System.ComponentModel.DescriptionAttribute>().Select(d => d.Description).FirstOrDefault() ?? "",
							Items = AsParameter(item.Items, parameters)
						});
						break;

					case ItemType.Space:
						ret.Add(new ParamDetail { Type = ParameterType.Space });
						break;

					case ItemType.Param:
						var param = parameters.First(p => p.name == item.Name);
						ret.Add(ParameterAttrToModel(param));
						break;
				}
			}

			return ret;
		}

		private ParamDetail ParameterAttrToModel(/*string monitorClass, */ParameterAttribute attr)
		{
			var p = new ParamDetail
			{
				Key = attr.name,
				Name = attr.name,
				DefaultValue = attr.required ? null : attr.defaultValue,
				Optional = !attr.required,
				Description = attr.description
			};

			var key = attr.type.Name;
			if (attr.type.IsGenericType)
			{
				key = attr.type.GetGenericArguments().First().Name;
			}
			else if (attr.type.IsEnum)
			{
				key = "Enum";
			}

			switch (key)
			{
				case "String":
				case "String[]":
				case "Int32[]":
					p.Type = ParameterType.String;
					break;
				case "UInt16":
					p.Type = ParameterType.Range;
					p.Max = UInt16.MaxValue;
					p.Min = UInt16.MinValue;
					break;
				case "UInt32":
					p.Type = ParameterType.Range;
					p.Max = UInt32.MaxValue;
					p.Min = UInt32.MinValue;
					break;
				case "Int32":
					p.Type = ParameterType.Range;
					p.Max = Int32.MaxValue;
					p.Min = Int32.MinValue;
					break;
				case "Boolean":
					p.Type = ParameterType.Bool;
					p.Options = new List<string> { "true", "false" };
					break;
				case "Enum":
					p.Type = ParameterType.Enum;
					p.Options = Enum.GetNames(attr.type).ToList();
					break;
				case "IPAddress":
					p.Type = ParameterType.Ipv4;
					break;
				default:
					p.Type = ParameterType.String;

					//var ex =
					//	new NotSupportedException("Monitor {0} has invalid parameter type {1}".Fmt(monitorClass, attr.type.FullName));
					//if (ValidationEventHandler != null)
					//	ValidationEventHandler(this, new ValidationEventArgs(ex, ""));

					break;
			}

			if (attr.name.Contains("OnCall"))
			{
				p.Type = ParameterType.Call;
				p.Options = _calls;
			}

			return p;
		}

	}
}
