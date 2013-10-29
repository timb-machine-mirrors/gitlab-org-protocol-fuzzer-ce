using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PeachFarm.Common
{
  public static class QueueNames
  {
    public static readonly string EXCHANGE_CONTROLLER = "";
    public static readonly string QUEUE_CONTROLLER = "peachfarm.controller.{0}";

    public static readonly string EXCHANGE_ADMIN = "";
    public static readonly string QUEUE_ADMIN = "peachfarm.admin.{0}";
		public static readonly string QUEUE_MONITOR = "peachfarm.monitor.{0}";

    public static readonly string EXCHANGETYPE_NODE = "fanout";
    public static readonly string EXCHANGE_NODE = "peachfarm.node";
    public static readonly string QUEUE_NODE = "peachfarm.node.{0}";

		public static readonly string QUEUE_REPORTGENERATOR_PROCESSONE = "peachfarm.reportgenerator-processone";
		public static readonly string QUEUE_REPORTGENERATOR = "peachfarm.reportgenerator";
		

    public static readonly string EXCHANGE_JOB = "peachfarm.job.{0}";
  }

	public static class Formats
	{
		public static readonly string JobFolder = "Job_{0}_{1}";
		public static readonly string NodeFolder = "Node_{0}";
		public static readonly string JobNodeFolder = "Job_{0}_{1}/Node_{2}";
	}

	public static class ApiUrls
	{
		private const string baseUrl = "/p";
		private const string param0 = "/{0}";
		private const string param1 = "/{1}";

		public static string f(string format, string p0, string p1 = null, string p2 = null, string p3 = null)
		{
			return String.Format(format, p0, p1, p2, p3);
		}

		public static readonly string Faults = baseUrl + "/faults";
		public static readonly string Fault = Faults + param0;
		public static readonly string Buckets = baseUrl + "/buckets";
		public static readonly string Bucket = Buckets + param0;
		public static readonly string Nodes = baseUrl + "/nodes";
		public static readonly string Jobs = baseUrl + "/jobs";
		public static readonly string Job = Jobs + param0;
		public static readonly string JobFaults = Job + "/faults";
		public static readonly string Pits = baseUrl + "/pits";
		public static readonly string PitVersion = Pits + "/{0}/{1}";
		public static readonly string Libraries = baseUrl + "/libraries";
		public static readonly string Library = Libraries + param0;
		public static readonly string LibraryVersions = Library + "/versions";
		public static readonly string LibraryVersion = LibraryVersions + param1;
		public static readonly string Peaches = baseUrl + "/peaches";
		public static readonly string Peach = Peaches + "/{0}/{1}/{2}/{3}";
		public static readonly string Targets = baseUrl + "/targets";
		public static readonly string Target = Targets + param0;
		public static readonly string TargetConfigs = Target + "/configs";
		public static readonly string TargetConfig = TargetConfigs + param1;
		public static readonly string TargetJobTemplate = Target + "/jobtemplates" + param0;
		public static readonly string SampleCollections = baseUrl + "/collections";
		public static readonly string SampleCollection = SampleCollections + param0;
		public static readonly string Samples = SampleCollection + "/samples";
		public static readonly string Users = baseUrl + "/users";
		public static readonly string User = Users + param0;
		public static readonly string Groups = baseUrl + "/groups";
		public static readonly string Group = Groups + param0;
	}
}
