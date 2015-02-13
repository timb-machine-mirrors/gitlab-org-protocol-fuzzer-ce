using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Logger = NLog.Logger;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class MonitorHandler : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
			private readonly Dictionary<string, Stream> _data;

			public string Name { get { return Url; } }

			public string Url { get; private set; }

			public List<string> Messages { get; private set; }

			public Context(MonitorHandler handler, ConnectRequest req)
			{
				_handler = handler;
				_monitors = new NamedCollection<Monitor>();
				_data = new Dictionary<string, Stream>();

				Url = "/pa/agent/" + Guid.NewGuid();
				Messages = new List<string>();

				CreateMonitors(req);
			}

			public void Dispose()
			{
				FlushCachedMonitorData();

				foreach (var mon in _monitors.Reverse())
				{
					try
					{
						mon.SessionFinished();
					}
					catch (Exception ex)
					{
						Logger.Debug("Ignoring session finished exception on {0} monitor '{1}'.", mon.Class, mon.Name);
						Logger.Debug(ex.Message);
					}
				}

				foreach (var mon in _monitors.Reverse())
				{
					try
					{
						mon.StopMonitor();
					}
					catch (Exception ex)
					{
						Logger.Debug("Ignoring stop exception on {0} monitor '{1}'.", mon.Class, mon.Name);
						Logger.Debug(ex.Message);
					}
				}

				_monitors.Clear();

				foreach (var msg in Messages)
				{
					_handler._routes.Remove(Url + "/" + msg);
				}

				_handler._routes.Remove(Url);

				_handler._contexts.Remove(this);
			}

			private void CreateMonitors(ConnectRequest req)
			{
				// Add DELETE first so if we fail to start we can just call Dispose()
				_handler._routes.Add(Url, "DELETE", OnAgentDisconnect);

				var calls = new HashSet<string>();

				try
				{
					foreach (var item in req.Monitors)
					{
						var cls = item.Class;
						var key = item.Name ?? _monitors.UniqueName();
						var type = ClassLoader.FindPluginByName<MonitorAttribute>(cls);

						if (type == null)
							throw new PeachException("Error, unable to locate monitor '{0}'.".Fmt(cls)); 

						var mon = (Monitor)Activator.CreateInstance(type, new object[] { key });
						mon.StartMonitor(item.Args);

						foreach (var kv in item.Args.Where(kv => kv.Key.EndsWith("OnCall")))
							calls.Add(kv.Value);

						_monitors.Add(mon);
					}

					foreach (var item in _monitors)
					{
						item.SessionStarting();
					}
				}
				catch
				{
					Dispose();

					throw;
				}

				Messages.AddRange(new[]
				{
					"IterationStarting",
					"IterationFinished",
					"DetectedFault",
					"GetMonitorData"
				});

				_handler._routes.Add(Url + "/IterationStarting", "PUT", OnIterationStarting);
				_handler._routes.Add(Url + "/IterationFinished", "PUT", OnIterationFinished);
				_handler._routes.Add(Url + "/DetectedFault", "GET", DetectedFault);
				_handler._routes.Add(Url + "/GetMonitorData", "GET", GetMonitorData);

				foreach (var item in calls)
				{
					Messages.Add("Message/" + item);
					_handler._routes.Add(Url + "/Message/" + item, "PUT", OnMessage);
				}
			}

			private RouteResponse OnIterationStarting(HttpListenerRequest req)
			{
				FlushCachedMonitorData();

				var json = req.FromJson<IterationStartingRequest>();
				var args = new IterationStartingArgs
				{
					IsReproduction = json.IsReproduction,
					LastWasFault = json.LastWasFault
				};

				foreach (var mon in _monitors)
				{
					mon.IterationStarting(args);
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
						Title = fault.Title,
						Data = new List<FaultResponse.Record.FaultData>()
					};

					foreach (var kv in fault.Data)
					{
						item.Data.Add(new FaultResponse.Record.FaultData
						{
							Key = kv.Key,
							Size = kv.Value.Length,
							Url = CacheMonitorData(kv.Value),
						});
					}

					if (fault.Fault != null)
					{
						item.Fault = new FaultResponse.Record.FaultDetail
						{
							Description = fault.Fault.Description,
							MajorHash = fault.Fault.MajorHash,
							MinorHash = fault.Fault.MinorHash,
							Risk = fault.Fault.Risk,
							MustStop = fault.Fault.MustStop
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

			private void FlushCachedMonitorData()
			{
				foreach (var kv in _data)
					_handler._routes.Remove(kv.Key);

				_data.Clear();
			}

			private string CacheMonitorData(byte[] data)
			{
				var url = "/pa/file/" + Guid.NewGuid();
				var stream = new MemoryStream(data);

				_data.Add(url, stream);
				_handler._routes.Add(url, "GET", req => RouteResponse.AsStream(stream));

				return url;
			}
		}
	}
}
