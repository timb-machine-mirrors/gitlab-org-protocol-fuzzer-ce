using System;
using System.Collections.Generic;
using System.IO;
using Nancy;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Agent.Channels.RestServer
{
	public class AgentService : RestService
	{
		public static readonly string Prefix = "/Agent";

		public AgentService(RestContext context)
			: base(context, Prefix)
		{
			Get["/AgentConnect"] = _ => AgentConnect();
			Get["/AgentDisconnect"] = _ => AgentDisconnect();

			Post["/StartMonitor"] = _ => StartMonitor();
			Get["/StopAllMonitors"] = _ => StopAllMonitors();

			Get["/SessionStarting"] = _ => SessionStarting();
			Get["/SessionFinished"] = _ => SessionFinished();

			Get["/IterationStarting"] = _ => IterationStarting();
			Get["/IterationFinished"] = _ => IterationFinished();

			Get["/DetectedFault"] = _ => DetectedFault();
			Get["/GetMonitorData"] = _ => GetMonitorData();

			Get["/MustStop"] = _ => MustStop();
			Get["/Message"] = _ => Message();

			// PUBLISHER ///////

			Get["/Publisher/Set_Iteration"] = _ => PublisherSetIteration();
			Get["/Publisher/Set_IsControlIteration"] = _ => PublisherSetIsControlIteration();
			Get["/Publisher/start"] = _ => PublisherStart();
			Get["/Publisher/stop"] = _ => PublisherStop();
			Get["/Publisher/open"] = _ => PublisherOpen();
			Get["/Publisher/close"] = _ => PublisherClose();
			Get["/Publisher/accept"] = _ => PublisherAccept();
			Get["/Publisher/call"] = _ => PublisherCall();
			Get["/Publisher/setProperty"] = _ => PublisherSetProperty();
			Get["/Publisher/getProperty"] = _ => PublisherGetProperty();
			Get["/Publisher/output"] = _ => PublisherOutput();
			Get["/Publisher/input"] = _ => PublisherInput();
			Get["/Publisher/WantBytes"] = _ => PublisherWantBytes();
			//Get["/Publisher/ReadBytes"] = _ => PublisherReadBytes();
			Get["/Publisher/Read"] = _ => PublisherRead();
			Get["/Publisher/ReadByte"] = _ => PublisherReadByte();
			//Get["/Publisher/ReadAllBytes"] = _ => PublisherReadAllBytes();
		}

		object AgentConnect()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.AgentConnect();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		object AgentDisconnect()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.AgentDisconnect();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		[Serializable]
		public class StartMonitorRequest
		{
			public Dictionary<string, string> args = null;
		}

		object StartMonitor()
		{
			try
			{
				var args = JsonConvert.DeserializeObject<StartMonitorRequest>(StreamToString(Request.Body)).args;
				var pargs = new Dictionary<string, Variant>();
				foreach (var item in args)
				{
					pargs[item.Key] = new Variant(item.Value);
				}

				var task = new AgentTask()
				{
					Task = _ =>
					{
						context.Agent.StartMonitor(Request.Query.name, Request.Query.cls, pargs);
						return null;
					}
				};

				context.Dispatcher.QueueTask(task);
				task.Completed.WaitOne();

				return HttpStatusCode.OK;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error, " + ex.Message);
				return HttpStatusCode.BadRequest;
			}
		}

		object StopAllMonitors()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.StopAllMonitors();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		object SessionStarting()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.SessionStarting();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		object SessionFinished()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.SessionFinished();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		object IterationStarting()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					Console.Error.WriteLine(">>IterationStarting");
					this.context.Agent.IterationStarting(
						uint.Parse(Request.Query.iterationCount), 
						Request.Query.isReproduction.ToString().ToLower() == "true");
					Console.Error.WriteLine("<<IterationStarting");
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();
			return HttpStatusCode.OK;
		}

		object IterationFinished()
		{
			Console.Error.WriteLine(">>IterationFinished");

			var task = new AgentTask()
			{
				Task = _ =>
				{
					context.Agent.IterationFinished();
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();

			Console.Error.WriteLine("<<IterationFinished");

			return Response.AsJson(new JsonResponse()
			{
				Status = task.Result.ToString()
			});
		}

		object DetectedFault()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					return this.context.Agent.DetectedFault();
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();

			return Response.AsJson(new JsonResponse()
			{
				Status = task.Result.ToString()
			});
		}

		object GetMonitorData()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					return this.context.Agent.GetMonitorData();
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();

			return Response.AsJson(new JsonFaultResponse()
			{
				Results = (Fault[])task.Result
			});
		}

		object MustStop()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					return this.context.Agent.MustStop();
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();

			return Response.AsJson(new JsonResponse()
			{
				Status = task.Result.ToString()
			});
		}

		object Message()
		{
			var task = new AgentTask()
			{
				Task = _ =>
				{
					this.context.Agent.Message(Request.Query.data);
					return null;
				}
			};

			context.Dispatcher.QueueTask(task);
			task.Completed.WaitOne();

			return HttpStatusCode.OK;
		}

		#region Publisher

		string StreamToString(Stream stream)
		{
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}

		object PublisherSetIteration()
		{
			var iter = JsonConvert.DeserializeObject<RestProxyPublisher.IterationRequest>(StreamToString(Request.Body));

			context.Publisher.Iteration = iter.iteration;
			return HttpStatusCode.OK;
		}

		object PublisherSetIsControlIteration()
		{
			var contrl = JsonConvert.DeserializeObject<RestProxyPublisher.IsControlIterationRequest>(StreamToString(Request.Body));

			context.Publisher.IsControlIteration = contrl.isControlIteration;
			return HttpStatusCode.OK;
		}

		object PublisherStart()
		{
			context.Publisher.Start();
			return HttpStatusCode.OK;
		}

		object PublisherStop()
		{
			context.Publisher.Stop();
			return HttpStatusCode.OK;
		}

		object PublisherOpen()
		{
			context.Publisher.Open();
			return HttpStatusCode.OK;
		}

		object PublisherClose()
		{
			context.Publisher.Close();
			return HttpStatusCode.OK;
		}

		object PublisherAccept()
		{
			context.Publisher.Accept();
			return HttpStatusCode.OK;
		}

		Peach.Core.Dom.DataModel CreateDm(byte[] data)
		{
			var dm = new Peach.Core.Dom.DataModel();
			dm.Add(new Peach.Core.Dom.Blob());

			dm[0].DefaultValue = new Variant(data);

			return dm;
		}

		object PublisherCall()
		{
			var call = JsonConvert.DeserializeObject<RestProxyPublisher.OnCallRequest>(
				StreamToString(Request.Body));

			List<ActionParameter> args = new List<ActionParameter>();

			foreach (var arg in call.args)
			{
				args.Add( new Peach.Core.Dom.ActionParameter(arg.name)
				{
					type = arg.type,
					dataModel = CreateDm(arg.data)
				});
			}

			return new RestProxyPublisher.OnCallResponse()
			{
				value = (byte[])context.Publisher.Call(call.method, args)
			};
		}

		object PublisherSetProperty()
		{
			var req = JsonConvert.DeserializeObject<RestProxyPublisher.OnSetPropertyRequest>(StreamToString(Request.Body));

			context.Publisher.SetProperty(req.property, new Variant(req.data));
			return HttpStatusCode.OK;
		}

		object PublisherGetProperty()
		{
			return new RestProxyPublisher.OnGetPropertyResponse()
			{
				error = false,
				value = (byte[])context.Publisher.GetProperty(
					JsonConvert.DeserializeObject<string>(StreamToString(Request.Body)))
			};
		}

		object PublisherOutput()
		{
			var req = JsonConvert.DeserializeObject<RestProxyPublisher.OnOutputRequest>(StreamToString(Request.Body));

			context.Publisher.Output(CreateDm(req.data));
			return HttpStatusCode.OK;
		}

		object PublisherInput()
		{
			context.Publisher.Input();
			return HttpStatusCode.OK;
		}

		object PublisherWantBytes()
		{
			var req = JsonConvert.DeserializeObject<RestProxyPublisher.WantBytesRequest>(StreamToString(Request.Body));

			context.Publisher.WantBytes(req.count);
			return HttpStatusCode.OK;
		}

		#region Stream

		object PublisherRead()
		{
			var req = JsonConvert.DeserializeObject<RestProxyPublisher.ReadRequest>(StreamToString(Request.Body));

			byte [] buff = new byte[req.count];
			int count = context.Publisher.Stream.Read(buff, req.offset, req.count);

			return new RestProxyPublisher.ReadResponse()
			{
				count = count,
				data = buff
			};
		}

		object PublisherReadByte()
		{
			return new RestProxyPublisher.ReadByteResponse()
			{
				data = context.Publisher.Stream.ReadByte()
			};
		}

		#endregion

		#endregion
	}
}
