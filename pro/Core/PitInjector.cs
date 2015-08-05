using System.Collections.Generic;
using Peach.Core;
using DomAgent = Peach.Core.Dom.Agent;
using DomMonitor = Peach.Core.Dom.Monitor;
using DomObject = Peach.Core.Dom.Dom;
using WebAgent = Peach.Pro.Core.WebServices.Models.Agent;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core
{
    public static class PitInjector
    {
		public static void InjectConfig(PitConfig cfg, DomObject dom)
		{
			var agents = new NamedCollection<DomAgent>();
			foreach (var agent in cfg.Agents)
			{
				agents.Add(new Peach.Core.Dom.Agent
				{
					Name = agent.Name ?? agents.UniqueName(),
					location = agent.AgentUrl,
					monitors = ConvertMonitors(agent),
				});
			}

			foreach (var test in dom.tests)
			{
				test.agents = agents;
			}
		}
		
		private static NamedCollection<DomMonitor> ConvertMonitors(WebAgent agent)
		{
			var monitors = new NamedCollection<DomMonitor>();
			foreach (var monitor in agent.Monitors)
			{
				monitors.Add(new DomMonitor
				{
					cls = monitor.MonitorClass,
					Name = monitor.Name ?? monitors.UniqueName(),
					parameters = ConvertParameters(monitor),
				});
			}
			return monitors;
		}

		private static Dictionary<string, Variant> ConvertParameters(Monitor monitor)
		{
			var ret = new Dictionary<string, Variant>();
			foreach (var x in monitor.Map)
			{
				if (x.Name == "StartMode")
				{
					switch (x.Value)
					{
					case "StartOnCall":
						ret.Add("StartOnCall", new Variant("ExitIterationEvent"));
						break;
					case "RestartOnEachTest":
						ret.Add("StartOnCall", new Variant("StartIterationEvent"));
						ret.Add("WaitForExitOnCall", new Variant("ExitIterationEvent"));
						break;
					default:
						ret.Add("RestartOnEachTest", new Variant(false));
						break;
					}
				}
				else
				{
					ret.Add(x.Name, new Variant(x.Value));
				}
			}
			return ret;
		}
    }
}

