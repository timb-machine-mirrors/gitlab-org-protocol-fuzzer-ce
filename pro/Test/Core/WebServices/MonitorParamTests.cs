using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	class MonitorParamTests
	{
		enum ItemType {  Group, Space, Monitor, Param }

		class TypedItem
		{
			public string Name { get; set; }
			public ItemType Type { get; set; }
			public List<TypedItem> Items { get; set; }
		}

		private static List<ParamDetail> AsParameter(List<TypedItem> items, List<ParameterAttribute> parameters)
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
						//var param = parameters.First(p => p.name == item.Name);
						//ret.Add(new PitDatabase().ParameterAttrToModel("unknown", param));
						break;
				}
			}

			return ret;
		}


		[Test]
		public void JsonDecode()
		{
			var record = new List<TypedItem>
			{
				new TypedItem
				{
					Name = "Power Control",
					Type = ItemType.Group,
					Items = new List<TypedItem>
					{
						new TypedItem
						{
							Name = "Gdb",
							Type = ItemType.Monitor,
							Items = new List<TypedItem>
							{
								new TypedItem
								{
									Name = "Core Parameters",
									Type = ItemType.Group,
									Items = new List<TypedItem>
									{
										new TypedItem
										{
											Name = "Executable",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Name = "Executable",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Type = ItemType.Space
										},
										new TypedItem
										{
											Name = "GdbPath",
											Type = ItemType.Param
										}
									}
								},
								new TypedItem
								{
									Name = "When To Trigger",
									Type = ItemType.Group,
									Items = new List<TypedItem>
									{
										new TypedItem
										{
											Name = "RestartOnEachTest",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Name = "RestartAfterFault",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Type = ItemType.Space
										},
										new TypedItem
										{
											Name = "StartOnCall",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Name = "WaitForExitOnCall",
											Type = ItemType.Param
										}
									}
								},
								new TypedItem
								{
									Name = "Advanced",
									Type = ItemType.Group,
									Items = new List<TypedItem>
									{
										new TypedItem
										{
											Name = "NoCpuKill",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Name = "FaultOnEarlyExit",
											Type = ItemType.Param
										},
										new TypedItem
										{
											Type = ItemType.Space
										},
										new TypedItem
										{
											Name = "WaitForExitTimeout",
											Type = ItemType.Param
										}
									}
								}
							}
						}
					}
				}
			};

			var json = JsonConvert.SerializeObject(record, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore
			});


			var obj = JsonConvert.DeserializeObject<List<TypedItem>>(json);

			Assert.NotNull(obj);

			//Console.WriteLine(json);

			var p = AsParameter(obj, null);
			json = JsonConvert.SerializeObject(p, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
			});

			Assert.NotNull(json);

			//Console.WriteLine(json);
		}

		[Test]
		public void TestMonitorAttributes()
		{
			var calls = new List<string> { "Foo", "Bar" };

			var details = MonitorMetadata.Generate(calls);

			var json = JsonConvert.SerializeObject(details, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
			});

			Assert.NotNull(json);

			Console.WriteLine(json);
		}
	}
}
