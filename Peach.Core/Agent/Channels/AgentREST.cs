//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Adam Cecchetti (adam@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Agent.Channels
{
	#region RestProxyPublisher

	internal class RestProxyPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string Url = null;
		public string Agent { get; set; }
		public string Class { get; set; }
		public Dictionary<string, string> Args { get; set; }

		[Serializable]
		public class CreatePublisherRequest
		{
			public uint iteration { get; set; }
			public bool isControlIteration { get; set; }
			public string Cls { get; set; }
			public Dictionary<string, string> args { get; set; }
		}

		[Serializable]
		public class RestProxyPublisherResponse
		{
			public bool error { get; set; }
			public string errorString { get; set; }
		}

		public RestProxyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			this.Args = new Dictionary<string,string>();

			foreach (var kv in args)
			{
				// Note: Cast to string rather than ToString()
				// since ToString can include debugging information.
				this.Args.Add(kv.Key, (string)kv.Value);
			}
		}

		public string Send(string query)
		{
			return Send(query, "");
		}

		public string Send(string query, Dictionary<string, Variant> args)
		{
			var newArg = new Dictionary<string, string>();

			foreach (var kv in args)
			{
				// NOTE: cast to string, rather than .ToString() since
				// .ToString() can include debugging information.
				newArg.Add(kv.Key, (string)kv.Value);
			}

			JsonArgsRequest request = new JsonArgsRequest();
			request.args = newArg;

			return Send(query, JsonConvert.SerializeObject(request));
		}

		public string Send(string query, string json, bool restart = true)
		{
			try
			{
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url + "/Publisher/" + query);
				httpWebRequest.ContentType = "text/json";

				if (string.IsNullOrEmpty(json))
				{
					httpWebRequest.Method = "GET";
				}
				else
				{
					httpWebRequest.Method = "POST";
					using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					{
						streamWriter.Write(json);
					}
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

				if (httpResponse.GetResponseStream() != null)
				{
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var jsonResponse = streamReader.ReadToEnd();
						var response = JsonConvert.DeserializeObject<RestProxyPublisherResponse>(jsonResponse);

						if (response.error)
						{
							logger.Warn("Query \"" + query + "\" error: " + response.errorString);
							RestartRemotePublisher();

							jsonResponse = Send(query, json, false);
							response = JsonConvert.DeserializeObject<RestProxyPublisherResponse>(jsonResponse);

							if (response.error)
							{
								logger.Warn("Unable to restart connection");
								throw new SoftException("Query \"" + query + "\" error: " + response.errorString);
							}
						}

						return jsonResponse;
					}
				}
				else
				{
					return "";
				}
			}
			catch (SoftException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new SoftException("Failure communicating with REST Agent", e);
			}
		}

		protected void RestartRemotePublisher()
		{
			logger.Debug("Restarting remote publisher");

			CreatePublisherRequest request = new CreatePublisherRequest();
			request.iteration = Iteration;
			request.isControlIteration = IsControlIteration;
			request.Cls = Class;
			request.args = Args;

			Send("CreatePublisher", JsonConvert.SerializeObject(request));
		}

		[Serializable]
		public class IterationRequest
		{
			public uint iteration { get; set; }
		}

		public override uint Iteration
		{
			get
			{
				return base.Iteration;
			}

			set
			{
				base.Iteration = value;

				IterationRequest request = new IterationRequest();
				request.iteration = value;

				Send("Set_Iteration", JsonConvert.SerializeObject(request));
			}
		}

		[Serializable]
		public class IsControlIterationRequest
		{
			public bool isControlIteration { get; set; }
		}

		public override bool IsControlIteration
		{
			get
			{
				return base.IsControlIteration;
			}
			set
			{
				base.IsControlIteration = value;

				var request = new IsControlIterationRequest();
				request.isControlIteration = value;

				Send("Set_IsControlIteration", JsonConvert.SerializeObject(request));
			}
		}

		[Serializable]
		public class ResultRequest
		{
			public string result { get; set; }
		}

		[Serializable]
		public class ResultResponse: RestProxyPublisherResponse
		{
			public string result { get; set; }
		}

		public override string Result
		{
			get
			{
				return JsonConvert.DeserializeObject<ResultRequest>(Send("Get_Result")).result;
			}
		}

		protected override void OnStart()
		{
			IsControlIteration = IsControlIteration;
			Iteration = Iteration;

			Send("start");
		}

		protected override void OnStop()
		{
			Send("stop");
		}

		protected override void OnOpen()
		{
			Send("open");
		}

		protected override void OnClose()
		{
			Send("close");
		}

		protected override void OnAccept()
		{
			Send("accept");
		}

		[Serializable]
		public class OnCallArgument
		{
			public string name { get; set; }
			public byte[] data { get; set; }
			public ActionParameter.Type type { get; set; }
		}

		[Serializable]
		public class OnCallRequest
		{
			public string method { get; set; }
			public OnCallArgument[] args { get; set; }
		}

		[Serializable]
		public class OnCallResponse : RestProxyPublisherResponse
		{
			public Variant value { get; set; }
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			var request = new OnCallRequest();

			request.method = method;
			request.args = new OnCallArgument[args.Count];

			for (int cnt = 0; cnt < args.Count; cnt++)
			{
				request.args[cnt] = new OnCallArgument();
				request.args[cnt].name = args[cnt].name;
				request.args[cnt].type = args[cnt].type;
				request.args[cnt].data = new byte[args[cnt].dataModel.Value.Length];
				args[cnt].dataModel.Value.Read(request.args[cnt].data, 0, (int)args[cnt].dataModel.Value.Length);
			}

			var json = Send("call", JsonConvert.SerializeObject(request));
			var response = JsonConvert.DeserializeObject<OnCallResponse>(json);

			return response.value;
		}

		[Serializable]
		public class OnSetPropertyRequest
		{
			public string property { get; set; }
			public byte[] data { get; set; }
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			// The Engine always gives us a BitStream but we can't remote that

			var request = new OnSetPropertyRequest();

			request.property = property;
			request.data = (byte[])value;

			Send("setProperty", JsonConvert.SerializeObject(request));
		}

		[Serializable]
		public class OnGetPropertyResponse : RestProxyPublisherResponse
		{
			public Variant value { get; set; }
		}

		protected override Variant OnGetProperty(string property)
		{
			var json = Send("getProperty");
			var response = JsonConvert.DeserializeObject<OnGetPropertyResponse>(json);
			return response.value;
		}

		[Serializable]
		public class OnOutputRequest
		{
			public byte[] data { get; set; }
		}

		protected override void OnOutput(BitwiseStream data)
		{
			var request = new OnOutputRequest();
			request.data = new byte[data.Length];
			data.Read(request.data, 0, (int)data.Length);

			data.Position = 0;

			Send("output", JsonConvert.SerializeObject(request));
		}

		protected override void OnInput()
		{
			Send("input");
			ReadAllBytes();
		}

		[Serializable]
		public class WantBytesRequest
		{
			public long count { get; set; }
		}

		public override void WantBytes(long count)
		{
			var request = new WantBytesRequest();
			request.count = count;

			Send("WantBytes", JsonConvert.SerializeObject(request));
			ReadAllBytes();
		}

		[Serializable]
		public class ReadBytesRequest
		{
			public int count { get; set; }
		}

		[Serializable]
		public class ReadBytesResponse : RestProxyPublisherResponse
		{
			public byte[] data { get; set; }
		}

		public byte[] ReadBytes(int count)
		{
			var request = new ReadBytesRequest();
			request.count = count;

			var json = Send("ReadBytes", JsonConvert.SerializeObject(request));
			var response = JsonConvert.DeserializeObject<ReadBytesResponse>(json);

			return response.data;
		}

		[Serializable]
		public class ReadRequest
		{
			public int offset { get; set; }
			public int count { get; set; }
		}
		[Serializable]
		public class ReadResponse : RestProxyPublisherResponse
		{
			public int count { get; set; }
			public byte[] data { get; set; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var request = new ReadRequest();
			request.offset = offset;
			request.count = count;

			var json = Send("Read", JsonConvert.SerializeObject(request));
			var response = JsonConvert.DeserializeObject<ReadResponse>(json);

			System.Array.Copy(response.data, buffer, response.count);

			return response.count;
		}

		[Serializable]
		public class ReadByteResponse : RestProxyPublisherResponse
		{
			public int data { get; set; }
		}

		public override int ReadByte()
		{
			var json = Send("ReadByte");
			var response = JsonConvert.DeserializeObject<ReadByteResponse>(json);

			return response.data;
		}

		public byte[] ReadAllBytes()
		{
			var json = Send("ReadAllBytes");
			var response = JsonConvert.DeserializeObject<ReadBytesResponse>(json);

			return response.data;
		}
	}

	#endregion

	[Serializable]
	public class JsonResponse
	{
		public string Status { get; set; }
		public string Data { get; set; }
		public Dictionary<string, object> Results { get; set; }
	}

	[Serializable]
	public class JsonFaultResponse
	{
		public string Status { get; set; }
		public string Data { get; set; }
		public Fault[] Results { get; set; }
	}

	[Serializable]
	public class JsonArgsRequest
	{
		public Dictionary<string, string> args { get; set; }
	}

	[Agent("http", true)]
	public class AgentClientRest : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			RestProxyPublisher publisher;

			public PublisherProxy(string serviceUrl, string cls, Dictionary<string, Variant> args)
			{
				publisher = new RestProxyPublisher(args)
				{
					Url = serviceUrl,
					Class = cls,
				};
			}

			#region IPublisher

			public uint Iteration
			{
				set
				{
					publisher.Iteration = value;
				}
			}

			public bool IsControlIteration
			{
				set
				{
					publisher.IsControlIteration = value;
				}
			}

			public string Result
			{
				get
				{
					return publisher.Result;
				}
			}

			public Stream Stream
			{
				get
				{
					return publisher;
				}
			}

			public void Start()
			{
				publisher.start();
			}

			public void Stop()
			{
				publisher.stop();
			}

			public void Open()
			{
				publisher.open();
			}

			public void Close()
			{
				publisher.close();
			}

			public void Accept()
			{
				publisher.accept();
			}

			public Variant Call(string method, List<ActionParameter> args)
			{
				return publisher.call(method, args);
			}

			public void SetProperty(string property, Variant value)
			{
				publisher.setProperty(property, value);
			}

			public Variant GetProperty(string property)
			{
				return publisher.getProperty(property);
			}

			public void Output(DataModel data)
			{
				publisher.output(data);
			}

			public void Input()
			{
				publisher.input();
			}

			public void WantBytes(long count)
			{
				publisher.WantBytes(count);
			}

			#endregion
		}

		#endregion

		#region Private Members

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return logger; } }

		string serviceUrl;

		#endregion

		#region Constructor

		public AgentClientRest(string name, string uri, string password)
			: base(name, uri, password)
		{
			serviceUrl = new Uri(new Uri(url), "/Agent").ToString();
		}

		#endregion

		#region AgentClient Overrides

		protected override void OnAgentConnect()
		{
			Send("AgentConnect");
		}

		protected override void OnAgentDisconnect()
		{
			Send("AgentDisconnect");
		}

		protected override IPublisher OnCreatePublisher(string cls, Dictionary<string, Variant> args)
		{
			return new PublisherProxy(serviceUrl, cls, args);
		}

		protected override void OnStartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			Send("StartMonitor?name=" + name + "&cls=" + cls, args);
		}

		protected override void OnStopAllMonitors()
		{
			Send("StopAllMonitors");
		}

		protected override void OnSessionStarting()
		{
			Send("SessionStarting");
		}

		protected override void OnSessionFinished()
		{
			Send("SessionFinished");
		}

		protected override void OnIterationStarting(uint iterationCount, bool isReproduction)
		{
			Send("IterationStarting?iterationCount=" + iterationCount.ToString() + "&" + "isReproduction=" + isReproduction.ToString());
		}

		protected override bool OnIterationFinished()
		{
			var json = Send("IterationFinished");
			return ParseResponse(json);
		}

		protected override bool OnDetectedFault()
		{
			var json = Send("DetectedFault");
			return ParseResponse(json);
		}

		protected override Fault[] OnGetMonitorData()
		{
			try
			{
				var json = Send("GetMonitorData");
				var response = JsonConvert.DeserializeObject<JsonFaultResponse>(json);

				return response.Results;
			}
			catch (Exception e)
			{
				logger.Debug(e.ToString());
				throw new PeachException("Failed to get Monitor Data", e);
			}
		}

		protected override bool OnMustStop()
		{
			var json = Send("MustStop");
			return ParseResponse(json);
		}

		protected override Variant OnMessage(string name, Variant data)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Helpers

		bool ParseResponse(string json)
		{
			if (string.IsNullOrEmpty(json))
				throw new PeachException("Agent Response Empty");

			JsonResponse resp;

			try
			{
				resp = JsonConvert.DeserializeObject<JsonResponse>(json);
			}
			catch (Exception e)
			{
				throw new PeachException("Failed to deserialize JSON response from Agent", e);
			}

			return Convert.ToBoolean(resp.Status);
		}

		string Send(string query)
		{
			return Send(query, "");
		}

		string Send(string query, Dictionary<string, Variant> args)
		{
			var newArg = new Dictionary<string, string>();

			foreach (var kv in args)
			{
				// Note: Cast rather than call .ToString() since
				// ToString() can include debugging information
				newArg.Add(kv.Key, (string)kv.Value);
			}

			var request = new JsonArgsRequest();
			request.args = newArg;

			return Send(query, JsonConvert.SerializeObject(request));
		}

		string Send(string query, string json)
		{
			try
			{
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(serviceUrl + "/" + query);
				httpWebRequest.ContentType = "text/json";
				if (string.IsNullOrEmpty(json))
				{
					httpWebRequest.Method = "GET";
				}
				else
				{
					httpWebRequest.Method = "POST";
					using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					{
						streamWriter.Write(json);
					}
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

				if (httpResponse.GetResponseStream() != null)
				{
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						return streamReader.ReadToEnd();
					}
				}
				else
				{
					return "";
				}
			}
			catch (Exception e)
			{
				throw new PeachException("Failure communicating with REST Agent", e);
			}
		}

		#endregion
	}
}
// end
