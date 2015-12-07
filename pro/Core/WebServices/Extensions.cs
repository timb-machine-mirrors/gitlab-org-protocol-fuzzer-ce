using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	internal static class Extensions
	{
		private class OrderedContractResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
			}
		}

		internal static string ToJson(this List<ParamDetail> details)
		{
			var json = JsonConvert.SerializeObject(details, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ContractResolver = new OrderedContractResolver()
			});

			return json;
		}

		public static List<Models.Agent> ToWeb(this List<PeachElement.AgentElement> agents)
		{
			return agents.Select(a =>
				new Models.Agent
				{
					AgentUrl = a.Location,
					Name = EnsureNotEmpty(a.Name, "Name", "agent"),
					Monitors = a.Monitors.Select(m =>
						new Monitor
						{
							Name = m.Name,
							MonitorClass = m.Class,
							Map = m.Params.Select(p =>
								new Param
								{
									Key = p.Name,
									Value = p.Value
								}).ToList()
						}).ToList()
				}).ToList();
		}

		public static List<PeachElement.AgentElement> FromWeb(this List<Models.Agent> agents)
		{
			return agents.Select(a => 
				new PeachElement.AgentElement
				{
					Name = a.Name,
					Location = a.AgentUrl,
					Monitors = a.Monitors.Select(m => new PeachElement.AgentElement.MonitorElement
					{
						Name = m.Name,
						Class = EnsureNotEmpty(m.MonitorClass, "MonitorClass", "monitor"),
						Params = m.Map.SelectMany(ToMonitorParam).ToList()
					}).ToList()
				}).ToList();
		}

		static string EnsureNotEmpty(string value, string name, string type)
		{
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Required parameter '" + name + "' was not specified for entry in "+ type + " list.");

			return value;
		}

		static IEnumerable<PeachElement.ParamElement> ToMonitorParam(Param p)
		{
			// For backwards compatibility:
			// Peach 3.7 posts name/value, Peach 3.8 posts key/value
			var key = EnsureNotEmpty(p.Key ?? p.Name, "Key", "monitor parameter");

			if (String.IsNullOrEmpty(p.Value))
				return new PeachElement.ParamElement[0];

			if (key != "StartMode")
				return new[] { new PeachElement.ParamElement { Name = key, Value = p.Value } };

			switch (p.Value)
			{
				case "StartOnCall": // File fuzzing
					return new[]
					{
						new PeachElement.ParamElement
						{
							Name = "StartOnCall",
							Value = "ExitIterationEvent"
						}
					};
				case "RestartOnEachTest": // Network client
					return new[]
					{
						new PeachElement.ParamElement
						{
							Name = "StartOnCall",
							Value = "StartIterationEvent"
						},
						new PeachElement.ParamElement
						{
							Name = "WaitForExitOnCall",
							Value = "ExitIterationEvent"
						}
					};
				default: // Network server
					return new[]
					{
						new PeachElement.ParamElement
						{
							Name = "RestartOnEachTest",
							Value = "false"
						}
					};
			}
		}

		public static List<ParamDetail> ToWeb(this PitDefines defines)
		{
			var reserved = defines.SystemDefines.Select(d => d.Key).ToList();

			var ret = DefineToParamDetail(defines.Children, reserved) ?? new List<ParamDetail>();

			reserved = new List<string>();

			ret.Add(new ParamDetail
			{
				Key = "SystemDefines",
				Name = "System Defines",
				Description = "These values are controlled by Peach.",
				Type = ParameterType.Group,
				Collapsed = true,
				OS = "",
				Items = defines.SystemDefines.Select(d => DefineToParamDetail(d, reserved)).ToList()
			});

			return ret;
		}

		public static void ApplyWeb(this PitDefines defines, List<Param> config)
		{
			const string UserDefinesName = "User Defines";
			const string UserDefinesDesc = "User provided configuration variables";

			var visited = new HashSet<string>();
			var missing = new HashSet<string>();
			var reserved = defines.SystemDefines.Select(d => d.Key).ToList();

			foreach (var def in defines.Walk())
			{
				visited.Add(def.Key);

				if (reserved.Contains(def.Key))
					continue;

				if (def.ConfigType == ParameterType.Space || def.ConfigType == ParameterType.Group)
					continue;

				var cfg = config.FirstOrDefault(i => i.Key == def.Key);
				if (cfg != null)
					def.Value = cfg.Value;
				else
					missing.Add(def.Key);
			}

			var newDefines = config.Where(c => !visited.Contains(c.Key)).ToList();
			var userDefines = defines.Children.LastOrDefault();

			if (newDefines.Count > 0)
			{
				if (userDefines == null || userDefines.Name != UserDefinesName)
				{
					userDefines = new PitDefines.Group { Name = UserDefinesName };

					defines.Children.Add(userDefines);
				}

				foreach (var def in newDefines)
				{
					if (reserved.Contains(def.Key))
						continue;

					userDefines.Children.Add(new PitDefines.UserDefine
					{
						Key = def.Key,
						Name = def.Name,
						Value = def.Value,
						Description = def.Description
					});
				}
			}

			if (userDefines != null && userDefines.Name == UserDefinesName)
			{
				userDefines.Description = UserDefinesDesc;

				userDefines.Children.RemoveAll(d => missing.Contains(d.Key));

				if (userDefines.Children.Count == 0)
					defines.Children.Remove(userDefines);
			}
		}

		private static List<ParamDetail> DefineToParamDetail(IEnumerable<PitDefines.Define> defines, List<string> reserved)
		{
			if (defines == null)
				return null;

			var ret = defines
				.Where(d => !reserved.Contains(d.Key))
				.Select(d => DefineToParamDetail(d, reserved))
				.ToList();

			return ret.Count > 0 ? ret : null;
		}

		private static ParamDetail DefineToParamDetail(PitDefines.Define define, List<string> reserved)
		{
			var grp = define as PitDefines.Collection;

			return new ParamDetail
			{
				Key = define.Key,
				Name = define.Name,
				Value = define.Value,
				Optional = define.Optional,
				Options = define.Defaults != null ? define.Defaults.ToList() : null,
				OS = grp != null ? grp.Platform.ToString() : null,
				Collapsed = grp != null && grp.Collapsed,
				Type = define.ConfigType,
				Min = define.Min,
				Max = define.Max,
				Description = define.Description,
				Items = DefineToParamDetail(define.Defines, reserved)
			};
		}
	}
}
