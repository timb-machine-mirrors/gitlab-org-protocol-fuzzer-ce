using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.IO;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	[Agent("json")]
	public class Client : AgentClient
	{
		class PublisherProxy : IPublisher
		{
			readonly Client _client;
			readonly PublisherRequest _createReq;

			Uri _publisherUri;

			public PublisherProxy(Client client, string name, string cls, Dictionary<string, string> args)
			{
				_client = client;
				_createReq = new PublisherRequest
				{
					Name = name,
					Class = cls,
					Args = args,
				};

				InputStream = new MemoryStream();

				Connect();

				_client._publishers.Add(this);
			}

			public void Dispose()
			{
				Send("DELETE", "", null);

				InputStream.Dispose();
				InputStream = null;

				_client._publishers.Remove(this);
			}

			public Stream InputStream
			{
				get;
				private set;
			}

			public void Connect()
			{
				_publisherUri = new Uri(_client._baseUrl, "/p/publisher");
				var resp = Send<PublisherResponse>("POST", "", _createReq);
				_publisherUri = new Uri(_client._baseUrl, resp.Url);
			}

			public void Open(uint iteration, bool isControlIteration)
			{
				var req = new PublisherOpenRequest
				{
					Iteration = iteration,
					IsControlIteration = isControlIteration,
				};

				Send("PUT", "/open", req);

				InputStream.Position = 0;
				InputStream.SetLength(0);
			}

			public void Close()
			{
				Send("PUT", "/close", null);
			}

			public void Accept()
			{
				Send("PUT", "/accept", null);
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				var req = new CallRequest
				{
					Method = method,
					Args = new List<CallRequest.Param>()
				};

				foreach (var arg in args)
				{
					var param = new CallRequest.Param
					{
						Name = arg.Name,
						Value = new byte[arg.Length]
					};

					arg.Seek(0, SeekOrigin.Begin);
					arg.Read(param.Value, 0, param.Value.Length);

					req.Args.Add(param);
				}

				var resp = Send<CallResponse>("PUT", "/call", req);

				return resp.ToVariant();
			}

			public void SetProperty(string property, Variant value)
			{
				var req = value.ToModel<SetPropertyRequest>();
				req.Property = property;
				Send("PUT", "/property", req);
			}

			public Variant GetProperty(string property)
			{
				var req = new GetPropertyRequest { Property = property };
				var resp = Send<VariantMessage>("GET", "/property", req);
				return resp.ToVariant();
			}

			public void Output(BitwiseStream data)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + "/output");
				var request = RouteResponse.AsStream(data);
				_client.Execute("PUT", uri, request, SendStream, resp => resp.Consume());
			}

			public void Input()
			{
				var resp = Send<BoolResponse>("PUT", "/input", null);
				if (resp.Value)
				{
					InputStream.Position = 0;
					InputStream.SetLength(0);
				}

				// Read all input bytes starting at offset 'Length'
				// so we don't re-download bytes we have already gotten.

				ReadInputData("?offset={0}".Fmt(InputStream.Length));
			}

			public void WantBytes(long count)
			{
				var needed = count - InputStream.Length + InputStream.Position;

				if (needed > 0)
					ReadInputData("?offset={0}&count={1}".Fmt(InputStream.Length, needed));
			}

			private void Send(string method, string path, object request)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + path);
				_client.Execute(method, uri, request, SendJson, resp => resp.Consume());
			}

			private T Send<T>(string method, string path, object request)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + path);
				return _client.Execute(method, uri, request, SendJson, req => req.FromJson<T>());
			}

			private void ReadInputData(string query)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + "/data" + query);

				_client.Execute("GET", uri, (object)null, null, resp =>
				{
					var pos = InputStream.Position;

					try
					{
						InputStream.Seek(0, SeekOrigin.End);

						using (var strm = resp.GetResponseStream())
						{
							if (strm != null)
								strm.CopyTo(InputStream);
						}
					}
					finally
					{
						InputStream.Seek(pos, SeekOrigin.Begin);
					}

					return (object)null;
				});
			}
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly Uri _baseUrl;
		private readonly List<PublisherProxy> _publishers;

		private ConnectRequest _connectReq;
		private ConnectResponse _connectResp;
		private Uri _agentUri;
		private bool _offline;

		public Client(string name, string uri, string password)
			: base(name, uri, password)
		{
			_baseUrl = new Uri(uri);

			if (_baseUrl.IsDefaultPort)
				_baseUrl = new Uri("{0}://{1}:{2}".Fmt(_baseUrl.Scheme, _baseUrl.Host, Server.DefaultPort));

			_baseUrl = new Uri("http://{0}:{1}".Fmt(_baseUrl.Host, _baseUrl.Port));

			_publishers = new List<PublisherProxy>();
		}

		public override void AgentConnect()
		{
			_offline = false;

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
			_offline = false;

			if (_connectReq.Monitors.Count > 0)
			{
				// Send the initial POST to the base url
				_agentUri = new Uri(_baseUrl, "/p/agent");
				_connectResp = Send<ConnectResponse>("POST", "", _connectReq);
				_agentUri = new Uri(_baseUrl, _connectResp.Url);
			}

			// If we are reconnecting, ensure all the publishers are recreated
			foreach (var pub in _publishers)
				pub.Connect();
		}

		public override void SessionFinished()
		{
			Send("DELETE", "", null);

			_connectResp = null;

			// Leave the publishers around a they will get cleaned up
			// when stop() is called on the RemotePublisher
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
			return new PublisherProxy(this, pubName, cls, args);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			// If any previous call failed, we need to reconnect
			if (_offline)
				SessionStarting();

			if (!_connectResp.Messages.Contains("IterationStarting"))
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
			if (_connectResp.Messages.Contains("IterationFinished"))
				Send("PUT", "/IterationFinished", null);
		}

		public override bool DetectedFault()
		{
			if (!_connectResp.Messages.Contains("DetectedFault"))
				return false;

			var resp = Send<BoolResponse>("GET", "/DetectedFault", null);

			return resp.Value;
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			if (!_connectResp.Messages.Contains("GetMonitorData"))
				return new MonitorData[0];

			var resp = Send<FaultResponse>("GET", "/GetMonitorData", null);

			if (resp.Faults == null)
				return new MonitorData[0];

			var ret = resp.Faults.Select(MakeFault);

			return ret;
		}

		public override void Message(string msg)
		{
			var path = "Message/{0}".Fmt(msg);

			if (_connectResp.Messages.Contains(path))
				Send("PUT", path, null);
		}

		private MonitorData MakeFault(FaultResponse.Record f)
		{
			var ret = new MonitorData
			{
				AgentName = Name,
				DetectionSource = f.DetectionSource,
				MonitorName = f.MonitorName,
				Title = f.Title,
				Data = f.Data.ToDictionary(i => i.Key, DownloadFile),
			};

			if (f.Fault != null)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = f.Fault.Description,
					MajorHash = f.Fault.MajorHash,
					MinorHash = f.Fault.MinorHash,
					Risk = f.Fault.Risk,
					MustStop = f.Fault.MustStop,
				};
			}

			return ret;
		}

		private byte[] DownloadFile(FaultResponse.Record.FaultData data)
		{
			Logger.Trace("Downloading {0} byte file '{1}'.", data.Size, data.Key);

			var uri = new Uri(_baseUrl, data.Url);

			return Execute("GET", uri, (object)null, null, resp =>
			{
				using (var strm = resp.GetResponseStream())
				{
					if (strm == null)
						return new byte[0];

					var ms = new MemoryStream();
					strm.CopyTo(ms);

					return ms.ToArray();
				}
			});
		}

		private void Send(string method, string path, object request)
		{
			var uri = new Uri(_agentUri, _agentUri.PathAndQuery + path);
			Execute(method, uri, request, SendJson, resp => resp.Consume());
		}

		private T Send<T>(string method, string path, object request)
		{
			var uri = new Uri(_agentUri, _agentUri.PathAndQuery + path);
			return Execute(method, uri, request, SendJson, req => req.FromJson<T>());
		}

		private static void SendJson(HttpWebRequest req, object obj)
		{
			if (req.Method == "GET")
			{
				Debug.Assert(obj == null);
				return;
			}

			var json = RouteResponse.AsJson(obj);

			SendStream(req, json);
		}

		private static void SendStream(HttpWebRequest req, RouteResponse obj)
		{
			req.ContentType = obj.ContentType;
			req.ContentLength = obj.Content.Length;

			using (var strm = req.GetRequestStream())
				obj.Content.CopyTo(strm);
		}

		private TOut Execute<TOut,TIn>(string method,
			Uri uri,
			TIn request,
			Action<HttpWebRequest, TIn> encode,
			Func<HttpWebResponse, TOut> decode)
		{
			if (_offline)
			{
				Logger.Debug("Agent server '{0}' is offline, ignoring command '{3} {4}'.",
					Url, method, uri.PathAndQuery);
				return default(TOut);
			}

			Logger.Trace("{0} {1}", method, uri);

			try
			{
				var req = (HttpWebRequest)WebRequest.Create(uri);

				req.Method = method;

				if (Equals(request, default(TIn)))
					req.ContentLength = 0;
				else
					encode(req, request);

				using (var resp = (HttpWebResponse)req.GetResponse())
				{
					Logger.Trace(">>> {0} {1}", (int)resp.StatusCode, resp.StatusDescription);

					return decode(resp);
				}
			}
			catch (WebException ex)
			{
				if (ex.Status != WebExceptionStatus.ProtocolError)
				{
					Logger.Debug(ex.Message);

					// Mark offline to trigger a future reconnect
					_offline = true;

					throw;
				}

				using (var resp = (HttpWebResponse)ex.Response)
				{
					Logger.Trace("<<< {0} {1}", (int)resp.StatusCode, resp.StatusDescription);

					// If we get a 500 or 503, this means the command we ran
					// failed to complete, but our agentUrl is still valid
					// so we don't want to clear the agent url

					if (resp.StatusCode != HttpStatusCode.InternalServerError &&
						resp.StatusCode != HttpStatusCode.ServiceUnavailable)
					{
						Logger.Debug(ex.Message);

						// Mark offline to trigger a future reconnect
						_offline = true;

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
