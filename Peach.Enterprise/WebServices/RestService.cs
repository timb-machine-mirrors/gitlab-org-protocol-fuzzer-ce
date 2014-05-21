
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Peach.Core;
using NLog;
using Nancy.Serialization.JsonNet;
using Newtonsoft.Json;

#if DISABLED
namespace Peach.Enterprise.WebServices
{
	public class RestService : Nancy.NancyModule
	{
		Engine engine;

		public RestService(Engine engine)
			: base("/p")
		{
			this.engine = engine;

			Get["/jobs"] = _ => GetJobs();

			Get["/jobs/{id}/visualizer"] = _ => GetVisualizerData(_.id);

			Get["/jobs/{id}"] = _ => GetJob(_.id);

			Get["/peaches"] = _ => { return null; };

			Get["/nodes"] = _ => GetNodes();
			Get["/nodes/{id}"] = _ => GetNode(_.id);

			Get["/pits"] = _ => GetPits();
			Get["/pits/{id}"] = _ => GetPit(_.id);

			Get["/libraries"] = _ => GetLibraries();
			Get["/libraries/{id}"] = _ => GetLibrary(_.id);

			Get["/faults"] = _ => GetFaults();
			Get["/faults/{id}"] = _ => GetFault(_.id);
			Get["/faults/risk"] = _ => GetFaultsRisk();
		}

		object GetFaultsRisk()
		{
			return null;
		}

		object GetVisualizerData(string id)
		{
			foreach (var logger in engine.context.test.loggers)
			{
				var v = logger as Peach.Enterprise.Loggers.VisualizerLogger;
				if (v != null)
				{
					var response = (Nancy.Response)v.getJson();

					response.ContentType = "application/json";

					return response;
				}
			}

			return null;
		}

		object GetJobs()
		{
			return new Job[] { new Job(new Uri(Request.Url), engine) };
		}

		object GetJob(string id)
		{

			
			var job = new Job(new Uri(Request.Url), engine);

			try
			{
				var a0 = job.name;
				var a1 = job.startDate;
				var a2 = job.stopDate;
				var a3 = job.runtime;
				var a4 = job.speed;
				var a5 = job.totalFaults;

				return job;
			}
			catch (Exception e)
			{
				return e;
			}
		}


		object GetNodes()
		{
			return new Node[] { new Node(new Uri(Request.Url), engine) };
		}
		object GetNode(string id)
		{
			return new Node(new Uri(Request.Url), engine);
		}

		object GetPits()
		{
			return new Pit[] { new Pit(new Uri(Request.Url), engine) };
		}
		object GetPit(string id)
		{
			return new Pit(new Uri(Request.Url), engine);
		}

		object GetLibraries()
		{
			return new Library[] { new Library(new Uri(Request.Url), engine) };
		}
		object GetLibrary(string id)
		{
			return new Library(new Uri(Request.Url), engine);
		}

		object GetFaults()
		{
			var faults = (List<Fault>)engine.context.stateStore["Peach.Rest.Faults"];
			return faults.ToArray();
		}
		object GetFault(string sid)
		{
			Guid id = Guid.Parse(sid);

			foreach (Fault fault in (List<Fault>)engine.context.stateStore["Peach.Rest.Faults"])
			{
				if (fault.id == id)
					return fault;
			}

			return null;
		}

		public static void Initialize(Engine engine)
		{
			engine.Fault += engine_Fault;
			engine.ReproFault += engine_ReproFault;
			engine.ReproFailed += engine_ReproFailed;
		}

		static void engine_ReproFailed(RunContext context, uint currentIteration)
		{
			((List<Fault>)context.stateStore["Peach.Rest.Faults"]).Last().reproducable = false;
		}

		static void engine_ReproFault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Core.Fault[] faultData)
		{
			Fault current = null;

			foreach (var fault in faultData)
			{
				if (fault.type == FaultType.Fault)
				{
					current = new Fault(context.engine, fault);
					current.reproducable = false;

					((List<Fault>)context.stateStore["Peach.Rest.Faults"]).Add(current);
					((List<Core.Fault>)context.stateStore["Peach.Faults"]).Add(fault);

					break;
				}
			}

			List<Fault.File> files = new List<Fault.File>();

			foreach (var fault in faultData)
			{
				if (fault.type != FaultType.Fault)
				{
					var file = new Fault.File();
					file.name = System.IO.Path.Combine(fault.folderName, fault.monitorName);
					files.Add(file);
				}
			}

			//current.files = files.ToArray();
		}

		static void engine_Fault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Core.Fault[] faultData)
		{
			var faults = ((List<Fault>)context.stateStore["Peach.Rest.Faults"]);
			var coreFaults = ((List<Core.Fault>)context.stateStore["Peach.Faults"]);

			Fault current = null;
			var last = faults.Last();

			foreach (var fault in faultData)
			{
				if (fault.type == FaultType.Fault)
				{
					current = new Fault(context.engine, fault);
					current.id = last.id;
					current.reproducable = true;

					faults[faults.IndexOf(last)] = current;
					coreFaults[coreFaults.IndexOf(coreFaults.Last())] = fault;

					break;
				}
			}

			List<Fault.File> files = new List<Fault.File>();

			foreach (var fault in faultData)
			{
				if (fault.type != FaultType.Fault)
				{
					var file = new Fault.File();
					file.name = System.IO.Path.Combine(fault.folderName, fault.monitorName);
					files.Add(file);
				}
			}

			//current.files = files.ToArray();
		}
	}

	public class Node
	{
		Uri _baseUrl;
		Engine _engine;

		public Node(Uri url, Engine engine)
		{
			_baseUrl = url;
			_engine = engine;

			mac = "00:00:00:00:00:00";
			ip = "127.0.0.1";
			tags = new string[0];
			status = "alive";
			version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			timestamp = DateTime.Now.ToShortTimeString();

			job = new Job(_baseUrl, _engine);
		}

		public string nodeUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/peaches/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string mac { get; set; }
		public string ip { get; set; }
		public string[] tags { get; set; }
		public string status { get; set; }
		public string version { get; set; }
		public string timestamp { get; set; }
		public Job job { get; set; }
	}

	public class Job
	{
		Uri _baseUrl;
		Engine _engine;

		public Job(Uri baseUrl, Engine engine)
		{
			_engine = engine;
			_baseUrl = baseUrl;
		}

		public string jobUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/jobs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string faultsUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/faults").ToString();
			}
		}
		public string targetUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/target/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string targetConfigUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/target/" + Guid.Empty.ToString() + "/configs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string nodesUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/nodes").ToString();
			}
		}
		public string pitUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string peachUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/peaches/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string reportUrl { get { return null; } }
		public string packageFileUrl { get { return null; } }

		public string name { get { return _engine.context.test.name; } }
		public string notes { get { return ""; } }
		public string user { get { return "peach"; } }
		public uint seed { get { return _engine.context.config.randomSeed; } }
		public uint iterationCount { get { return _engine.context.currentIteration; } }
		public string startDate { get { return ((DateTime)_engine.context.stateStore["Peach.StartTime"]).ToShortDateString(); } }
		public string stopDate { get { return ""; } }

		public string runtime { get { return (DateTime.Now - ((DateTime)_engine.context.stateStore["Peach.StartTime"])).ToString(@"hh\:mm\:ss"); } }
		public int speed { get { return (int)((iterationCount / (DateTime.Now - ((DateTime)_engine.context.stateStore["Peach.StartTime"])).TotalMinutes) * 60); } }
		
		public string[] tags { get { return new string[0]; } }
		public Group[] groups { get { return new Group[0]; } }

		public int totalFaults { get { return ((List<Fault>)_engine.context.stateStore["Peach.Rest.Faults"]).Count; } }
	}

	public class Library
	{
		Uri _baseUrl;
		Engine _engine;

		public Library(Uri baseUrl, Engine engine)
		{
			_baseUrl = baseUrl;
			_engine = engine;
		}

		public string libraryUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/libraries/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string name { get { return "Peach Library"; } }
		public string description { get { return ""; } }
		public bool locked { get { return true; } }
		public Version[] versions { get; set; }
		public Group[] groups { get; set; }
		public string user { get { return "peach"; } }
		public DateTime timestamp { get { return DateTime.Now; } }

		public class Version
		{
			Uri _baseUrl;
			Engine _engine;

			public Version(Uri baseUrl, Engine engine)
			{
				_baseUrl = baseUrl;
				_engine = engine;
			}

			public int version { get { return 1; } }
			public bool locked { get { return true; } }
			public Pit[] pits { get { return new Pit[] { new Pit(_baseUrl, _engine) }; } }
			public string user { get { return "peach"; } }
			public DateTime timestamp { get { return DateTime.Now; } }
		}
	}

	public class Group
	{
		public Group(Uri baseUrl, Engine engine)
		{
		}

		public string groupUrl { get; set; }
		public string access { get { return "read"; } }
	}

	public class Pit
	{
		Uri _baseUrl;
		Engine _engine;

		public Pit(Uri baseUrl, Engine engine)
		{
			_baseUrl = baseUrl;
			_engine = engine;
		}

		public string pitUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}

		public string name { get { return _engine.context.config.pitFile; } }
		public string description { get { return ""; } }
		public string[] tags { get { return new string[0]; } }
	}

	public class Fault
	{
		Uri _baseUrl = null;
		Engine _engine;
		Peach.Core.Fault _fault = null;
		DateTime _timestamp;
		public Guid id = Guid.NewGuid();

		public Fault(Engine engine, Peach.Core.Fault fault)
		{
			_engine = engine;
			_baseUrl = new Uri("http://localhost:8888");
			_fault = fault;
			_timestamp = DateTime.Now;
			//files = new File[] { };
			reproducable = false;
		}

		public string faultUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/faults/" + id.ToString()).ToString();
			}
		}
		public string jobUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/jobs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string targetUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/targets/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string pitUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string nodeUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/nodes/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string peachUrl
		{
			get
			{
				return new Uri(_baseUrl, "/p/peaches/" + Guid.Empty.ToString()).ToString();
			}
		}

		public string title { get { return _fault.title; } }
		//public string description { get { return _fault.description; } }
		public string source { get { return _fault.detectionSource; } }
		public bool reproducable { get; set; }
		public uint iteration { get { return _fault.iteration; } }
		public uint seed { get { return _engine.context.config.randomSeed; } }
		public string faultType { get { return _fault.type.ToString(); } }
		public string exploitability { get { return _fault.exploitability; } }
		public string majorHash { get { return _fault.majorHash; } }
		public string minorHash { get { return _fault.minorHash; } }
		public string folderName { get { return _fault.folderName; } }
		public string timestamp { get { return _timestamp.ToString(); } }
		//public File[] files { get; set; }
		//public string[][] mutations { get { return new string[][] {}; } }
		//public string[] tags { get { return new string[] {}; } }

		public class File
		{
			Uri _baseUrl = null;
			Guid id = Guid.NewGuid();

			public File()
			{
			}

			public string fileUrl
			{
				get
				{
					return new Uri(_baseUrl, "/p/files/" + id.ToString()).ToString();
				}
			}

			public string name { get; set; }
		}
	}
}
#endif

// end
