
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Peach.Core;
using NLog;

namespace Peach.Enterprise.WebServices
{
	public class RestContext
	{
		public Engine Engine = null;
		public List<Fault> Faults = new List<Fault>();
		public Uri BaseUrl = new Uri("http://unknown");
	}

	public class RestService : Nancy.NancyModule
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		static RestContext _context = new RestContext();

		public RestService()
			: base("/p")
		{
			Get["/jobs"] = _ => GetJobs();
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
		}

		object GetJobs()
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Job[] { new Job(_context) }; 
		}

		object GetJob(string id)
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Job(_context);
		}

		object GetNodes()
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Node[] { new Node(_context) };
		}
		object GetNode(string id)
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Node(_context);
		}

		object GetPits()
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Pit[] { new Pit(_context) };
		}
		object GetPit(string id)
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Pit(_context);
		}

		object GetLibraries()
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Library[] { new Library(_context) };
		}
		object GetLibrary(string id)
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return new Library(_context);
		}

		object GetFaults()
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			return _context.Faults.ToArray();
		}
		object GetFault(string sid)
		{
			_context.BaseUrl = new Uri(this.Context.Request.Url.ToString());
			_context.BaseUrl = new Uri(_context.BaseUrl.AbsoluteUri.Substring(0, _context.BaseUrl.AbsoluteUri.Length - _context.BaseUrl.AbsolutePath.Length));
			Guid id = Guid.Parse(sid);

			foreach (Fault fault in _context.Faults)
			{
				if (fault.id == id)
					return fault;
			}

			return null;

		}

		public static void Initialize(Engine engine)
		{
			_context.Engine = engine;
			engine.Fault += engine_Fault;
			engine.ReproFault += engine_ReproFault;
			engine.ReproFailed += engine_ReproFailed;
		}

		static void engine_ReproFailed(RunContext context, uint currentIteration)
		{
			_context.Faults.Last().reproducable = false;
		}

		static void engine_ReproFault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Core.Fault[] faultData)
		{
			Fault current = null;

			foreach (var fault in faultData)
			{
				if (fault.type == FaultType.Fault)
				{
					current = new Fault(_context, fault);
					current.reproducable = false;

					_context.Faults.Add(current);

					break;
				}
			}

			List<Fault.File> files = new List<Fault.File>();

			foreach (var fault in faultData)
			{
				if (fault.type != FaultType.Fault)
				{
					var file = new Fault.File(_context);
					file.name = System.IO.Path.Combine(fault.folderName, fault.monitorName);
					files.Add(file);
				}
			}

			current.files = files.ToArray();
		}

		static void engine_Fault(RunContext context, uint currentIteration, Core.Dom.StateModel stateModel, Core.Fault[] faultData)
		{
			Fault current = null;
			var last = _context.Faults.Last();

			foreach (var fault in faultData)
			{
				if (fault.type == FaultType.Fault)
				{
					current = new Fault(_context, fault);
					current.id = last.id;
					current.reproducable = true;

					_context.Faults[_context.Faults.IndexOf(last)] = current;

					break;
				}
			}

			List<Fault.File> files = new List<Fault.File>();

			foreach (var fault in faultData)
			{
				if (fault.type != FaultType.Fault)
				{
					var file = new Fault.File(_context);
					file.name = System.IO.Path.Combine(fault.folderName, fault.monitorName);
					files.Add(file);
				}
			}

			current.files = files.ToArray();
		}
	}

	public class Node
	{
		RestContext _context;

		public Node(RestContext context)
		{
			_context = context;
			mac = "00:00:00:00:00:00";
			ip = "127.0.0.1";
			tags = new string[0];
			status = "alive";
			version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			timestamp = DateTime.Now.ToShortTimeString();
			job = new Job(context);
		}

		public string nodeUrl
		{
			get
			{
				return new Uri(
					_context.BaseUrl,
					"/p/peaches/" + Guid.Empty.ToString()).ToString();
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
		RestContext _context;
		uint _currentIteration = 0;

		public Job(RestContext context)
		{
			_context = context;
			_context.Engine.IterationStarting += engine_IterationStarting;
		}

		~Job()
		{
			if(_context != null && _context.Engine != null)
				_context.Engine.IterationStarting -= engine_IterationStarting;
		}

		void engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
 			_currentIteration = currentIteration;
		}

		public string jobUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/jobs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string faultsUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/faults").ToString();
			}
		}
		public string targetUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/target/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string targetConfigUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/target/" + Guid.Empty.ToString() + "/configs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string nodesUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/nodes").ToString();
			}
		}
		public string pitUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string peachUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/peaches/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string reportUrl { get { return null; } }
		public string packageFileUrl { get { return null; } }

		public string name { get { return "Default Peach Job"; } }
		public string notes { get { return ""; } }
		public string user { get { return "peach"; } }
		public uint seed { get { return _context.Engine.context.config.randomSeed; } }
		public uint iterationCount { get { return _currentIteration; } }
		public DateTime startDate { get { return DateTime.Now; } }
		public DateTime stopData { get { return DateTime.Now; } }
		public string[] tags { get { return new string[0]; } }
		public Group[] groups { get { return new Group[0]; } }
	}

	public class Library
	{
		RestContext _context;

		public Library(RestContext context)
		{
			_context = context;
		}

		public string libraryUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/libraries/" + Guid.Empty.ToString()).ToString();
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
			RestContext _context;

			public Version(RestContext context)
			{
				_context = context;
			}

			public int version { get { return 1; } }
			public bool locked { get { return true; } }
			public Pit[] pits { get { return new Pit[] { new Pit(_context)}; } }
			public string user { get { return "peach"; } }
			public DateTime timestamp { get { return DateTime.Now; } }
		}
	}

	public class Group
	{
		public Group(RestContext context)
		{
		}

		public string groupUrl { get; set; }
		public string access { get { return "read"; } }
	}

	public class Pit
	{
		RestContext _context;

		public Pit(RestContext context)
		{
			_context = context;
		}

		public string pitUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}

		public string name { get { return _context.Engine.context.config.pitFile; } }
		public string description { get { return ""; } }
		public string[] tags { get { return new string[0]; } }
	}

	public class Fault
	{
		RestContext _context;
		Peach.Core.Fault _fault = null;
		DateTime _timestamp;
		public Guid id = Guid.NewGuid();

		public Fault(RestContext context, Peach.Core.Fault fault)
		{
			_context = context;
			_fault = fault;
			_timestamp = DateTime.Now;

		}

		public string faultUrl
		{
			get
			{
				return new Uri(_context.BaseUrl, "/p/faults/" + id.ToString()).ToString();
			}
		}
		public string jobUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/jobs/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string targetUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/targets/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string pitUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/pits/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string nodeUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/nodes/" + Guid.Empty.ToString()).ToString();
			}
		}
		public string peachUrl
		{
			get
			{
				return new Uri(_context.BaseUrl,"/p/peaches/" + Guid.Empty.ToString()).ToString();
			}
		}

		public string title { get { return _fault.title; } }
		public string description { get { return _fault.description; } }
		public string source { get { return _fault.detectionSource; } }
		public bool reproducable { get; set; }
		public uint iteration { get { return _fault.iteration; } }
		public uint seed { get { return _context.Engine.context.config.randomSeed; } }
		public FaultType faultType { get { return _fault.type; } }
		public string exploitability { get { return _fault.exploitability; } }
		public string majorHash { get { return _fault.majorHash; } }
		public string minorHash { get { return _fault.minorHash; } }
		public string folderName { get { return _fault.folderName; } }
		public string timestamp { get { return _timestamp.ToString(); } }
		public File[] files { get; set; }
		public string[][] mutations { get { return new string[0][]; } }
		public string[] tags { get { return new string[0]; } }

		public class File
		{
			RestContext _context;
			Guid id = Guid.NewGuid();

			public File(RestContext context)
			{
				_context = context;
			}

			public string fileUrl
			{
				get
				{
					return new Uri(_context.BaseUrl, "/p/files/" + id.ToString()).ToString();
				}
			}

			public string name { get; set; }
		}
	}
}

// end
