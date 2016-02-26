using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

		public static List<ParamDetail> ToWeb(this PitDefines defines)
		{
			var ifaces = new IfaceOptions();
			var reserved = defines.SystemDefines.Select(d => d.Key).ToList();

			var ret = DefineToParamDetail(defines.Children, reserved, ifaces) ?? new List<ParamDetail>();

			reserved = new List<string>();

			ret.Add(new ParamDetail
			{
				Key = "SystemDefines",
				Name = "System Defines",
				Description = "These values are controlled by Peach.",
				Type = ParameterType.Group,
				Collapsed = true,
				OS = "",
				Items = defines.SystemDefines.Select(d => DefineToParamDetail(d, reserved, ifaces)).ToList()
			});

			return ret.Where(d => d.Type != ParameterType.Group || d.Items != null).ToList();
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

		private class IfaceOptions
		{
			private List<NetworkInterface> interfaces;

			public IEnumerable<NetworkInterface> Interfaces
			{
				get
				{
					// ReSharper disable once ConvertIfStatementToNullCoalescingExpression
					if (interfaces == null)
						interfaces = NetworkInterface.GetAllNetworkInterfaces()
							.Where(i => i.OperationalStatus == OperationalStatus.Up)
							.Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet
								|| i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
								|| i.NetworkInterfaceType == NetworkInterfaceType.Loopback)
							.ToList();

					return interfaces;
				}
			}
		}

		private static List<ParamDetail> DefineToParamDetail(IEnumerable<PitDefines.Define> defines, List<string> reserved, IfaceOptions ifaces)
		{
			if (defines == null)
				return null;

			var ret = defines
				.Where(d => !reserved.Contains(d.Key))
				.Select(d => DefineToParamDetail(d, reserved, ifaces))
				.ToList();

			return ret.Count > 0 ? ret : null;
		}

		private static ParamDetail DefineToParamDetail(PitDefines.Define define, List<string> reserved, IfaceOptions ifaces)
		{
			var grp = define as PitDefines.Collection;

			var ret = new ParamDetail
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
				Items = DefineToParamDetail(define.Defines, reserved, ifaces)
			};

			switch (ret.Type)
			{
				case ParameterType.Hwaddr:
					ret.Options.AddRange(
						ifaces.Interfaces
							.Select(i => i.GetPhysicalAddress().GetAddressBytes())
							.Select(a => string.Join(":", a.Select(b => b.ToString("x2"))))
							.Where(s => !string.IsNullOrEmpty(s)));
					break;
				case ParameterType.Iface:
					ret.Options.AddRange(ifaces.Interfaces.Select(i => i.Name));
					break;
				case ParameterType.Ipv4:
					ret.Options.AddRange(
						ifaces.Interfaces
							.SelectMany(i => i.GetIPProperties().UnicastAddresses)
							.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
							.Select(a => a.Address.ToString()));
					break;
				case ParameterType.Ipv6:
					ret.Options.AddRange(
						ifaces.Interfaces
							.SelectMany(i => i.GetIPProperties().UnicastAddresses)
							.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
							.Select(a => a.Address.ToString()));
					break;
			}

			return ret;
		}
	}
}
