using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	internal static class Extensions
	{
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
			if (string.IsNullOrEmpty(value))
				throw new ArgumentException("Required parameter '" + name + "' was not specified for entry in "+ type + " list.");

			return value;
		}

		static IEnumerable<PeachElement.ParamElement> ToMonitorParam(Param p)
		{
			// For backwards compatibility:
			// Peach 3.7 posts name/value, Peach 3.8 posts key/value
			var key = EnsureNotEmpty(p.Key ?? p.Name, "Key", "monitor parameter");

			if (string.IsNullOrEmpty(p.Value))
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
			var ret = DefineToParamDetail(defines.Children);

			ret.Add(new ParamDetail
			{
				Key = "SystemDefines",
				Name = "System Defines",
				Description = "These values are controlled by Peach.",
				Type = ParameterType.Group,
				Collapsed = true,
				OS = "",
				Items = defines.SystemDefines.Select(DefineToParamDetail).ToList()
			});

			return ret;
		}

		public static void ApplyWeb(this PitDefines defines, List<Param> config)
		{
			//var reserved = new HashSet<string>();

			//foreach (var def in defines.Platforms.SelectMany(p => p.Defines))
			//{
			//	var param = config.SingleOrDefault(x => x.Key == def.Key);
			//	if (param != null)
			//		def.Value = param.Value;

			//}

			//if (File.Exists(fileName))
			//{
			//	foreach (var def in PitDefines.Parse(fileName))
			//	{
			//		if (def.ConfigType != ParameterType.User && def.ConfigType != ParameterType.System)
			//		{
			//			var param = config.SingleOrDefault(x => x.Key == def.Key);
			//			if (param != null)
			//			{
			//				def.Value = param.Value;
			//			}
			//			defines.Add(def);
			//			reserved.Add(def.Key);
			//		}
			//	}
			//}

			//foreach (var param in config)
			//{
			//	if (!reserved.Contains(param.Key))
			//	{
			//		defines.Add(new PitDefines.UserDefine
			//		{
			//			Key = param.Key,
			//			Name = param.Key,
			//			Value = param.Value,
			//			Description = ""
			//		});
			//	}
			//}

			//var final = new PitDefines
			//{
			//	Platforms = new List<PitDefines.Collection>(new[] {
			//		new PitDefines.All
			//		{
			//			Defines = defines.ToList(),
			//		}
			//	}),
			//};
		}

		private static List<ParamDetail> DefineToParamDetail(IEnumerable<PitDefines.Define> defines)
		{
			if (defines == null)
				return null;

			var ret = defines.Select(DefineToParamDetail).ToList();

			return ret.Count > 0 ? ret : null;
		}

		private static ParamDetail DefineToParamDetail(PitDefines.Define define)
		{
			var grp = define as PitDefines.Collection;

			return new ParamDetail
			{
				Key = define.Key,
				Name = define.Name,
				Value = define.Value,
				Optional = define.Optional,
				Options = grp != null ? null : define.Defaults.ToList(),
				OS = grp != null ? grp.Platform.ToString() : null,
				Type = define.ConfigType,
				Min = define.Min,
				Max = define.Max,
				Description = define.Description,
				Items = DefineToParamDetail(define.Defines)
			};
		}
	}
}
