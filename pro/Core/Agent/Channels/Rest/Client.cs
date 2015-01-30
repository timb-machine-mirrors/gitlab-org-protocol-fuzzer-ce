using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	[Agent("json")]
	public class Client : AgentClient
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly Uri _baseUrl;

		private ConnectRequest _connectReq;
		private ConnectResponse _connectResp;
		private Uri _agentUrl;
		private bool _mustStop;

		public Client(string name, string uri, string password)
			: base(name, uri, password)
		{
			_baseUrl = new Uri(uri);

			if (_baseUrl.IsDefaultPort)
				_baseUrl = new Uri("{0}://{1}:{2}".Fmt(_baseUrl.Scheme, _baseUrl.Host, Server.DefaultPort));

			_baseUrl = new Uri("http://{0}:{1}".Fmt(_baseUrl.Host, _baseUrl.Port));
		}

		public override void AgentConnect()
		{
			_connectReq = new ConnectRequest
			{
				Monitors = new List<ConnectRequest.Monitor>(),
			};
		}

		public override void StartMonitor(string monName, string cls, Dictionary<string, string> args)
		{
			_connectReq.Monitors.Add(new ConnectRequest.Monitor
			{
				Name = monName,
				Class = cls,
				Args = args
			});
		}

		public override void SessionStarting()
		{
			// Send the initial POST to the base url
			_agentUrl = new Uri(_baseUrl, "/p/agent");
			_connectResp = Send<ConnectResponse>("POST", "", _connectReq);
			_agentUrl = new Uri(_baseUrl, _connectResp.Url);
		}

		public override void SessionFinished()
		{
			Send("DELETE", "", null);

			_connectResp = null;
			_agentUrl = null;
		}

		public override void StopAllMonitors()
		{
			// OnSessionFinished does StopAllMonitors
		}

		public override void AgentDisconnect()
		{
			// OnSessionFinished does AgentDisconnect
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			throw new NotImplementedException();
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			// If any previous call failed, we need to reconnect
			if (_agentUrl == null)
				SessionStarting();

			// Reset any state from a previous call to GetMoniorData
			_mustStop = false;

			if (!_connectResp.Messages.Contains("/IterationStarting"))
				return;

			var req = new IterationStartingRequest
			{
				IsReproduction = args.IsReproduction,
				LastWasFault = args.LastWasFault,
			};

			Send("PUT", "/IterationStarting", req);
		}

		public override void IterationFinished()
		{
			if (_connectResp.Messages.Contains("/IterationFinished"))
				Send("PUT", "/IterationFinished", null);
		}

		public override bool DetectedFault()
		{
			if (!_connectResp.Messages.Contains("/DetectedFault"))
				return false;

			var resp = Send<BoolResponse>("GET", "/DetectedFault", null);

			return resp.Value;
		}

		public override IEnumerable<Fault> GetMonitorData()
		{
			if (!_connectResp.Messages.Contains("/GetMonitorData"))
				return null;

			var resp = Send<FaultResponse>("GET", "/GetMonitorData", null);

			if (resp.Faults == null)
				return null;

			var ret = resp.Faults.Select(MakeFault);

			return ret;
		}

		public override bool MustStop()
		{
			return _mustStop;
		}

		public override void Message(string msg)
		{
			var path = "/Message/{0}".Fmt(msg);

			if (_connectResp.Messages.Contains(path))
				Send("PUT", path, null);
		}

		private Fault MakeFault(FaultResponse.Record f)
		{
			var ret = new Fault
			{
				type = FaultType.Data,
				agentName = Name,
				monitorName = f.MonitorName,
				detectionSource = f.DetectionSource,
				collectedData = f.Data.Select(d => new Fault.Data
				{
					Key = d.Key,
					Value = d.Value,
				}).ToList(),
			};

			if (f.Fault != null)
			{
				ret.type = FaultType.Fault;
				ret.title = f.Fault.Title;
				ret.description = f.Fault.Description;
				ret.majorHash = f.Fault.MajorHash;
				ret.minorHash = f.Fault.MinorHash;
				ret.exploitability =f.Fault.Risk;

				_mustStop |= f.Fault.MustStop;
			}

			return ret;
		}

		private void Send(string method, string path, object request)
		{
			Execute(method, path, request, resp => resp.Consume());
		}

		private T Send<T>(string method, string path, object request)
		{
			return Execute(method, path, request, req => req.FromJson<T>());
		}

		private T Execute<T>(string method,
			string path,
			object request,
			Func<HttpWebResponse, T> decode)
		{
			if (_agentUrl == null)
			{
				Logger.Debug("Agent server '{0}' is offline, ignoring '{1}' command.", Url, path);
				return default(T);
			}

			var uri = new Uri(_agentUrl, path);

			Logger.Trace("{0} {1}", method, uri);

			try
			{
				var req = (HttpWebRequest) WebRequest.Create(uri);

				req.Method = method;
				req.SendJson(request);

				using (var resp = (HttpWebResponse) req.GetResponse())
				{
					Logger.Trace(">>> {0} {1}", (int) resp.StatusCode, resp.StatusDescription);

					return decode(resp);
				}
			}
			catch (WebException ex)
			{
				if (ex.Status != WebExceptionStatus.ProtocolError)
				{
					Logger.Debug(ex.Message);

					// Clear the agent url to trigger a future reconnect
					_agentUrl = null;

					throw;
				}

				using (var resp = (HttpWebResponse)ex.Response)
				{
					Logger.Trace("<<< {0} {1}", (int) resp.StatusCode, resp.StatusDescription);

					// If we get a 500 or 503, this means the command we ran
					// failed to complete, but our agentUrl is still valid
					// so we don't want to clear the agent url

					if (resp.StatusCode != HttpStatusCode.InternalServerError &&
					    resp.StatusCode != HttpStatusCode.ServiceUnavailable)
					{
						Logger.Debug(ex.Message);

						// Clear the agent url to trigger a future reconnect
						_agentUrl = null;

						// Consume all bytes sent to us in the response
						resp.Consume();

						throw;
					}

					var error = resp.FromJson<ExceptionResponse>();

					Logger.Trace(error.Message);
					Logger.Trace("Server Stack Trace:\n{0}", error.StackTrace);

					// 503 means try again later
					if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
						throw new SoftException(error.Message, ex);

					// 500 is hard fail
					throw new PeachException(error.Message, ex);
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex.Message);
				Logger.Trace(ex);

				throw;
			}
		}
	}
}
