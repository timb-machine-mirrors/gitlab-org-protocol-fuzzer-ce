using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class MonitorHandler : IDisposable
	{
		private readonly NamedCollection<Context> _contexts;
		private readonly RouteHandler _routes;

		public MonitorHandler(RouteHandler routes)
		{

			_contexts = new NamedCollection<Context>();
			_routes = routes;
			_routes.Add("/p/agent", "POST", OnAgentConnect);
		}

		public void Dispose()
		{
			while (_contexts.Count > 0)
			{
				_contexts[0].Dispose();
			}
		}

		private RouteResponse OnAgentConnect(HttpListenerRequest req)
		{
			var ctx = new Context(this, req.FromJson<ConnectRequest>());

			_contexts.Add(ctx);

			var resp = new ConnectResponse
			{
				Url = ctx.Url,
				Messages = ctx.Messages,
			};

			return RouteResponse.AsJson(resp, HttpStatusCode.Created);
		}

		class Context : INamed, IDisposable
		{
			#region Obsolete Functions

			[Obsolete("This property is obsolete and has been replaced by the Name property.")]
			public string name { get { return Name; } }

			#endregion

			private readonly MonitorHandler _handler;
			private readonly NamedCollection<Monitor> _monitors;

			public string Name { get { return Url; } }

			public string Url { get; private set; }

			public List<string> Messages { get; private set; }

			public Context(MonitorHandler handler, ConnectRequest req)
			{
				_handler = handler;
				_monitors = new NamedCollection<Monitor>();

				Url = "/pa/agent/" + Guid.NewGuid();
				Messages = new List<string>();

				CreateMonitors(req);
			}

			public void Dispose()
			{
				foreach (var mon in _monitors.Reverse())
				{
					mon.SessionFinished();
				}

				foreach (var mon in _monitors.Reverse())
				{
					mon.StopMonitor();
				}

				_monitors.Clear();

				foreach (var msg in Messages)
				{
					_handler._routes.Remove(Url + msg);
				}

				_handler._routes.Remove(Url);

				_handler._contexts.Remove(this);
			}

			private void CreateMonitors(ConnectRequest req)
			{
				var calls = new HashSet<string>();

				foreach (var item in req.Monitors)
				{
					var cls = item.Class;
					var key = item.Name ?? _monitors.UniqueName();
					var type = ClassLoader.FindTypeByAttribute<MonitorAttribute>((t, a) => a.Name == cls);

					if (type == null)
						throw new PeachException("Couldn't load monitor");

					var mon = (Monitor) Activator.CreateInstance(type, new object[] {key});
					mon.StartMonitor(item.Args);
					foreach (var kv in item.Args.Where(kv => kv.Key.EndsWith("OnCall")))
					{
						calls.Add(kv.Value);
					}

					_monitors.Add(mon);
				}

				foreach (var item in _monitors)
				{
					item.SessionStarting();
				}

				Messages.AddRange(new[]
				{
					"/IterationStarting",
					"/IterationFinished",
					"/DetectedFault",
					"/GetMonitorData"
				});

				_handler._routes.Add(Url, "DELETE", OnAgentDisconnect);
				_handler._routes.Add(Url + "/IterationStarting", "PUT", OnIterationStarting);
				_handler._routes.Add(Url + "/IterationFinished", "PUT", OnIterationFinished);
				_handler._routes.Add(Url + "/DetectedFault", "GET", DetectedFault);
				_handler._routes.Add(Url + "/GetMonitorData", "GET", GetMonitorData);

				foreach (var item in calls)
				{
					Messages.Add("/Message/" + item);
					_handler._routes.Add(Url + "/Message/" + item, "PUT", OnMessage);
				}
			}

			private RouteResponse OnIterationStarting(HttpListenerRequest req)
			{
				var args = req.FromJson<IterationStartingRequest>();

				foreach (var mon in _monitors)
				{
					mon.IterationStarting(args.Iteration, args.IsReproduction);
				}

				return RouteResponse.Success();
			}

			private RouteResponse OnIterationFinished(HttpListenerRequest req)
			{
				foreach (var mon in _monitors.Reverse())
				{
					mon.IterationFinished();
				}

				return RouteResponse.Success();
			}

			private RouteResponse OnMessage(HttpListenerRequest req)
			{
				var msg = req.Url.Segments[req.Url.Segments.Length - 1];

				foreach (var mon in _monitors)
				{
					mon.Message(msg);
				}

				return RouteResponse.Success();
			}

			private RouteResponse DetectedFault(HttpListenerRequest req)
			{
				var ret = new BoolResponse { Value = false };

				foreach (var mon in _monitors)
				{
					ret.Value |= mon.DetectedFault();
				}

				return RouteResponse.AsJson(ret);
			}

			private RouteResponse GetMonitorData(HttpListenerRequest req)
			{
				var ret = new FaultResponse { Faults = new List<FaultResponse.Record>() };

				foreach (var mon in _monitors)
				{
					var fault = mon.GetMonitorData();
					if (fault == null)
						continue;

					var item = new FaultResponse.Record
					{
						MonitorName = mon.Name,
						DetectionSource = mon.Class,
						Data = new List<FaultResponse.Record.FaultData>()
					};

					foreach (var data in fault.collectedData)
					{
						item.Data.Add(new FaultResponse.Record.FaultData
						{
							Key = data.Key,
							Value = data.Value,
						});
					}

					if (fault.type == FaultType.Fault)
					{
						item.Fault = new FaultResponse.Record.FaultDetail
						{
							Title = fault.title,
							Description = fault.description,
							MajorHash = fault.majorHash,
							MinorHash = fault.minorHash,
							Risk = fault.exploitability,
							MustStop = mon.MustStop(),
						};
					}

					ret.Faults.Add(item);
				}

				return RouteResponse.AsJson(ret);
			}

			private RouteResponse OnAgentDisconnect(HttpListenerRequest req)
			{
				Dispose();

				return RouteResponse.Success();
			}
		}
	}
}
